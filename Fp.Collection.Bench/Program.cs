using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Fp.Collection.Bench
{
	public class Program
	{
		public static void Main(string[] args)
		{
			Summary summary = BenchmarkRunner.Run<ClearBench>();
		}
	}
}