using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XidNet.Tests
{
	[TestClass]
	public class Xid_Performance_Tests
	{
		[TestMethod]
		[Microsoft.VisualStudio.TestTools.UnitTesting.Timeout(2700)]
		public void Xid_New_Performance()
		{
			var iterations = 20000000;
			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			for (var cnt = 0; cnt < iterations; cnt++)
			{
				var x = Xid.NewXid();
			}
			sw.Stop();
			System.Diagnostics.Trace.WriteLine(sw.Elapsed);
			System.Diagnostics.Trace.WriteLine(((Convert.ToDouble(sw.Elapsed.TotalMilliseconds * TimeSpan.TicksPerMillisecond) / Convert.ToDouble(iterations)) * 100).ToString("#0.00") + "ns per op");
		}
	}
}