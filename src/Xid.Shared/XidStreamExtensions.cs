using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XidNet
{
	/// <summary>
	/// Provides extensions to <see cref="System.IO.Stream"/> for reading and write <see cref="Xid"/>s.
	/// </summary>
	public static class XidStreamExtensions
	{
		/// <summary>
		/// Writes a <see cref="Xid"/> to a <see cref="System.IO.Stream"/> instance without creating an intermediate buffer.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="xid">A <see cref="Xid"/> instance to write the raw bytes of.</param>
		/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="stream"/> is null.</exception>
		public static void WriteXid(this System.IO.Stream stream, Xid xid)
		{
			if (stream == null) throw new ArgumentNullException(nameof(stream));

			stream.WriteByte(xid._B1);
			stream.WriteByte(xid._B2);
			stream.WriteByte(xid._B3);
			stream.WriteByte(xid._B4);
			stream.WriteByte(xid._B5);
			stream.WriteByte(xid._B6);
			stream.WriteByte(xid._B7);
			stream.WriteByte(xid._B8);
			stream.WriteByte(xid._B9);
			stream.WriteByte(xid._B10);
			stream.WriteByte(xid._B11);
			stream.WriteByte(xid._B12);
		}

		/// <summary>
		/// Reads the next 12 bytes from a stream and creates a new <see cref="Xid"/> instance from them, without creating an intermediate buffer.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="stream"/> is null.</exception>
		/// <exception cref="System.InvalidOperationException">Thrown if there are less than 12 bytes left in the stream. In this case the stream position will have been advanced to the last byte available before the exception is thrown.</exception>
		/// <returns>Returns a <see cref="Xid"/> instance built by reading the next 12 bytes from the <paramref name="stream"/> provided.</returns>
		public static Xid ReadXid(this System.IO.Stream stream)
		{
			if (stream == null) throw new ArgumentNullException(nameof(stream));

			return new Xid
			(
				ReadByteOrError(stream),
				ReadByteOrError(stream),
				ReadByteOrError(stream),
				ReadByteOrError(stream),
				ReadByteOrError(stream),
				ReadByteOrError(stream),
				ReadByteOrError(stream),
				ReadByteOrError(stream),
				ReadByteOrError(stream),
				ReadByteOrError(stream),
				ReadByteOrError(stream),
				ReadByteOrError(stream)
			);
		}

#if SUPPORTS_AGGRESSIVEINLINING
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		private static byte ReadByteOrError(Stream stream)
		{
			var retVal = stream.ReadByte();
			if (retVal < 0) throw new InvalidOperationException(ErrorMessages.UnexpectedEndOfStream);
			return (byte)retVal;
		}

	}
}