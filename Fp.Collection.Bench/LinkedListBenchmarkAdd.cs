using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Fp.Collection.Bench
{
	[Config(typeof(GcVariationConfig))]
	[Orderer(SummaryOrderPolicy.FastestToSlowest)]
	[MemoryDiagnoser]
	public class LinkedListBenchmarkAdd
	{
		private byte[] _data;

		[Params(10_000_000)]
		public int ElementCount;

		[Params(false, true)]
		public bool Prealocated;

		private LinkedList<byte> _linkedList;
		private Collections.LinkedList<byte> _altLinkedList;

		[IterationCleanup]
		public void IterationCleanup()
		{
			_linkedList.Clear();
			_altLinkedList.Clear();
		}

		[GlobalSetup]
		public void Setup()
		{
			_linkedList = new LinkedList<byte>();
			_altLinkedList = new Collections.LinkedList<byte>(Prealocated ? ElementCount : 8);

			_data = new byte[ElementCount];
			new Random(42).NextBytes(_data);
		}

		[Benchmark]
		public void StandardLinkedListAdd()
		{
			for (int i = 0; i < ElementCount; i++)
			{
				if (i % 2 == 0)
				{
					_linkedList.AddLast(_data[i]);
				}
				else
				{
					_linkedList.AddFirst(_data[i]);
				}
			}
		}

		[Benchmark]
		public void YevLinkedListAdd()
		{
			for (int i = 0; i < ElementCount; i++)
			{
				if (i % 2 == 0)
				{
					_altLinkedList.AddLast(_data[i]);
				}
				else
				{
					_altLinkedList.AddFirst(_data[i]);
				}
			}
		}
	}
}