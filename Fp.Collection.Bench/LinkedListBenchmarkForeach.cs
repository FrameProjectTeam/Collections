using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Fp.Collection.Bench
{
    [Config(typeof(GcVariationConfig))]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [MemoryDiagnoser]
    public class LinkedListBenchmarkForeach
    {
        private byte[] _data;

        [Params(1_000_000)]
        public int ElementCount;

        [Params(false, true)]
        public bool Prealocated;

        private LinkedList<byte> _linkedList;
        private Collections.LinkedList<byte> _altLinkedList;

        [GlobalSetup]
        public void Setup()
        {
            _linkedList = new LinkedList<byte>();
            _altLinkedList = new Collections.LinkedList<byte>(Prealocated ? ElementCount : 8);

            _data = new byte[ElementCount];
            new Random(42).NextBytes(_data);

            for (int i = 0; i < ElementCount; i++)
            {
                if (i % 2 == 0)
                {
                    _linkedList.AddLast(_data[i]);
                    _altLinkedList.AddLast(_data[i]);
                }
                else
                {
                    _linkedList.AddFirst(_data[i]);
                    _altLinkedList.AddFirst(_data[i]);
                }
            }
        }

        [Benchmark]
        public byte StandardLinkedListForeach()
        {
            byte hex = 0;

            foreach (byte item in _linkedList)
            {
                hex ^= item;
            }

            return hex;
        }
        
        
        [Benchmark]
        public byte StandardLinkedListFor()
        {
            byte hex = 0;

            var node = _linkedList.First;

            while(node != null)
            {
                hex ^= node.Value;
                node = node.Next;
            }
            
            return hex;
        }

        [Benchmark]
        public byte YevLinkedListForeach()
        {
            byte hex = 0;

            foreach (byte item in _altLinkedList)
            {
                hex ^= item;
            }

            return hex;
        }
        
        [Benchmark]
        public byte YevLinkedListFor()
        {
            byte hex = 0;

            int idx = _altLinkedList.FirstIdx;

            while(idx >= 0)
            {
                hex ^= _altLinkedList.GetValue(idx);
                _altLinkedList.MoveNext(ref idx);
            }
            
            return hex;
        }
    }
}