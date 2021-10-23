using System;
using System.Collections.Generic;

namespace Fp.Collections
{
    public class BiMap<TFirst, TSecond> : BiMap<TFirst, TSecond, IEqualityComparer<TFirst>, IEqualityComparer<TSecond>>
    {
        public BiMap() : this(0) { }
        public BiMap(int initialCapacity) : base(initialCapacity, EqualityComparer<TFirst>.Default, EqualityComparer<TSecond>.Default) { }
    }

    public class BiMap<TFirst, TSecond, TFirstComparer, TSecondComparer>
        where TFirstComparer : IEqualityComparer<TFirst>
        where TSecondComparer : IEqualityComparer<TSecond>
    {
        private readonly InternalDictionary<TFirst, TSecond, TFirstComparer, TSecondComparer> _forward;
        private readonly InternalDictionary<TSecond, TFirst, TSecondComparer, TFirstComparer> _backward;

        public BiMap(TFirstComparer firstComparer, TSecondComparer secondComparer) : this(0, firstComparer, secondComparer) { }

        public BiMap(int initialCapacity, TFirstComparer forwardComparer, TSecondComparer backwardComparer)
        {
            _forward = new InternalDictionary<TFirst, TSecond, TFirstComparer, TSecondComparer>(initialCapacity, forwardComparer);
            _backward = new InternalDictionary<TSecond, TFirst, TSecondComparer, TFirstComparer>(initialCapacity, backwardComparer);
            _forward.SetBackward(_backward);
            _backward.SetBackward(_forward);
        }

        public IDictionary<TFirst, TSecond> Forward => _forward;
        public IDictionary<TSecond, TFirst> Backward => _backward;

        public int Count => Forward.Count;

        private sealed class InternalDictionary<TKey, TValue, TForwardComparer, TBackwardComparer> : FpDictionary<TKey, TValue, TForwardComparer>
            where TForwardComparer : IEqualityComparer<TKey>
            where TBackwardComparer : IEqualityComparer<TValue>
        {
            private InternalDictionary<TValue, TKey, TBackwardComparer, TForwardComparer> _bwdDictionary;

            public InternalDictionary(TForwardComparer comparer) : base(comparer) { }
            public InternalDictionary(int capacity, TForwardComparer comparer) : base(capacity, comparer) { }
            public InternalDictionary(IDictionary<TKey, TValue> dictionary, TForwardComparer comparer) : base(dictionary, comparer) { }

            public void SetBackward(InternalDictionary<TValue, TKey, TBackwardComparer, TForwardComparer> backward)
            {
                Assert.IsNotNull(backward);
                Assert.IsTrue(Count == 0);
                Assert.IsTrue(backward.Count == 0);

                _bwdDictionary = backward;
            }

            public override bool TryAdd(in TKey key, TValue value)
            {
                if (TryInsertInternal(key, value, out _))
                {
                    if (_bwdDictionary.TryInsertInternal(value, key, out _))
                    {
                        return true;
                    }

                    Remove(key);
                }

                return false;
            }

            public override bool TryGetAndRemove(in TKey key, out TValue value)
            {
                return base.TryGetAndRemove(in key, out value) && _bwdDictionary.Remove(value);
            }

            public override void Clear()
            {
                _bwdDictionary.Clear();
                base.Clear();
            }

            protected override void Insert(in TKey key, TValue value, bool add, out int entry)
            {
                if (TryInsertInternal(key, value, out entry))
                {
                    if (_bwdDictionary.TryInsertInternal(value, key, out int bwdEntry))
                    {
                        return;
                    }

                    if (add)
                    {
                        //If can't add in backward remove already added item from forward
                        TryGetAndRemoveInternal(key, out _);
                        throw new ArgumentException($"Cant add duplicate key {value}");
                    }

                    //Remove old forward entry
                    TKey bwdOldValue = _bwdDictionary.GetValueByEntry(bwdEntry);
                    TryGetAndRemoveInternal(bwdOldValue, out _);

                    //Replace backward entry
                    TKey bwdNewValue = key;
                    _bwdDictionary.ReplaceValueByEntry(ref bwdEntry, ref bwdNewValue);

                    return;
                }

                if (add)
                {
                    throw new ArgumentException($"Cant add duplicate key {key}");
                }

                //Remove old backward entry
                ref TValue fwdValue = ref GetValueByEntry(entry);
                _bwdDictionary.TryGetAndRemoveInternal(fwdValue, out _);

                //Replace backward entry
                _bwdDictionary.TryInsertInternal(value, key, out _);
                //Replace forward entry
                fwdValue = value;
            }
        }
    }
}