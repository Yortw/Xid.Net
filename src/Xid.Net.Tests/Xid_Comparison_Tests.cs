using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XidNet.Tests
{
	[TestClass]
	public class Xid_Comparison_Tests
	{

		[TestMethod]
		public void Xid_Empty_IsZero()
		{
			Assert.AreEqual(new string('0', 20), Xid.Empty.ToString());
		}

		[TestMethod]
		public void Xid_Empty_EqualsDefaultValue()
		{
			Assert.AreEqual(Xid.Empty, new Xid());
		}

		[TestMethod]
		public void Xid_Equals_ReturnsTrueWhenEqual()
		{
			var x = Xid.NewXid();
			var y = new Xid(x.ToBytes());
			Assert.AreEqual<Xid>(x, y);
			Assert.AreEqual<string>(x.ToString(), y.ToString());
			Assert.IsTrue(x == y);
		}

		[TestMethod]
		public void Xid_Equals_NotEqualToNull()
		{
			var x = Xid.NewXid();
			Assert.AreNotEqual(x, null);
		}

		[TestMethod]
		public void Xid_Equals_NotEqualToNonXid()
		{
			var x = Xid.NewXid();
			Assert.AreNotEqual(x, new object());
		}

		[TestMethod]
		public void Xid_Equals_ReturnsFalseWhenUnequal()
		{
			var x = Xid.NewXid();
			var y = Xid.Empty;
			Assert.AreNotEqual<Xid>(x, y);
			Assert.AreNotEqual<string>(x.ToString(), y.ToString());
			Assert.IsFalse(x == y);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Xid_CompareToObj_ThrowsOnOtherNotXid()
		{
			var x = Xid.NewXid();
			var y = x.CompareTo(new object());
		}

		[TestMethod]
		public void Xid_CompareToObj_ReturnsNegativeOneWhenLessThan()
		{
			var x = Xid.NewXid();
			var y = Xid.Empty;
			Assert.AreEqual(-1, y.CompareTo((object)x));
		}

		[TestMethod]
		public void Xid_CompareToObj_ReturnsZeroWhenEqual()
		{
			var x = Xid.NewXid();
			var y = Xid.Parse(x.ToString());
			Assert.AreEqual(0, y.CompareTo((object)x));
		}

		[TestMethod]
		public void Xid_CompareToObj_ReturnsOneWhenGreaterThan()
		{
			var x = Xid.NewXid();
			var y = Xid.NewXid();
			Assert.AreEqual(1, y.CompareTo((object)x));
		}

		[TestMethod]
		public void Xid_CompareTo_ReturnsNegativeOneWhenLessThan()
		{
			var x = Xid.NewXid();
			var y = Xid.Empty;
			Assert.AreEqual(-1, y.CompareTo(x));
		}

		[TestMethod]
		public void Xid_CompareTo_ReturnsZeroWhenEqual()
		{
			var x = Xid.NewXid();
			var y = Xid.Parse(x.ToString());
			Assert.AreEqual(0, y.CompareTo(x));
		}

		[TestMethod]
		public void Xid_CompareTo_ReturnsOneWhenGreaterThan()
		{
			var x = Xid.NewXid();
			var y = Xid.NewXid();
			Assert.AreEqual(1, y.CompareTo(x));
		}

	}
}