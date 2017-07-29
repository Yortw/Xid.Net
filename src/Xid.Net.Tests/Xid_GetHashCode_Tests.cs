using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XidNet.Tests
{
	[TestClass]
	public class Xid_GetHashCode_Tests
	{

		[TestMethod]
		public void Xid_GetHashCode_EqualCodesForEqualValues()
		{
			var maxIterations = 1000000;
			for (int cnt = 0; cnt < maxIterations; cnt++)
			{
				var x = Xid.NewXid();
				var y = Xid.Parse(x.ToString());
				Assert.AreEqual(x.GetHashCode(), y.GetHashCode());
			}
		}

		[TestMethod]
		public void Xid_GetHashCode_UnequalCodesForUnequalValues()
		{
			var maxIterations = 1000000;
			for (int cnt = 0; cnt < maxIterations; cnt++)
			{
				var x = Xid.NewXid();
				var y = Xid.NewXid();
				Assert.AreNotEqual(x.GetHashCode(), y.GetHashCode());
			}
		}

	}
}