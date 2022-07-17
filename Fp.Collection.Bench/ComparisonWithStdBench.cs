using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

namespace Fp.Collection.Bench
{
	[MemoryDiagnoser]
	public class ComparisonWithStdBench
	{
		private Collections.LinkedList<byte> _llfp;
		private System.Collections.Generic.LinkedList<byte> _ll;
		private byte[] _array;

		public int Count => 1 << 14;

		[IterationSetup]
		public void Setup()
		{
			_array = new byte[Count];
			Random.Shared.NextBytes(_array);

			_llfp = new Collections.LinkedList<byte>(Count);
			_ll = new System.Collections.Generic.LinkedList<byte>();

			foreach(byte val in _array)
			{
				_llfp.AddLast(val);
				_ll.AddLast(val);
			}
		}

		[Benchmark]
		public double FpLinked()
		{
			double avg = 1;

			int idx = _llfp.FirstIdx;

			while(idx >= 0)
			{
				avg += _llfp.GetValueRef(ref idx);
				_llfp.MoveNext(ref idx);
			}

			avg /= Count;
			return avg;
		}

		[Benchmark]
		public double StdLinked()
		{
			double avg = 1;

			LinkedListNode<byte> node = _ll.First;

			while(node != null)
			{
				avg += node.Value;
				node = node.Next;
			}

			avg /= Count;
			return avg;
		}

		[Benchmark]
		public double StdArray()
		{
			double avg = 1;

			for(var i = 0; i < _array.Length; i++)
			{
				avg += _array[i];
			}

			avg /= Count;
			return avg;
		}
	}
}