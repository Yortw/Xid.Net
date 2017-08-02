using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

// Ported from https://github.com/rs/xid
// Original license; https://github.com/rs/xid/blob/master/LICENSE
// Xid.Net Latest code/license; https://github.com/Yortw/Xid.Net

namespace XidNet
{
	/// <summary>
	/// Represents a statistcally unique value, similar to a <see cref="System.Guid"/> but only 15 bytes in it's raw form and 20 bytes when encoded as a string.
	/// </summary>
	/// <remarks>
	/// <para>Xid is using Mongo Object ID algorithm to generate globally unique ids with a different serialization (base64) to make it shorter when transported as a string: https://docs.mongodb.org/manual/reference/object-id/</para>
	/// <list type="Bullet">
	/// <item>4-byte value representing the seconds since the Unix epoch.</item>
	/// <item>3-byte machine identifier.</item>
	/// <item>2-byte process id.</item>
	/// <item>3-byte counter, starting with a random value.</item>
	/// </list>
	/// <para>
	/// The binary representation of the id is compatible with Mongo 12 bytes Object IDs. The string representation is using base32 hex (w/o padding) for better space efficiency when stored in that form (20 bytes). The hex variant of base32 is used to retain the sortable property of the id.
	/// </para>
	/// <para>
	/// Xid doesn't use base64 because case sensitivity and the 2 non alphanum chars may be an issue when transported as a string between various systems. Base36 wasn't retained either because 1/ it's not standard 2/ the resulting size is not predictable (not bit aligned) and 3/ it would not remain sortable. To validate a base32 xid, expect a 20 chars long, all lowercase sequence of a to v letters and 0 to 9 numbers ([0-9a-v]{20}).
	/// </para>
	/// <para>
	/// No configuration or central generator server is required so it can be used directly in server's code.
	/// </para>
	/// <para>Xid's are also k-sortable.</para>
	/// <para>Unicity guaranteed for 16,777,216 (24 bits) unique ids per second and per host/process.</para>
	/// <para>Lock-free (i.e.: unlike UUIDv1 and v2).</para>
	/// </remarks>
	[CLSCompliant(true)]
	public partial struct Xid : IFormattable, IEquatable<Xid>, IComparable, IComparable<Xid>
	{

		#region Public Static Interface

		#region Public Constants & Static Properties

		/// <summary>
		/// The length of a Xid when converted to a string.
		/// </summary>
		public const int EncodedLength = 20;
		/// <summary>
		/// The length of a Xid as raw bytes.
		/// </summary>
		public const int Length = 12;

		/// <summary>
		/// An empty Xid, the defaut value for a new Xid using the default constructor.
		/// </summary>
		public static readonly Xid Empty = new Xid();

		#endregion

		private const long FileTimeOffset = 584388 * 864000000000;

		/// <summary>
		/// Creates a new <see cref="Xid"/> value.
		/// </summary>
		/// <remarks>
		/// <para>This method is thread-safe in that it can be called from multiple threads simulataneously and each call will return a valid <see cref="Xid"/> and not corrupt any internal state.</para>
		/// </remarks>
		/// <returns>A new <see cref="Xid"/> value.</returns>
		public static Xid NewXid()
		{
			var retVal = new Xid();

			// Timestamp, 4 bytes, big endian
			// Use UtcNow as it's significantly faster/lower allocation than .Now. 
			// Tried optimizing by calling GetSystemTimeAsFileTime directly and adding
			// in the appropriate offset, which worked, but was consistently slower (slightly)
			// and isn't portable across OS', so reverted to UtcNow.Ticks.
			// Shame about the DateTime values created needlessly, but haven't found better
			// solution yet.
			var ts = (DateTime.UtcNow.Ticks - UnixEpochTicks) / 10000000;

			retVal._B1 = (byte)(ts >> 24);
			retVal._B2 = (byte)(ts >> 16);
			retVal._B3 = (byte)(ts >> 8);
			retVal._B4 = (byte)ts;

			// Machine, first 3 bytes of md5(hostname)
			retVal._B5 = MachineID[0];
			retVal._B6 = MachineID[1];
			retVal._B7 = MachineID[2];

			// Pid, 2 bytes, specs don't specify endianness, but we use big endian.
			retVal._B8 = ProcessId[0];
			retVal._B9 = ProcessId[1];

			// Counter, 3 bytes, big endian
			var c = System.Threading.Interlocked.Increment(ref Counter);
			retVal._B10 = (byte)(c >> 16);
			retVal._B11 = (byte)(c >> 8);
			retVal._B12 = (byte)c;

			return retVal;
		}

		/// <summary>
		/// Parses a string into a <see cref="Xid"/>, throws an exception if parse fails.
		/// </summary>
		/// <remarks>Assumes the string is a base 32 encoded Xid.</remarks>
		/// <param name="encodedXid">The string to be parsed.</param>
		/// <returns>A new <see cref="Xid"/> from the parsed string.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="encodedXid"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="encodedXid"/> is not the correct length (<see cref="EncodedLength"/>) for a Xid, or if the Xid cannot be parsed for any other reason (such as containing invalid characters).</exception>
		public static Xid Parse(string encodedXid)
		{
			if (encodedXid == null) throw new ArgumentNullException(nameof(encodedXid));
			if (encodedXid.Length != EncodedLength) throw new ArgumentException(String.Format(System.Globalization.CultureInfo.CurrentCulture, ErrorMessages.XidStringArgLength, EncodedLength), nameof(encodedXid));
			if (!ContainsOnlyValidXidChars(encodedXid)) throw new ArgumentException("encodedXid is not a valid Xid.", nameof(encodedXid));
			var rawBytes = InternalParse(encodedXid);
			if (rawBytes == null) throw new ArgumentException("encodedXid is not a valid Xid.", nameof(encodedXid));

			return new Xid(rawBytes);
		}

		/// <summary>
		/// Attempts to parse a string into a <see cref="Xid"/>. 
		/// </summary>
		/// <remarks>Assumes the string is a base 32 encoded Xid.</remarks>
		/// <param name="encodedXid">The string to be parsed.</param>
		/// <param name="xid">A <see cref="Xid"/> if the parse is successful.</param>
		/// <returns>True if the string was successfully parsed, otherwise false.</returns>
		public static bool TryParse(string encodedXid, out Xid xid)
		{
			if (String.IsNullOrEmpty(encodedXid) || encodedXid.Length != EncodedLength)
			{
				xid = Xid.Empty;
				return false;
			}

			if (!ContainsOnlyValidXidChars(encodedXid))
			{
				xid = Xid.Empty;
				return false;
			}

			var rawBytes = InternalParse(encodedXid);
			if (rawBytes != null)
			{
				xid = new Xid(rawBytes);
				return true;
			}

			xid = Xid.Empty;
			return false;
		}

		private static bool ContainsOnlyValidXidChars(string encodedXid)
		{
			Char c;
			for (var i = 0; i < encodedXid.Length; i++)
			{
				c = encodedXid[i];
				if (c < '0' || (c > '9' && c < 'a') || c > 'v')
				{
					return false;
				}
			}
			return true;
		}

		private static byte[] InternalParse(string xidStr)
		{
			var retVal = new byte[Xid.Length];

			retVal[0] = (byte)(DecodeMap[xidStr[0]] << 3 | DecodeMap[xidStr[1]] >> 2);
			retVal[1] = (byte)(DecodeMap[xidStr[1]] << 6 | DecodeMap[xidStr[2]] << 1 | DecodeMap[xidStr[3]] >> 4);
			retVal[2] = (byte)(DecodeMap[xidStr[3]] << 4 | DecodeMap[xidStr[4]] >> 1);
			retVal[3] = (byte)(DecodeMap[xidStr[4]] << 7 | DecodeMap[xidStr[5]] << 2 | DecodeMap[xidStr[6]] >> 3);
			retVal[4] = (byte)(DecodeMap[xidStr[6]] << 5 | DecodeMap[xidStr[7]]);
			retVal[5] = (byte)(DecodeMap[xidStr[8]] << 3 | DecodeMap[xidStr[9]] >> 2);
			retVal[6] = (byte)(DecodeMap[xidStr[9]] << 6 | DecodeMap[xidStr[10]] << 1 | DecodeMap[xidStr[11]] >> 4);
			retVal[7] = (byte)(DecodeMap[xidStr[11]] << 4 | DecodeMap[xidStr[12]] >> 1);
			retVal[8] = (byte)(DecodeMap[xidStr[12]] << 7 | DecodeMap[xidStr[13]] << 2 | DecodeMap[xidStr[14]] >> 3);
			retVal[9] = (byte)(DecodeMap[xidStr[14]] << 5 | DecodeMap[xidStr[15]]);
			retVal[10] = (byte)(DecodeMap[xidStr[16]] << 3 | DecodeMap[xidStr[17]] >> 2);
			retVal[11] = (byte)(DecodeMap[xidStr[17]] << 6 | DecodeMap[xidStr[18]] << 1 | DecodeMap[xidStr[19]] >> 4);

			return retVal;
		}

		#endregion

		#region Private Constants & Static Members

		private const int DecodedLen = 15; // len after base32 decoding with the padded data encoding stores a custom version of the base32 encoding with lower case letters.
		private const string Encoding = "0123456789abcdefghijklmnopqrstuv";
		private const int MaxCounter = 16777216;
		private const int HalfMaxCounter = 8388608;
		private const int HashMultiplier = 486187739;
		private static readonly long UnixEpochTicks = 621355968000000000;

		private static readonly byte[] DecodeMap = new byte[256];
		private static readonly Random _Rand = new Random();

		//private static long LastTimestamp = 0;

		private static volatile int Counter = GenerateRandomSeed();
		private static byte[] ProcessId = GenerateProcessIdBytes();

		private static byte[] MachineID = GenerateMachineIdBytes();

		private static byte[] GenerateProcessIdBytes()
		{
			var pid = System.Diagnostics.Process.GetCurrentProcess().Id;
			var retVal = new byte[2];
			retVal[0] = (byte)(pid >> 8);
			retVal[1] = (byte)pid;
			return retVal;
		}

		private static int GenerateRandomSeed()
		{
			//Don't use the full counter range as a high value
			//means we're likely to overflow the counter within
			//one second which breaks the k-ordering feature.
			var randNumber = _Rand.Next(0, HalfMaxCounter);
			var buffer = BitConverter.GetBytes(randNumber);

			return (int)((UInt32)buffer[0] << 16 | (UInt32)buffer[1] << 8 | (UInt32)buffer[2]);
		}

		private static byte[] GenerateMachineIdBytes()
		{
			var machineId = 0;
			var machineName = ReadMachineName();
			if (!String.IsNullOrEmpty(machineName))
			{
				using (var md5 = System.Security.Cryptography.MD5.Create())
				{
					machineId = md5.ComputeHash(System.Text.UTF8Encoding.UTF8.GetBytes(machineName)).Sum((b) => (int)b);
				}
			}
			else
				machineId = _Rand.Next(0, UInt16.MaxValue);

			var retVal = new byte[3];
			var rawBytes = BitConverter.GetBytes(machineId);
			retVal[0] = rawBytes[0];
			retVal[1] = rawBytes[2];
			retVal[2] = rawBytes[3];

			return retVal;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2207:InitializeValueTypeStaticFieldsInline", Justification = "Not really possible given the array initailization required here.")]
		static Xid()
		{
			for (var i = 0; i < DecodeMap.Length; i++)
			{
				DecodeMap[i] = 0xFF;
			}

			for (var i = 0; i < Encoding.Length; i++)
			{
				DecodeMap[Encoding[i]] = (byte)i;
			}
		}

		#endregion

		#region Instance Implementation

		internal byte _B1;
		internal byte _B2;
		internal byte _B3;
		internal byte _B4;
		internal byte _B5;
		internal byte _B6;
		internal byte _B7;
		internal byte _B8;
		internal byte _B9;
		internal byte _B10;
		internal byte _B11;
		internal byte _B12;

		/// <summary>
		/// Constructs a new <see cref="Xid"/> using the specified bytes.
		/// </summary>
		/// <param name="rawValues">A byte array containing the values to reconstruct a <see cref="Xid"/> from.</param>
		/// <seealso cref="ToBytes()"/>
		/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="rawValues"/> is null.</exception>
		/// <exception cref="System.ArgumentException">Thrown if the length of <paramref name="rawValues"/> is less than the number of bytes for a Xid.</exception>
		public Xid(byte[] rawValues)
		{
			if (rawValues == null) throw new ArgumentNullException(nameof(rawValues));
			if (rawValues.Length != Length) throw new ArgumentException(String.Format(System.Globalization.CultureInfo.CurrentCulture, ErrorMessages.XidByteArgLength), nameof(rawValues));

			_B1 = rawValues[0];
			_B2 = rawValues[1];
			_B3 = rawValues[2];
			_B4 = rawValues[3];
			_B5 = rawValues[4];
			_B6 = rawValues[5];
			_B7 = rawValues[6];
			_B8 = rawValues[7];
			_B9 = rawValues[8];
			_B10 = rawValues[9];
			_B11 = rawValues[10];
			_B12 = rawValues[11];
		}

		/// <summary>
		/// Constructs a new <see cref="Xid"/> using the specified bytes.
		/// </summary>
		/// <param name="rawValues">A byte array containing the values to reconstruct a <see cref="Xid"/> from.</param>
		/// <param name="offset">The offset to start reading from in <paramref name="rawValues"/> when creating the <see cref="Xid"/>.</param>
		/// <seealso cref="ToBytes()"/>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="rawValues"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown if the length of <paramref name="rawValues"/> less the <paramref name="offset"/> is less than the number of bytes for a Xid.</exception>
		public Xid(byte[] rawValues, int offset)
		{
			if (rawValues == null) throw new ArgumentNullException(nameof(rawValues));
			if (rawValues.Length - offset < Length) throw new ArgumentException(ErrorMessages.XidInsufficientBytes, nameof(rawValues));
			if (int.MaxValue - Length < offset) throw new ArgumentException(ErrorMessages.XidOffsetTooLarge, nameof(rawValues));

			_B1 = rawValues[offset + 0];
			_B2 = rawValues[offset + 1];
			_B3 = rawValues[offset + 2];
			_B4 = rawValues[offset + 3];
			_B5 = rawValues[offset + 4];
			_B6 = rawValues[offset + 5];
			_B7 = rawValues[offset + 6];
			_B8 = rawValues[offset + 7];
			_B9 = rawValues[offset + 8];
			_B10 = rawValues[offset + 9];
			_B11 = rawValues[offset + 10];
			_B12 = rawValues[offset + 11];
		}

		/// <summary>
		/// Constructs a new <see cref="Xid"/> using the specified bytes.
		/// </summary>
		/// <param name="b1">The first byte of the timestamp.</param>
		/// <param name="b2">The second byte of the timestamp.</param>
		/// <param name="b3">The third byte of the timestamp.</param>
		/// <param name="b4">The fourth byte of the timestamp.</param>
		/// <param name="b5">The first byte of the machine id.</param>
		/// <param name="b6">The second byte of the machine id.</param>
		/// <param name="b7">The third byte of the machine id.</param>
		/// <param name="b8">The first byte of the pid.</param>
		/// <param name="b9">The second byte of the pid.</param>
		/// <param name="b10">The first byte of the counter value.</param>
		/// <param name="b11">The second byte of the counter value.</param>
		/// <param name="b12">The third byte of the counter value.</param>
		/// <seealso cref="ToBytes()"/>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b")]
		public Xid(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9, byte b10, byte b11, byte b12)
		{
			_B1 = b1;
			_B2 = b2;
			_B3 = b3;
			_B4 = b4;
			_B5 = b5;
			_B6 = b6;
			_B7 = b7;
			_B8 = b8;
			_B9 = b9;
			_B10 = b10;
			_B11 = b11;
			_B12 = b12;
		}

		/// <summary>
		/// Encodes the value of this <see cref="Xid"/> as a base 32 string.
		/// </summary>
		/// <returns>A string containing the base 32 encoded value of this <see cref="Xid"/>.</returns>
		public override string ToString()
		{
			return ToString(null, null);
		}

		/// <summary>
		/// Returns a new byte array containing the individual bytes that make up this <see cref="Xid"/> value.
		/// </summary>
		/// <returns>A new byte array containing the raw values of this xid.</returns>
		public byte[] ToBytes()
		{
			var retVal = new Byte[Length];
			ToBytes(retVal);
			return retVal;
		}

		/// <summary>
		/// Fills the provided byte array with the values of this xid.
		/// </summary>
		/// <param name="buffer">The byte array to fill.</param>
		public void ToBytes(byte[] buffer)
		{
			ToBytes(buffer, 0);
		}

		/// <summary>
		/// Writes the raw byte values for this Xid to the provided byte array starting at the specified offset.
		/// </summary>
		/// <param name="buffer">The byte array to fill.</param>
		/// <param name="offset">The first index in <paramref name="buffer"/> at which to start writing the <see cref="Xid"/> values.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="buffer"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown if the length of the buffer less the offset is less <see cref="Length"/> of a Xid, or if the offset plus the length of a Xid would overflow an integer value.</exception>
		public void ToBytes(byte[] buffer, int offset)
		{
			if (buffer == null) throw new ArgumentNullException(nameof(buffer));
			if (buffer.Length < offset + Xid.Length) throw new ArgumentException(ErrorMessages.XidInsufficientBytes, nameof(buffer));
			if (int.MaxValue - Length < offset) throw new ArgumentException(ErrorMessages.XidOffsetTooLarge, nameof(offset));

			buffer[offset + 0] = _B1;
			buffer[offset + 1] = _B2;
			buffer[offset + 2] = _B3;
			buffer[offset + 3] = _B4;
			buffer[offset + 4] = _B5;
			buffer[offset + 5] = _B6;
			buffer[offset + 6] = _B7;
			buffer[offset + 7] = _B8;
			buffer[offset + 8] = _B9;
			buffer[offset + 9] = _B10;
			buffer[offset + 10] = _B11;
			buffer[offset + 11] = _B12;
		}

		/// <summary>
		/// Returns the time stamp portion of this Xid as a <see cref="System.DateTime"/> value.
		/// </summary>
		/// <returns>A <see cref="System.DateTime"/> instance representing the creation date &amp; time of the Xid with second accuracy.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Not appropriate, not a simple property.")]
		public DateTime GetTimestamp()
		{
			return new DateTime
			(
				UnixEpochTicks +
				(
					Convert.ToUInt32
					(
						(((uint)_B1 << 24) & 0xFF000000)
								| (((uint)_B2 << 16) & 0x00FF0000)
								| (((uint)_B3 << 8) & 0x0000FF00)
								| ((uint)_B4 & 0x000000FF)
					) * TimeSpan.TicksPerSecond
				),
				DateTimeKind.Utc
			);
		}

		/// <summary>
		/// Returns the 3 byte identifier of the machine that generated this Xid, as a byte array.
		/// </summary>
		/// <returns>A byte array containing the machine identifier.</returns>
		public byte[] GetMachineId()
		{
			return new byte[] { _B5, _B6, _B7 };
		}

		/// <summary>
		/// Returns the 2 byte process id of this Xid as a <see cref="ushort"/>.
		/// </summary>
		/// <returns></returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Not appropriate, not a simple property..")]
		[CLSCompliant(false)]
		public UInt16 GetProcessId()
		{
			return Convert.ToUInt16
			(
				(_B8 << 8) | _B9
			);
		}

		/// <summary>
		/// Returns the 3-byte sequential value portion of this Xid.
		/// </summary>
		/// <returns>An integer containing the value of the counter used to generate this Xid.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Not appropriate, not a simple property.")]
		public Int32 GetCounter()
		{
			return (Int32)((UInt32)(_B10) << 16 | (UInt32)_B11 << 8 | (UInt32)_B12);
		}

		#region IFormattable Implementation		

		/// <summary>
		/// Encodes this <see cref="Xid"/> as a base 32 string.
		/// </summary>
		/// <param name="format">Not currently used.</param>
		/// <param name="formatProvider">Not currently used.</param>
		/// <returns>Return a string containing a base 32 representation of the value of this <see cref="Xid"/>.</returns>
		public string ToString(string format, IFormatProvider formatProvider)
		{
			var dest = new char[EncodedLength];

			dest[0] = Encoding[_B1 >> 3];
			dest[1] = Encoding[(_B2 >> 6) & 0x1F | (_B1 << 2) & 0x1F];
			dest[2] = Encoding[(_B2 >> 1) & 0x1F];
			dest[3] = Encoding[(_B3 >> 4) & 0x1F | (_B2 << 4) & 0x1F];
			dest[4] = Encoding[_B4 >> 7 | (_B3 << 1) & 0x1F];
			dest[5] = Encoding[(_B4 >> 2) & 0x1F];
			dest[6] = Encoding[_B5 >> 5 | (_B4 << 3) & 0x1F];
			dest[7] = Encoding[_B5 & 0x1F];
			dest[8] = Encoding[_B6 >> 3];
			dest[9] = Encoding[(_B7 >> 6) & 0x1F | (_B6 << 2) & 0x1F];
			dest[10] = Encoding[(_B7 >> 1) & 0x1F];
			dest[11] = Encoding[(_B8 >> 4) & 0x1F | (_B7 << 4) & 0x1F];
			dest[12] = Encoding[_B9 >> 7 | (_B8 << 1) & 0x1F];
			dest[13] = Encoding[(_B9 >> 2) & 0x1F];
			dest[14] = Encoding[(_B10 >> 5) | (_B9 << 3) & 0x1F];
			dest[15] = Encoding[_B10 & 0x1F];
			dest[16] = Encoding[_B11 >> 3];
			dest[17] = Encoding[(_B12 >> 6) & 0x1F | (_B11 << 2) & 0x1F];
			dest[18] = Encoding[(_B12 >> 1) & 0x1F];
			dest[19] = Encoding[(_B12 << 4) & 0x1F];

			return new string(dest);
		}

		#endregion

		#region IEquatable<Xid>

		/// <summary>
		/// Compares this <see cref="Xid"/> to <paramref name="other"/> and returns true if they represent the same value.
		/// </summary>
		/// <param name="other">Another <see cref="Xid"/> to compare to.</param>
		/// <returns>True if the <see cref="Xid"/>s represent the same value, otherwise false.</returns>
		public bool Equals(Xid other)
		{
			return _B1 == other._B1
				&& _B2 == other._B2
				&& _B3 == other._B3
				&& _B4 == other._B4
				&& _B5 == other._B5
				&& _B6 == other._B6
				&& _B7 == other._B7
				&& _B8 == other._B8
				&& _B9 == other._B9
				&& _B10 == other._B10
				&& _B11 == other._B11
				&& _B12 == other._B12;
		}

		#endregion

		/// <summary>
		/// Returns true if <paramref name="obj"/> is a <see cref="Xid"/> that has the same value as this instance.
		/// </summary>
		/// <param name="obj">A value to check equality with.</param>
		/// <returns>True if <paramref name="obj"/> is an equal Xid value.</returns>
		public override bool Equals(object obj)
		{
			if (obj == null || !(obj is Xid)) return false;

			return Equals((Xid)obj);
		}

		/// <summary>
		/// Returns the hashcode for this instance.
		/// </summary>
		/// <returns>An integer value containing the hashcode for this instance.</returns>
		public override int GetHashCode()
		{
			unchecked // Overflow is fine, just wrap
			{
				var hash = 17;

				hash = hash * HashMultiplier + _B1;
				hash = hash * HashMultiplier + _B2;
				hash = hash * HashMultiplier + _B3;
				hash = hash * HashMultiplier + _B4;
				hash = hash * HashMultiplier + _B5;
				hash = hash * HashMultiplier + _B6;
				hash = hash * HashMultiplier + _B7;
				hash = hash * HashMultiplier + _B8;
				hash = hash * HashMultiplier + _B9;
				hash = hash * HashMultiplier + _B10;
				hash = hash * HashMultiplier + _B11;
				hash = hash * HashMultiplier + _B12;
				return hash;
			}
		}

		#region IComparable

		/// <summary>
		/// Compares the current instance with another object of the same type and returns
		/// an integer that indicates whether the current instance precedes, follows, or
		/// occurs in the same position in the sort order as the other object.
		/// </summary>
		/// <param name="obj">An object to compare with this instance.</param>
		/// <returns>
		/// A value that indicates the relative order of the objects being compared. The
		/// return value has these meanings: Value Meaning Less than zero This instance precedes
		/// obj in the sort order. Zero This instance occurs in the same position in the
		/// sort order as obj. Greater than zero This instance follows obj in the sort order.		
		/// </returns>
		public int CompareTo(object obj)
		{
			if (obj == null) return 1;
			if (!(obj is Xid)) throw new ArgumentException("obj must be a Xid", nameof(obj));

			return CompareTo((Xid)obj);
		}

		#endregion

		#region IComparable<Xid>

		/// <summary>
		/// Compares the current instance with another value of the same type and returns
		/// an integer that indicates whether the current instance precedes, follows, or
		/// occurs in the same position in the sort order as the other object.
		/// </summary>
		/// <param name="other">An <see cref="Xid"/> to compare with this instance.</param>
		/// <returns>
		/// A value that indicates the relative order of the objects being compared. The
		/// return value has these meanings: Value Meaning Less than zero This instance precedes
		/// obj in the sort order. Zero This instance occurs in the same position in the
		/// sort order as obj. Greater than zero This instance follows obj in the sort order.		
		/// </returns>
		public int CompareTo(Xid other)
		{
			if (other._B1 != this._B1) return CompareUnequalBytes(this._B1, other._B1);
			if (other._B2 != this._B2) return CompareUnequalBytes(this._B2, other._B2);
			if (other._B3 != this._B3) return CompareUnequalBytes(this._B3, other._B3);
			if (other._B4 != this._B4) return CompareUnequalBytes(this._B4, other._B4);
			if (other._B5 != this._B5) return CompareUnequalBytes(this._B5, other._B5);
			if (other._B6 != this._B6) return CompareUnequalBytes(this._B6, other._B6);
			if (other._B7 != this._B7) return CompareUnequalBytes(this._B7, other._B7);
			if (other._B8 != this._B8) return CompareUnequalBytes(this._B8, other._B8);
			if (other._B9 != this._B9) return CompareUnequalBytes(this._B9, other._B9);
			if (other._B10 != this._B10) return CompareUnequalBytes(this._B10, other._B10);
			if (other._B11 != this._B11) return CompareUnequalBytes(this._B11, other._B11);
			if (other._B12 != this._B12) return CompareUnequalBytes(this._B12, other._B12);

			return 0;
		}

#if SUPPORTS_AGGRESSIVEINLINING
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		private static int CompareUnequalBytes(byte me, byte them)
		{
			if (me < them) return -1;

			return 1;
		}

		#endregion

		#endregion

		#region Operators

		/// <summary>
		/// Overloads the == operator to provide value based equality.
		/// </summary>
		/// <param name="a">A <see cref="Xid"/> to equality check.</param>
		/// <param name="b">A <see cref="Xid"/> to equality check.</param>
		/// <returns>True if <paramref name="a"/> and <paramref name="b"/> represent the same value.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a")]
		public static bool operator ==(Xid a, Xid b)
		{
			return a.Equals(b);
		}

		/// <summary>
		/// Overloads the != operator to provide value based inequality.
		/// </summary>
		/// <param name="a">A <see cref="Xid"/> to inequality check.</param>
		/// <param name="b">A <see cref="Xid"/> to inequality check.</param>
		/// <returns>True if <paramref name="a"/> and <paramref name="b"/> do not represent the same value.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a")]
		public static bool operator !=(Xid a, Xid b)
		{
			return !(a == b);
		}

		/// <summary>
		/// Peforms a greater than comparison of <paramref name="a"/> and <paramref name="b"/>.
		/// </summary>
		/// <param name="a">A <see cref="Xid"/> to compare.</param>
		/// <param name="b">A <see cref="Xid"/> to compare.</param>
		/// <returns>True if <paramref name="a"/> is greater than <paramref name="b"/>.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a")]
		public static bool operator >(Xid a, Xid b)
		{
			return a.CompareTo(b) == 1;
		}

		/// <summary>
		/// Peforms a greater than or equal to comparison of <paramref name="a"/> and <paramref name="b"/>.
		/// </summary>
		/// <param name="a">A <see cref="Xid"/> to compare.</param>
		/// <param name="b">A <see cref="Xid"/> to compare.</param>
		/// <returns>True if <paramref name="a"/> is greater than or equal to <paramref name="b"/>.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a")]
		public static bool operator >=(Xid a, Xid b)
		{
			return a.CompareTo(b) >= 0;
		}

		/// <summary>
		/// Peforms a less than comparison of <paramref name="a"/> and <paramref name="b"/>.
		/// </summary>
		/// <param name="a">A <see cref="Xid"/> to compare.</param>
		/// <param name="b">A <see cref="Xid"/> to compare.</param>
		/// <returns>True if <paramref name="a"/> is less than <paramref name="b"/>.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a")]
		public static bool operator <(Xid a, Xid b)
		{
			return a.CompareTo(b) == -1;
		}

		/// <summary>
		/// Peforms a less than or equal to comparison of <paramref name="a"/> and <paramref name="b"/>.
		/// </summary>
		/// <param name="a">A <see cref="Xid"/> to compare.</param>
		/// <param name="b">A <see cref="Xid"/> to compare.</param>
		/// <returns>True if <paramref name="a"/> is less than or equal to <paramref name="b"/>.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "a")]
		public static bool operator <=(Xid a, Xid b)
		{
			return a.CompareTo(b) <= 0;
		}

		#endregion

	}
}