using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using XidNet;
using System.Threading.Tasks;

namespace XidNet.Tests
{
	[TestClass]
	public class Xid_Tests
	{

		[TestMethod]
		public void Xid_New_GeneratesXid()
		{
			var xid = Xid.NewXid();
			var t = xid.GetTimestamp();
			var p = xid.GetProcessId();
			var p1 = System.Diagnostics.Process.GetCurrentProcess().Id;
			var c = xid.GetCounter();

			System.Diagnostics.Trace.WriteLine(xid.ToString());
		}

		[TestMethod]
		public void Xid_New_GeneratesSequence()
		{
			Xid previousId = Xid.Empty;
			for (int cnt = 0; cnt < 1000; cnt++)
			{
				var xid = Xid.NewXid();
				if (cnt > 0)
				{
					var secs = xid.GetTimestamp().Subtract(previousId.GetTimestamp()).TotalSeconds;
					Assert.IsTrue(secs >= 0 && secs <= 30);
					Assert.AreEqual(previousId.GetProcessId(), xid.GetProcessId());
					Assert.IsTrue(ArraysAreEqual(previousId.GetMachineId(), xid.GetMachineId()));
					Assert.AreEqual(previousId.GetCounter() + 1, xid.GetCounter());
				}
				System.Diagnostics.Trace.WriteLine(xid.ToString());
				previousId = xid;
			}
		}

		[TestMethod]
		public void Xid_New_GeneratesUniqueXids()
		{
			int maxIterations = 20000000;
			if (IntPtr.Size > 4)
				maxIterations = 40000000; //Moa tests on 64 bit! (32 bit runs out of contiguous memory for dictionary)

			var unique = new Dictionary<Xid, Xid>(maxIterations);
			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			for (int cnt = 0; cnt < maxIterations; cnt++)
			{
				var x = Xid.NewXid();
				unique.Add(x, x);
			}
			sw.Stop();
			System.Diagnostics.Trace.WriteLine(sw.Elapsed);
		}

		[TestMethod]
		public void Xid_New_GeneratesUniqueXids_Threaded()
		{
			int maxIterations = 10000000;

			int threadCount = Math.Max(2, Environment.ProcessorCount);
			int step = maxIterations / threadCount;

			//var unique = new Dictionary<string, Xid>(maxIterations);
			var unique = new Dictionary<Xid, Xid>(maxIterations / 2);

			var threads = new List<System.Threading.Thread>(threadCount);
			var dictionaries = new List<Dictionary<Xid, Xid>>(threadCount);

			for (int tcnt = 0; tcnt < threadCount; tcnt++)
			{
				dictionaries.Add(new Dictionary<Xid, Xid>(step));

				threads.Add
				(
					new System.Threading.Thread
					(
						(o) =>
						{
							var g = dictionaries.Count;
							var oa = (object[])o;

							var i = (int)oa[0];
							var dict = (Dictionary<Xid, Xid>)oa[1];

							int start = i * step;
							int end = start + step;

							for (int cnt = start; cnt < end; cnt++)
							{
								var x = Xid.NewXid();
								dict.Add(x, x);
							}
						}
					)
				);
			}

			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			for (int cnt = 0; cnt < threadCount; cnt++)
			{
				threads[cnt].Start(new object[] { cnt, dictionaries[cnt] });
			}

			foreach (var thread in threads)
			{
				thread.Join();
			}
			sw.Stop();

			foreach (var dict in dictionaries)
			{
				foreach (var kvp in dict)
				{
					unique.Add(kvp.Key, kvp.Key);
				}
			}

			System.Diagnostics.Trace.WriteLine(sw.Elapsed);
			Assert.AreEqual(maxIterations, unique.Count);
		}

		[TestMethod]
		public void Xid_New_NewXidsAreKSorted()
		{
			var lastXid = Xid.Empty;
			for (int cnt = 0; cnt < 1000000; cnt++)
			{
				var x = Xid.NewXid();
				Assert.AreEqual(1, x.CompareTo(lastXid));
				Assert.IsTrue(x > lastXid);
				Assert.AreEqual(-1, lastXid.CompareTo(x));
				Assert.IsTrue(lastXid < x);

				lastXid = x;
			}
		}

		private bool ArraysAreEqual(byte[] v1, byte[] v2)
		{
			if (v1.Length != v2.Length) return false;

			for (int cnt = 0; cnt < v1.Length; cnt++)
			{
				Assert.AreEqual(v1[cnt], v2[cnt]);
			}

			return true;
		}

	}
}
