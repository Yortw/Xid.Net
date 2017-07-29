using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XidNet.Tests
{
	[TestClass]
	public class Xid_Parse_Tests
	{

		[TestMethod]
		public void Xid_Parse_ParsesXidToStringToSameValueXid()
		{
			var x = Xid.NewXid();
			var y = Xid.Parse(x.ToString());

			Assert.AreEqual(x, y);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Xid_Parse_ThrowsOnNullString()
		{
			var y = Xid.Parse(null);

			Assert.Fail();
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Xid_Parse_ThrowsOnEmptyString()
		{
			var y = Xid.Parse(String.Empty);

			Assert.Fail();
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Xid_Parse_ThrowsOnShortString()
		{
			var y = Xid.Parse("9m4e2mr");

			Assert.Fail();
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Xid_Parse_ThrowsOnLongString()
		{
			var y = Xid.Parse("9m4e2mr0ui3e8a215n4g9m4e2mr0ui3e8a215n4g");

			Assert.Fail();
		}

		[TestMethod]
		public void Xid_Parse_ParsesValueToExpectedBytes()
		{
			var x = Xid.Parse("9m4e2mr0ui3e8a215n4g");
			var y = new Xid(new byte[] { 0x4d, 0x88, 0xe1, 0x5b, 0x60, 0xf4, 0x86, 0xe4, 0x28, 0x41, 0x2d, 0xc9 });
			Assert.AreEqual(x, y);
			Assert.AreEqual("9m4e2mr0ui3e8a215n4g", y.ToString());
			Assert.AreEqual(4271561, x.GetCounter());
			Assert.AreEqual(63436413019, x.GetTimestamp().Subtract(DateTime.MinValue).TotalSeconds);
			Assert.AreEqual(0xe428, x.GetProcessId());
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Xid_Parse_ThrowsOnInvalidCharacters()
		{
			var y = Xid.Parse("9m4e2mrZui3e8a215n4g");

			Assert.Fail();
		}


		[TestMethod]
		public void Xid_TryParse_ParsesValidXid()
		{
			var s = "9m4e2mr0ui3e8a215n4g";
			var ok = Xid.TryParse(s, out Xid x);
			Assert.IsTrue(ok);
			Assert.AreEqual(Xid.Parse(s), x);
		}

		[TestMethod]
		public void Xid_TryParse_ReturnsFalseOnNullString()
		{
			string s = null;
			var ok = Xid.TryParse(s, out Xid x);
			Assert.IsFalse(ok);
		}

		[TestMethod]
		public void Xid_TryParse_ReturnsFalseOnEmptyString()
		{
			string s = String.Empty;
			var ok = Xid.TryParse(s, out Xid x);
			Assert.IsFalse(ok);
		}

		[TestMethod]
		public void Xid_TryParse_ReturnsFalseOnShortString()
		{
			string s = "9m4e2mr0ui3e8a215g";
			var ok = Xid.TryParse(s, out Xid x);
			Assert.IsFalse(ok);
		}

		[TestMethod]
		public void Xid_TryParse_ReturnsFalseOnLongString()
		{
			string s = "9m4e2mr0ui3e8a215n4g9m4e2mr0ui3e8a215n4g";
			var ok = Xid.TryParse(s, out Xid x);
			Assert.IsFalse(ok);
		}

		[TestMethod]
		public void Xid_TryParse_ReturnsFalseWhenStringNotValidXid()
		{
			string s = "9m4e2mrZui3e8a215n4g";
			var ok = Xid.TryParse(s, out Xid x);
			Assert.IsFalse(ok);
		}


	}
}