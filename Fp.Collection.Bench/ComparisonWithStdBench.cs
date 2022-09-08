using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Fp.Collection.Bench
{
	public enum FillType
	{
		Linear = 0,
		PingPong = 1,
		Random = 2,
	}
	
	[Orderer(SummaryOrderPolicy.FastestToSlowest)]
	[MemoryDiagnoser]
	public class ComparisonWithStdBench
	{
		private Collections.LinkedList<byte> _llfp;
		private LinkedList<byte> _ll;
		private byte[] _array;

		[Params(1_000)]
		public int Count { get; set; }

		[Params(FillType.Linear, FillType.PingPong, FillType.Random)]
		public FillType Fill { get; set; }
		
		[IterationSetup]
		public void Setup()
		{
			_array = new byte[Count];
			Random.Shared.NextBytes(_array);

			_llfp = new Collections.LinkedList<byte>(Count);
			_ll = new LinkedList<byte>();

			for(var i = 0; i < _array.Length; i++)
			{
				byte val = _array[i];
				switch(Fill)
				{
					case FillType.Linear:
					{
						_llfp.AddLast(val);
						_ll.AddLast(val);
						break;
					}
					case FillType.PingPong:
					{
						if(i % 2 == 0)
						{
							_llfp.AddLast(val);
							_ll.AddLast(val);	
						}
						else
						{
							_llfp.AddFirst(val);
							_ll.AddFirst(val);
						}
						break;
					}
					case FillType.Random:
					{
						if(i < 2)
						{
							_llfp.AddLast(val);
							_ll.AddLast(val);	
						}
						else
						{
							LinkedListNode<byte> targetNode = _ll.First;
							LinkedListNode<byte> node = _ll.First;

							for(var j = 2; node != null; j++)
							{
								if((Random.Shared.Next() % j) == 0)
								{
									targetNode = node;
								}

								node = node.Next;
							}

							_ll.AddAfter(targetNode, val);
							
							int targetIdx = _llfp.FirstIdx;
							int curIdx = _llfp.FirstIdx;

							for(var j = 2; curIdx >= 0; j++)
							{
								if((Random.Shared.Next() % j) == 0)
								{
									targetIdx = curIdx;
								}

								_llfp.MoveNext(ref curIdx);
							}
							_llfp.AddAfter(targetIdx, val);
						}
						
						break;
					}
				}
			}
		}

		[Benchmark]
		public double FpLinked()
		{
			double avg = 1;

			int idx = _llfp.FirstIdx;

			while(idx >= 0)
			{
				avg += _llfp.GetValue(ref idx);
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
	}
}