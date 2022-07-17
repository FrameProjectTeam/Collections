using System;

using BenchmarkDotNet.Attributes;

namespace Fp.Collection.Bench
{
	public class ClearBench
	{
		private byte[] _array;

		private Collections.LinkedList<byte> _ll;
		
		[Params(1 << 14, 1 << 15, 1 << 16)]
		public int Count { get; set; }
		
		[IterationSetup]
		public void Setup()
		{
			_array = new byte[Count];
			Random.Shared.NextBytes(_array);

			_ll = new Collections.LinkedList<byte>(Count);

			foreach(byte val in _array)
			{
				if(val < 127)
				{
					_ll.AddLast(val);
				}
			}
		}

		[Benchmark]
		public void Clear()
		{
			_ll.ChainClear();
		}
		
		[Benchmark]
		public void ClearAlt()
		{
			_ll.Clear();
		}
	}
}