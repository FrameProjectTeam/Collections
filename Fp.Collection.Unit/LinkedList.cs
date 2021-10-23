using System;
using System.Collections.Generic;

using Fp.Collections;

using NUnit.Framework;

namespace Fp.Collection.Unit
{
    public class LinkedList
    {
        private Random _rand;
        private FpLinkedList<string> _llStr;
        private FpLinkedList<double> _llDouble;

        [SetUp]
        public void Setup()
        {
            _rand = new Random();

            _llStr = new FpLinkedList<string>();
            _llDouble = new FpLinkedList<double>();
        }

        [Test]
        public void AddLastValue()
        {
            FpLinkedListNode<string> firstNode = _llStr.AddLast("First");
            FpLinkedListNode<string> secondNode = _llStr.AddLast("Second");
            FpLinkedListNode<string> thirdNode = _llStr.AddLast("Third");

            Assert.AreEqual(3, _llStr.Count);

            Assert.AreEqual("First", firstNode.Value);
            Assert.AreEqual("Second", secondNode.Value);
            Assert.AreEqual("Third", thirdNode.Value);
        }

        [Test]
        public void AddFirstValue()
        {
            FpLinkedListNode<string> thirdNode = _llStr.AddFirst("Third");
            FpLinkedListNode<string> secondNode = _llStr.AddFirst("Second");
            FpLinkedListNode<string> firstNode = _llStr.AddFirst("First");

            Assert.AreEqual(3, _llStr.Count);

            Assert.AreEqual("First", firstNode.Value);
            Assert.AreEqual("Second", secondNode.Value);
            Assert.AreEqual("Third", thirdNode.Value);
        }

        [Test]
        public void AddRemove()
        {
            FpLinkedListNode<string> firstNode = _llStr.AddLast("First");
            _llStr.Remove(in firstNode);

            Assert.Throws<InvalidOperationException>(() =>
            {
                string _ = firstNode.Value;
            });
        }

        [Test]
        public void AddRemoveAdd()
        {
            FpLinkedListNode<string> firstNode = _llStr.AddLast("First");
            _llStr.Remove(in firstNode);

            FpLinkedListNode<string> secondNode = _llStr.AddLast("Second");

            Assert.AreEqual(1, _llStr.Count);
            Assert.AreEqual("Second", secondNode.Value);
            Assert.Throws<InvalidOperationException>(() =>
            {
                string _ = firstNode.Value;
            });
        }

        [Test]
        public void InvalidNode()
        {
            FpLinkedListNode<string> invalidNode = default;
            FpLinkedListNode<string> invalidNode2 = FpLinkedListNode<string>.Invalid;

            Assert.Throws<InvalidOperationException>(() => { _llStr.GetValue(in invalidNode); });
            Assert.Throws<InvalidOperationException>(() => { _llStr.GetValue(in invalidNode2); });
        }

        [Test]
        public void CheckFirst()
        {
            _llStr.AddLast("First");
            _llStr.AddLast("Second");
            _llStr.AddLast("Third");

            Assert.AreEqual(3, _llStr.Count);
            Assert.AreEqual("First", _llStr.First.Value);
        }

        [Test]
        public void CheckLast()
        {
            _llStr.AddLast("First");
            _llStr.AddLast("Second");
            _llStr.AddLast("Third");

            Assert.AreEqual(3, _llStr.Count);
            Assert.AreEqual("Third", _llStr.Last.Value);
        }

        [Test]
        public void RandomOrder()
        {
            _llStr.AddLast("Second");
            _llStr.AddLast("Third");
            _llStr.AddFirst("First");

            Assert.AreEqual(3, _llStr.Count);
            Assert.AreEqual("First", _llStr.First.Value);
            Assert.AreEqual("Third", _llStr.Last.Value);
        }

        [Test]
        public void RemoveFirst()
        {
            _llStr.AddLast("Second");
            _llStr.AddLast("Third");
            _llStr.AddFirst("First");

            FpLinkedListNode<string> firstNode = _llStr.First;
            _llStr.Remove(in firstNode);

            Assert.AreEqual(2, _llStr.Count);
            Assert.AreEqual("Second", _llStr.First.Value);
            Assert.AreEqual("Third", _llStr.Last.Value);
        }

        [Test]
        public void RemoveLast()
        {
            _llStr.AddLast("Second");
            _llStr.AddLast("Third");
            _llStr.AddFirst("First");

            FpLinkedListNode<string> lastNode = _llStr.Last;
            _llStr.Remove(in lastNode);

            Assert.AreEqual(2, _llStr.Count);
            Assert.AreEqual("First", _llStr.First.Value);
            Assert.AreEqual("Second", _llStr.Last.Value);
        }

        [Test]
        public void ReplaceValue()
        {
            _llStr.AddLast("InvalidValue");
            FpLinkedListNode<string> secondNode = _llStr.AddLast("InvalidValue");
            _llStr.AddLast("InvalidValue");

            _llStr.SetValue(_llStr.First, "First");
            _llStr.SetValue(secondNode, "Second");
            _llStr.SetValue(_llStr.Last, "Third");

            Assert.AreEqual("First", _llStr.First.Value);
            Assert.AreEqual("Second", secondNode.Value);
            Assert.AreEqual("Third", _llStr.Last.Value);
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
            Assert.IsFalse(_llStr.First.IsValid);
            Assert.IsFalse(_llStr.Last.IsValid);
        }

        [Test]
        public void NextNode()
        {
            _llStr.AddLast("Second");
            _llStr.AddLast("Third");
            FpLinkedListNode<string> firstNode = _llStr.AddFirst("First");

            bool hasSecond = _llStr.TryGetNextNode(in firstNode, out FpLinkedListNode<string> secondNode);
            bool hasThird = _llStr.TryGetNextNode(in secondNode, out FpLinkedListNode<string> thirdNode);
            bool hasFourth = _llStr.TryGetNextNode(in thirdNode, out FpLinkedListNode<string> fourthNode);

            Assert.IsTrue(hasSecond);
            Assert.IsTrue(secondNode.IsValid);
            Assert.AreEqual("Second", secondNode.Value);

            Assert.IsTrue(hasThird);
            Assert.IsTrue(thirdNode.IsValid);
            Assert.AreEqual("Third", thirdNode.Value);

            Assert.IsFalse(hasFourth);
            Assert.IsFalse(fourthNode.IsValid);
            Assert.Throws<InvalidOperationException>(() =>
            {
                string _ = fourthNode.Value;
            });
        }

        [Test]
        public void PreviousNode()
        {
            _llStr.AddLast("Second");
            FpLinkedListNode<string> thirdNode = _llStr.AddLast("Third");
            _llStr.AddFirst("First");

            bool hasSecond = _llStr.TryGetPreviousNode(in thirdNode, out FpLinkedListNode<string> secondNode);
            bool hasFirst = _llStr.TryGetPreviousNode(in secondNode, out FpLinkedListNode<string> firstNode);
            bool hasZero = _llStr.TryGetPreviousNode(in firstNode, out FpLinkedListNode<string> zeroNode);

            Assert.IsTrue(hasSecond);
            Assert.IsTrue(secondNode.IsValid);
            Assert.AreEqual("Second", secondNode.Value);

            Assert.IsTrue(hasFirst);
            Assert.IsTrue(firstNode.IsValid);
            Assert.AreEqual("First", firstNode.Value);

            Assert.IsFalse(hasZero);
            Assert.IsFalse(zeroNode.IsValid);
            Assert.Throws<InvalidOperationException>(() =>
            {
                string _ = zeroNode.Value;
            });
        }

        [Test]
        public void Clear()
        {
            _llStr.AddLast("First");
            _llStr.AddLast("Second");
            _llStr.AddLast("Third");

            _llStr.Clear();

            Assert.AreEqual(0, _llStr.Count);
        }

        [Test]
        public void AddAfterBefore()
        {
            FpLinkedListNode<string> first = _llStr.AddLast("Second");
            _llStr.AddAfter(in first, "Third");
            _llStr.AddBefore(in first, "First");
            
            CollectionAssert.AreEqual(new [] {"First", "Second", "Third"}, _llStr);
        }

        [Test]
        public void EnumeratorVersionCheck()
        {
            FpLinkedListNode<string> first = _llStr.AddLast("Second");
            _llStr.AddAfter(in first, "Third");
            _llStr.AddBefore(in first, "First");

            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (string s in _llStr)
                {
                    _llStr.Remove(in first);
                }
            });
        }

        [Test]
        public void EnumeratorReset()
        {
            FpLinkedListNode<string> first = _llStr.AddLast("Second");
            _llStr.AddAfter(in first, "Third");
            _llStr.AddBefore(in first, "First");

            FpLinkedList<string>.Enumerator enumerator = _llStr.GetEnumerator();

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