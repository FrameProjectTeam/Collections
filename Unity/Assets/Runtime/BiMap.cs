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
        private readonly InternalMap<TFirst, TSecond, TFirstComparer, TSecondComparer> _forward;
        private readonly InternalMap<TSecond, TFirst, TSecondComparer, TFirstComparer> _backward;

        public BiMap(TFirstComparer firstComparer, TSecondComparer secondComparer) : this(0, firstComparer, secondComparer) { }

        public BiMap(int initialCapacity, TFirstComparer forwardComparer, TSecondComparer backwardComparer)
        {
            _forward = new InternalMap<TFirst, TSecond, TFirstComparer, TSecondComparer>(initialCapacity, forwardComparer);
            _backward = new InternalMap<TSecond, TFirst, TSecondComparer, TFirstComparer>(initialCapacity, backwardComparer);
            _forward.SetBackward(_backward);
            _backward.SetBackward(_forward);
        }

        public IDictionary<TFirst, TSecond> Forward => _forward;
        public IDictionary<TSecond, TFirst> Backward => _backward;

        public int Count => Forward.Count;

        private sealed class InternalMap<TKey, TValue, TForwardComparer, TBackwardComparer> : Map<TKey, TValue, TForwardComparer>
            where TForwardComparer : IEqualityComparer<TKey>
            where TBackwardComparer : IEqualityComparer<TValue>
        {
            private InternalMap<TValue, TKey, TBackwardComparer, TForwardComparer> _bwdMap;

            public InternalMap(TForwardComparer comparer) : base(comparer) { }
            public InternalMap(int capacity, TForwardComparer comparer) : base(capacity, comparer) { }
            public InternalMap(IDictionary<TKey, TValue> dictionary, TForwardComparer comparer) : base(dictionary, comparer) { }

            public void SetBackward(InternalMap<TValue, TKey, TBackwardComparer, TForwardComparer> backward)
            {
                Assert.IsNotNull(backward);
                Assert.IsTrue(Count == 0);
                Assert.IsTrue(backward.Count == 0);

                _bwdMap = backward;
            }

            public override bool TryAdd(in TKey key, TValue value)
            {
                if (TryInsert(key, value, out _))
                {
                    if (_bwdMap.TryInsert(value, key, out _))
                    {
                        return true;
                    }

                    Remove(key);
                }

                return false;
            }

            public override bool TryRemove(in TKey key, out TValue value)
            {
                return base.TryRemove(in key, out value) && _bwdMap.Remove(value);
            }

            public override void Clear()
            {
                _bwdMap.Clear();
                base.Clear();
            }

            protected override void ReplaceOrInsert(in TKey key, TValue value)
            {
                if (TryInsert(key, value, out int entry))
                {
                    if (_bwdMap.TryInsert(value, key, out int bwdEntry))
                    {
                        return;
                    }

                    //Remove old forward entry
                    TKey bwdOldValue = _bwdMap.GetValueByEntry(bwdEntry);
                    base.TryRemove(bwdOldValue, out _);

                    //Replace backward entry
                    TKey bwdNewValue = key;
                    _bwdMap.ReplaceValueByEntry(ref bwdEntry, ref bwdNewValue);

                    return;
                }

                //Remove old backward entry
                ref TValue fwdValue = ref GetValueByEntry(entry);
                _bwdMap.TryRemove(fwdValue, out _);

                //Replace backward entry
                _bwdMap.TryInsert(value, key, out _);
                //Replace forward entry
                fwdValue = value;
            }

            protected override void Insert(in TKey key, TValue value, out int entry)
            {
                if(!TryInsert(key, value, out entry))
                {
                    throw new ArgumentException($"Cant add duplicate key {key}");
                }

                if (_bwdMap.TryInsert(value, key, out _))
                {
                    return;
                }

                //If can't add in backward remove already added item from forward
                base.TryRemove(key, out _);
                throw new ArgumentException($"Cant add duplicate key {value}");
            }
        }
    }
}