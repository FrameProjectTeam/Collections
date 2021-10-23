using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Fp.Collections
{
    [DebuggerDisplay("Count = {Count}, Capacity = {Capacity}")]
    public sealed class FpLinkedList<T> : ILinkedList<T>, IEnumerable<T>
    {
        private int _first = -1;
        private int _last = -1;

        private int _freeList = -1;
        private int _freeCount;

        private int _count;

        private LinkedListEntry[] _entries;

        private int _version;

        public FpLinkedList(int initialCapacity = 8)
        {
            Capacity = Math.Max(initialCapacity, 1);

            _entries = new LinkedListEntry[initialCapacity];
        }

        private bool IsFull => _count == Capacity;

#region IEnumerable Implementation

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#endregion

#region IEnumerable<T> Implementation

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

#endregion

#region ILinkedList<T> Implementation

        public int Count => _count - _freeCount;
        public int Capacity { get; private set; }

        public FpLinkedListNode<T> First => TryGetFirst(out FpLinkedListNode<T> node) ? node : FpLinkedListNode<T>.Invalid;

        public FpLinkedListNode<T> Last => TryGetLast(out FpLinkedListNode<T> node) ? node : FpLinkedListNode<T>.Invalid;

        public void Remove(in FpLinkedListNode<T> node)
        {
            ValidateNode(in node);

            int next = _entries[node.Idx].Next;
            int prev = _entries[node.Idx].Previous;

            if (next < 0)
            {
                _last = prev;
            }
            else //if has next node
            {
                _entries[next].Previous = prev;
            }

            if (prev < 0)
            {
                _first = next;
            }
            else // if has prev node
            {
                _entries[prev].Next = next;
            }

            GivePlace(node.Idx);
        }

        public FpLinkedListNode<T> AddFirst(T value)
        {
            AddFirst(value, out FpLinkedListNode<T> node);
            return node;
        }

        public void AddFirst(T value, out FpLinkedListNode<T> node)
        {
            node = default;

            TakePlace(out int idx);

            if (InitNode(value, idx, ref node))
            {
                return;
            }

            _entries[_first].Previous = idx;

            _entries[idx].Value = value;
            _entries[idx].Previous = -1;
            _entries[idx].Next = _first;
            _entries[idx].Version = ++_version;

            _first = idx;

            node = new FpLinkedListNode<T>(this, idx, _version);
        }

        public FpLinkedListNode<T> AddLast(T value)
        {
            AddLast(value, out FpLinkedListNode<T> node);
            return node;
        }

        public void AddLast(T value, out FpLinkedListNode<T> node)
        {
            node = default;

            TakePlace(out int idx);

            if (InitNode(value, idx, ref node))
            {
                return;
            }

            _entries[_last].Next = idx;

            _entries[idx].Value = value;
            _entries[idx].Previous = _last;
            _entries[idx].Next = -1;
            _entries[idx].Version = ++_version;

            _last = idx;

            node = new FpLinkedListNode<T>(this, idx, _version);
        }

        public void AddAfter(in FpLinkedListNode<T> node, T value, out FpLinkedListNode<T> insertedNode)
        {
            ValidateNode(in node);
            
            TakePlace(out int idx);

            if (_last == node.Idx)
            {
                _last = idx;
            }
            
            _entries[idx].Value = value;
            _entries[idx].Previous = node.Idx;
            _entries[idx].Next = _entries[node.Idx].Next;
            _entries[idx].Version = ++_version;

            _entries[node.Idx].Next = idx;
            
            insertedNode = new FpLinkedListNode<T>(this, idx, _version);
        }

        public void AddBefore(in FpLinkedListNode<T> node, T value, out FpLinkedListNode<T> insertedNode)
        {
            ValidateNode(in node);
            
            TakePlace(out int idx);

            if (_first == node.Idx)
            {
                _first = idx;
            }

            _entries[idx].Value = value;
            _entries[idx].Previous = _entries[node.Idx].Previous;
            _entries[idx].Next = node.Idx;
            _entries[idx].Version = ++_version;

            _entries[node.Idx].Previous = idx;
            
            insertedNode = new FpLinkedListNode<T>(this, idx, _version);
        }
        
        public FpLinkedListNode<T> AddAfter(in FpLinkedListNode<T> node, T value)
        {
            AddAfter(in node, value, out FpLinkedListNode<T> result);
            return result;
        }

        public FpLinkedListNode<T> AddBefore(in FpLinkedListNode<T> node, T value)
        {
            AddBefore(in node, value, out FpLinkedListNode<T> result);
            return result;
        }

        public T GetValue(in FpLinkedListNode<T> node)
        {
            ValidateNode(in node);

            return _entries[node.Idx].Value;
        }

        public void SetValue(in FpLinkedListNode<T> node, T value)
        {
            ValidateNode(in node);

            _entries[node.Idx].Value = value;
        }

        /// <summary>
        ///     Swap values internally without version changes
        /// </summary>
        /// <param name="first">First node of this sequence</param>
        /// <param name="second">Second node of this sequence</param>
        public void SwapValuesSilent(in FpLinkedListNode<T> first, in FpLinkedListNode<T> second)
        {
            ValidateNode(in first);
            ValidateNode(in second);

            (_entries[first.Idx].Value, _entries[second.Idx].Value) = (_entries[second.Idx].Value, _entries[first.Idx].Value);
            _entries[First.Idx].Version = ++_version;
            _entries[second.Idx].Version = _version;
        }
        
        /// <summary>
        ///     Swap values and refresh nodes to valid state
        /// </summary>
        /// <param name="first">First node of this sequence</param>
        /// <param name="second">Second node of this sequence</param>
        public void SwapValues(ref FpLinkedListNode<T> first, ref FpLinkedListNode<T> second)
        {
            ValidateNode(in first);
            ValidateNode(in second);

            (_entries[first.Idx].Value, _entries[second.Idx].Value) = (_entries[second.Idx].Value, _entries[first.Idx].Value);
            
            _version++;
            
            _entries[first.Idx].Version = _version;
            _entries[second.Idx].Version = _version;

            first = new FpLinkedListNode<T>(this, first.Idx, _version);
            second = new FpLinkedListNode<T>(this, second.Idx, _version);
        }
        
        public bool TryGetNextNode(in FpLinkedListNode<T> node, out FpLinkedListNode<T> next)
        {
            ValidateNode(in node);

            int nextNode = _entries[node.Idx].Next;
            if (nextNode < 0)
            {
                next = FpLinkedListNode<T>.Invalid;
                return false;
            }

            next = new FpLinkedListNode<T>(this, nextNode, _version);
            return true;
        }

        public bool TryGetPreviousNode(in FpLinkedListNode<T> node, out FpLinkedListNode<T> previous)
        {
            ValidateNode(in node);

            int entryIdx = _entries[node.Idx].Previous;
            if (entryIdx < 0)
            {
                previous = FpLinkedListNode<T>.Invalid;
                return false;
            }

            previous = new FpLinkedListNode<T>(this, entryIdx, _version);
            return true;
        }

        public void Clear()
        {
            _count = 0;
            _freeCount = 0;

            _first = _last = _freeList = -1;

            Array.Clear(_entries, 0, _count);
            _version++;
        }

        public bool TryGetFirst(out FpLinkedListNode<T> node)
        {
            if (_first < 0)
            {
                node = FpLinkedListNode<T>.Invalid;
                return false;
            }

            node = new FpLinkedListNode<T>(this, _first, _version);
            return true;
        }

        public bool TryGetLast(out FpLinkedListNode<T> node)
        {
            if (_last == -1)
            {
                node = FpLinkedListNode<T>.Invalid;
                return false;
            }

            node = new FpLinkedListNode<T>(this, _last, _version);
            return true;
        }

#endregion

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsValidNode(in FpLinkedListNode<T> node)
        {
            return _entries[node.Idx].Version <= node.Version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GrowIfFull()
        {
            if (!IsFull)
            {
                return;
            }

            Capacity *= 2;
            Array.Resize(ref _entries, Capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GivePlace(int idx)
        {
            _entries[idx].Value = default;
            _entries[idx].Next = _freeList;
            _entries[idx].Version = ++_version;

            _freeList = idx;

            _freeCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TakePlace(out int idx)
        {
            if (_freeCount > 0)
            {
                idx = _freeList;
                _freeList = _entries[idx].Next;
                _freeCount--;
            }
            else
            {
                GrowIfFull();
                idx = _count++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool InitNode(T value, int idx, ref FpLinkedListNode<T> node)
        {
            if (_last != -1)
            {
                return false;
            }

            _last = idx;
            _first = idx;

            _entries[idx].Value = value;
            _entries[idx].Version = ++_version;
            _entries[idx].Next = -1;
            _entries[idx].Previous = -1;

            node = new FpLinkedListNode<T>(this, idx, _version);
            return true;
        }

        private void ValidateNode(in FpLinkedListNode<T> node)
        {
            if (node.List != this || !IsValidNode(in node))
            {
                throw new InvalidOperationException("Node is invalid");
            }
        }

        private struct LinkedListEntry
        {
            public T Value;

            public int Next;
            public int Previous;

            public int Version;
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly FpLinkedList<T> _list;
            private readonly int _version;
            private int _index;

            public Enumerator(FpLinkedList<T> list)
            {
                _list = list;
                _version = list._version;
                _index = list._first;
                Current = default;
            }

#region IDisposable Implementation

            public void Dispose() { }

#endregion

#region IEnumerator Implementation

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_version != _list._version)
                {
                    throw new InvalidOperationException("List version validation error, list has been changed and this iterator being invalidate");
                }

                if (_index < 0)
                {
                    return false;
                }

                Current = _list._entries[_index].Value;
                _index = _list._entries[_index].Next;

                return true;
            }

            public void Reset()
            {
                Current = default;
                _index = _list._first;
            }

#endregion

#region IEnumerator<T> Implementation

            public T Current { get; private set; }

#endregion
        }
    }
}