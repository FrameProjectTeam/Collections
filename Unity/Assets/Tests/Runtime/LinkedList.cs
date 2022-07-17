using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Fp.Collections.Tests
{
    public class LinkedList
    {
        private Random _rand;
        private LinkedList<string> _llStr;
        private LinkedList<double> _llDouble;

        [SetUp]
        public void Setup()
        {
            _rand = new Random();

            _llStr = new LinkedList<string>();
            _llDouble = new LinkedList<double>();
        }

        [Test]
        public void AddLastValue()
        {
            int firstNode = _llStr.AddLast("First");
            int secondNode = _llStr.AddLast("Second");
            int thirdNode = _llStr.AddLast("Third");

            Assert.AreEqual(3, _llStr.Count);

            Assert.AreEqual("First", _llStr.GetValue(firstNode));
            Assert.AreEqual("Second", _llStr.GetValue(secondNode));
            Assert.AreEqual("Third", _llStr.GetValue(thirdNode));
        }

        [Test]
        public void AddFirstValue()
        {
            int thirdNode = _llStr.AddFirst("Third");
            int secondNode = _llStr.AddFirst("Second");
            int firstNode = _llStr.AddFirst("First");

            Assert.AreEqual(3, _llStr.Count);

            Assert.AreEqual("First", _llStr.GetValue(firstNode));
            Assert.AreEqual("Second", _llStr.GetValue(secondNode));
            Assert.AreEqual("Third", _llStr.GetValue(thirdNode));
        }

        [Test]
        public void AddRemove()
        {
            int firstNode = _llStr.AddLast("First");
            _llStr.Remove(firstNode);

            Assert.IsFalse(_llStr.HasValue(firstNode));
        }

        [Test]
        public void AddRemoveAdd()
        {
            int firstNode = _llStr.AddLast("First");
            _llStr.Remove(firstNode);

            int secondNode = _llStr.AddLast("Second");

            Assert.AreEqual(1, _llStr.Count);
            Assert.AreEqual(firstNode, secondNode);
            Assert.AreEqual("Second", _llStr.GetValue(secondNode));
        }

        [Test]
        public void CheckFirst()
        {
            _llStr.AddLast("First");
            _llStr.AddLast("Second");
            _llStr.AddLast("Third");

            Assert.AreEqual(3, _llStr.Count);
            Assert.AreEqual("First", _llStr.GetValue(_llStr.FirstIdx));
        }

        [Test]
        public void CheckLast()
        {
            _llStr.AddLast("First");
            _llStr.AddLast("Second");
            _llStr.AddLast("Third");

            Assert.AreEqual(3, _llStr.Count);
            Assert.AreEqual("Third", _llStr.GetValue(_llStr.LastIdx));
        }

        [Test]
        public void RandomOrder()
        {
            _llStr.AddLast("Second");
            _llStr.AddLast("Third");
            _llStr.AddFirst("First");

            Assert.AreEqual(3, _llStr.Count);
            Assert.AreEqual("First", _llStr.GetValue(_llStr.FirstIdx));
            Assert.AreEqual("Third", _llStr.GetValue(_llStr.LastIdx));
        }

        [Test]
        public void RemoveFirst()
        {
            _llStr.AddLast("Second");
            _llStr.AddLast("Third");
            _llStr.AddFirst("First");

            int firstNode = _llStr.FirstIdx;
            _llStr.Remove(firstNode);

            Assert.AreEqual(2, _llStr.Count);
            Assert.AreEqual("Second", _llStr.GetValue(_llStr.FirstIdx));
            Assert.AreEqual("Third", _llStr.GetValue(_llStr.LastIdx));
        }

        [Test]
        public void RemoveLast()
        {
            _llStr.AddLast("Second");
            _llStr.AddLast("Third");
            _llStr.AddFirst("First");

            int lastNode = _llStr.LastIdx;
            _llStr.Remove(lastNode);

            Assert.AreEqual(2, _llStr.Count);
            Assert.AreEqual("First", _llStr.GetValue(_llStr.FirstIdx));
            Assert.AreEqual("Second", _llStr.GetValue(_llStr.LastIdx));
        }

        [Test]
        public void ReplaceValue()
        {
            _llStr.AddLast("InvalidValue");
            int secondNode = _llStr.AddLast("InvalidValue");
            _llStr.AddLast("InvalidValue");

            _llStr.SetValue(_llStr.FirstIdx, "First");
            _llStr.SetValue(secondNode, "Second");
            _llStr.SetValue(_llStr.LastIdx, "Third");

            Assert.AreEqual("First", _llStr.GetValue(_llStr.FirstIdx));
            Assert.AreEqual("Second", _llStr.GetValue(secondNode));
            Assert.AreEqual("Third", _llStr.GetValue(_llStr.LastIdx));
        }

        [Test]
        public void RandomValidation()
        {
            const int valuesCount = 1000;

            var randomizedValues = new HashSet<double>();

            while (randomizedValues.Count < valuesCount)
            {
                double randVal = _rand.NextDouble();
                if (!randomizedValues.Add(randVal))
                {
                    continue;
                }

                if (_rand.NextDouble() > 0.5)
                {
                    _llDouble.AddFirst(randVal);
                }
                else
                {
                    _llDouble.AddLast(randVal);
                }
            }

            randomizedValues.ExceptWith(_llDouble);
            Assert.AreEqual(0, randomizedValues.Count);
        }

        [Test]
        public void InvalidOperation()
        {
            Assert.IsFalse(_llStr.FirstIdx >= 0);
            Assert.IsFalse(_llStr.LastIdx >= 0);
        }

        [Test]
        public void NextNode()
        {
            _llStr.AddLast("Second");
            _llStr.AddLast("Third");
            int firstNode = _llStr.AddFirst("First");

            bool hasSecond = _llStr.TryGetNext(firstNode, out int secondNode);
            bool hasThird = _llStr.TryGetNext(secondNode, out int thirdNode);
            bool hasFourth = _llStr.TryGetNext(thirdNode, out int fourthNode);

            Assert.IsTrue(hasSecond);
            Assert.IsTrue(secondNode >= 0);
            Assert.AreEqual("Second", _llStr.GetValue(secondNode));

            Assert.IsTrue(hasThird);
            Assert.IsTrue(thirdNode >= 0);
            Assert.AreEqual("Third", _llStr.GetValue(thirdNode));

            Assert.IsFalse(hasFourth);
            Assert.IsFalse(fourthNode >= 0);
        }

        [Test]
        public void PreviousNode()
        {
            _llStr.AddLast("Second");
            int thirdNode = _llStr.AddLast("Third");
            _llStr.AddFirst("First");

            bool hasSecond = _llStr.TryGetPrevious(thirdNode, out int secondNode);
            bool hasFirst = _llStr.TryGetPrevious(secondNode, out int firstNode);
            bool hasZero = _llStr.TryGetPrevious(firstNode, out int zeroNode);

            Assert.IsTrue(hasSecond);
            Assert.IsTrue(secondNode >= 0);
            Assert.AreEqual("Second", _llStr.GetValue(secondNode));

            Assert.IsTrue(hasFirst);
            Assert.IsTrue(firstNode >= 0);
            Assert.AreEqual("First", _llStr.GetValue(firstNode));

            Assert.IsFalse(hasZero);
            Assert.IsFalse(zeroNode >= 0);
        }

        [Test]
        public void Clear()
        {
            _llStr.AddLast("First");
            _llStr.AddLast("Second");
            _llStr.AddLast("Third");

            _llStr.ChainClear();

            Assert.AreEqual(0, _llStr.Count);
        }

        [Test]
        public void Swap()
        {
            var indices = new int[2];
            indices[0] = _llStr.AddLast("Second");
            indices[1] = _llStr.AddLast("First");

            _llStr.Swap(indices[0], indices[1]);

            Assert.AreEqual("First", _llStr.GetValue(indices[0]));
            Assert.AreEqual("Second", _llStr.GetValue(indices[1]));
        }

        [Test]
        public void TryGetFirstLast()
        {
            var indices = new int[3];
            indices[0] = _llStr.AddLast("First");
            indices[1] = _llStr.AddLast("Second");
            indices[2] = _llStr.AddLast("Third");

            bool hasFirst = _llStr.TryGetFirst(out int firstIdx);
            bool hasLast = _llStr.TryGetLast(out int lastIdx);
            
            Assert.IsTrue(hasFirst);
            Assert.AreEqual(indices[0], firstIdx);
            Assert.IsTrue(hasLast);
            Assert.AreEqual(indices[2], lastIdx);
        }
        
        [Test]
        public void TryGetFirstLast2()
        {
            var indices = new int[2];
            indices[0] = _llStr.AddLast("First");
            indices[1] = _llStr.AddLast("Third");

            bool hasFirst = _llStr.TryGetFirst(out int firstIdx);
            bool hasLast = _llStr.TryGetLast(out int lastIdx);
            
            Assert.IsTrue(hasFirst);
            Assert.AreEqual(indices[0], firstIdx);
            Assert.IsTrue(hasLast);
            Assert.AreEqual(indices[1], lastIdx);
        }
        
        [Test]
        public void TryGetFirstLast3()
        {
            var indices = new int[1];
            indices[0] = _llStr.AddLast("First");

            bool hasFirst = _llStr.TryGetFirst(out int firstIdx);
            bool hasLast = _llStr.TryGetLast(out int lastIdx);
            
            Assert.IsTrue(hasFirst);
            Assert.IsTrue(hasLast);
            Assert.AreEqual(indices[0], firstIdx);
            Assert.AreEqual(firstIdx, lastIdx);
        }
        
        
        [Test]
        public void TryGetFirstLast4()
        {
            bool hasFirst = _llStr.TryGetFirst(out _);
            bool hasLast = _llStr.TryGetLast(out _);
            
            Assert.IsFalse(hasFirst);
            Assert.IsFalse(hasLast);
        }
        
        [Test]
        public void AddAfterBefore()
        {
            int first = _llStr.AddLast("Second");
            _llStr.AddAfter(first, "Third");
            _llStr.AddBefore(first, "First");
            
            CollectionAssert.AreEqual(new [] {"First", "Second", "Third"}, _llStr);
        }

        [Test]
        public void EnumeratorVersionCheck()
        {
            int first = _llStr.AddLast("Second");
            _llStr.AddAfter(first, "Third");
            _llStr.AddBefore(first, "First");

            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (string s in _llStr)
                {
                    _llStr.Remove(first);
                }
            });
        }
        
        [Test]
        public void Add32Elements()
        {
            for(var i = 0; i < 32; i++)
            {
                _llStr.AddLast(i.ToString());
            }

            Assert.AreEqual(32, _llStr.Count);
            CollectionAssert.AreEqual(Enumerable.Range(0, 32).Select(i => i.ToString()), _llStr);
        }
        
                
        [Test]
        public void CheckCapacityBounds()
        {
            for(var i = 0; i < 32; i++)
            {
                _llStr.AddLast(i.ToString());
            }
            
            _llStr.ChainClear();

            Assert.GreaterOrEqual(_llStr.Capacity, 32);

            Assert.IsFalse(_llStr.HasValue(-1));
            Assert.IsFalse(_llStr.HasValue(0));
            Assert.IsFalse(_llStr.HasValue(32));
            Assert.IsFalse(_llStr.HasValue(_llStr.Capacity - 1));
            Assert.IsFalse(_llStr.HasValue(_llStr.Capacity));
            
            Assert.Throws<IndexOutOfRangeException>(() => _llStr.GetValue(-1));
            Assert.Throws<IndexOutOfRangeException>(() => _llStr.GetValue(_llStr.Capacity));
        }

        [Test]
        public void EnumeratorReset()
        {
            int first = _llStr.AddLast("Second");
            _llStr.AddAfter(first, "Third");
            _llStr.AddBefore(first, "First");

            using LinkedList<string>.Enumerator enumerator = _llStr.GetEnumerator();

            var idx = 0;
            var firstArr = new string[_llStr.Count];
            var secondArr = new string[_llStr.Count];
            
            while (enumerator.MoveNext())
            {
                firstArr[idx++] = enumerator.Current;
            }

            idx = 0;
            enumerator.Reset();
            
            while (enumerator.MoveNext())
            {
                secondArr[idx++] = enumerator.Current;
            }
            
            CollectionAssert.AreEqual(firstArr, secondArr);
            CollectionAssert.AreEqual(firstArr, _llStr);
            CollectionAssert.AreEqual(secondArr, _llStr);
        }
    }
}