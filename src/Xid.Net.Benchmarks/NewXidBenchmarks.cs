using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XidNet;

namespace XidNet.Benchmarks
{
	public class NewXidBenchmarks
	{
		[Benchmark]
		public Xid CreateXidBenchmark() => Xid.NewXid();

		[Benchmark]
		public Guid CreateGuidBenchmark() => Guid.NewGuid();
	}
}