using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XidNet.Tests
{
	[TestClass]
	public class Xid_Conversion_Tests
	{

		[TestMethod]
		public void Xid_ToByte_Creates12ByteArray()
		{
			var x = Xid.NewXid().ToBytes();
			Assert.IsNotNull(x);
			Assert.AreEqual(12, x.Length);
			int value = 0;
			for (int cnt = 0; cnt < x.Length; cnt++)
			{
				value += x[cnt];
			}

			Assert.IsTrue(value > 0);
		}

		[TestMethod]
		public void Xid_ToByte_FillsBuffer()
		{
			var buffer = new byte[12];
			var x = Xid.NewXid();
			x.ToBytes(buffer);
			var y = new Xid(buffer);
			Assert.AreEqual(x, y);
		}

		[TestMethod]
		public void Xid_ToByte_FillsBufferAtOffset()
		{
			var buffer = new byte[100];
			var x = Xid.NewXid();
			var buffer2 = x.ToBytes();
			x.ToBytes(buffer, 50);
			for (int i = 50; i < 62; i++)
			{
				Assert.AreEqual(buffer[i], buffer2[i - 50]);
			}
		}


		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void Xid_ToByte_ThrowsOnNullBuffer()
		{
			var x = Xid.NewXid();
			x.ToBytes(null);
		}

		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void Xid_ToByte_ThrowsOnBufferTooSmall()
		{
			var buffer = new byte[11];
			var x = Xid.NewXid();
			x.ToBytes(buffer);
		}

		[ExpectedException(typeof(ArgumentException))]
		[TestMethod]
		public void Xid_ToByte_ThrowsOnBufferTooSmallDueToOffset()
		{
			var buffer = new byte[100];
			var x = Xid.NewXid();
			x.ToBytes(buffer, 95);
		}


		[TestMethod]
		public void Xid_ToString_CreatesCorrectLengthString()
		{
			for (int cnt = 0; cnt < 100000; cnt++)
			{
				var x = Xid.NewXid();
				var s = x.ToString();
				Assert.AreEqual(Xid.EncodedLength, s.Length);
			}
		}

		[TestMethod]
		public void Xid_ToString_IgnoresFormatAndCulture()
		{
			var x = Xid.NewXid();
			var s = x.ToString();
			var s2 = x.ToString("X", System.Globalization.CultureInfo.CurrentCulture);
			Assert.AreEqual(s, s2);
		}

		[TestMethod]
		public void Xid_ToString_ParsesBackToOriginalValue()
		{
			var x = Xid.NewXid();
			var s = x.ToString();
			var y = Xid.Parse(s);
			Assert.AreEqual(x, y);
		}

	}
}