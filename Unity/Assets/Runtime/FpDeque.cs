using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Fp.Collections
{
    /// <summary>
    ///     A double-ended queue (deque), which provides O(1) indexed access, O(1) removals from the begin to the end,
    ///     amortized
    ///     O(1) insertions to the begin and the end, and O(N) insertions and removals anywhere else (with the operations
    ///     getting
    ///     slower as the index approaches the middle).
    /// </summary>
    /// <typeparam name="T">The type of elements contained in the deque.</typeparam>
    [DebuggerDisplay("Count = {Count}, Capacity = {Capacity}")]
    [DebuggerTypeProxy(typeof(FpDeque<>.DebugView))]
    public sealed class FpDeque<T> : IList<T>, IReadOnlyList<T>, IList
    {
        private const int DefaultCapacity = 4;

        private const int MaximumCapacity = 0x7FEFFFFF;

        private T[] _buffer;

        private int _offset;

        private int _version;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FpDeque{T}" /> class with the specified capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity. Must be greater than <c>0</c>.</param>
        public FpDeque(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity mustn't be negative or zero.");
            }

            _buffer = new T[capacity];
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FpDeque{T}" /> class with the elements from the specified
        ///     collection.
        /// </summary>
        /// <param name="list">The collection. May not be <c>null</c>.</param>
        public FpDeque(IReadOnlyList<T> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            int count = list.Count;
            if (count > 0)
            {
                _buffer = new T[count];
                DoInsertRange(0, list);
            }
            else
            {
                _buffer = new T[DefaultCapacity];
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FpDeque{T}" /> class.
        /// </summary>
        public FpDeque()
            : this(DefaultCapacity) { }

        /// <summary>
        ///     Gets or sets the capacity for this deque. This value must always be greater than zero, and this property cannot be
        ///     set to a value less than <see cref="Count" />.
        /// </summary>
        /// <exception cref="InvalidOperationException"><c>Capacity</c> cannot be set to a value less than <see cref="Count" />.</exception>
        public int Capacity
        {
            get => _buffer.Length;

            set
            {
                if (value < Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                                                          "Capacity cannot be set to a value less than Count");
                }

                if (value == _buffer.Length)
                {
                    return;
                }

                // Create the new _buffer and copy our existing range.
                var newBuffer = new T[value];
                CopyToArray(newBuffer);

                // Set up to use the new _buffer.
                _buffer = newBuffer;
                _offset = 0;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether this instance is empty.
        /// </summary>
        private bool IsEmpty => Count == 0;

        /// <summary>
        ///     Gets a value indicating whether this instance is at full capacity.
        /// </summary>
        private bool IsFull => Count == Capacity;

        /// <summary>
        ///     Gets a value indicating whether the buffer is "split"
        ///     (meaning the beginning of the view is at a later index in <see cref="_buffer" /> than the end).
        /// </summary>
        private bool IsSplit => _offset > Capacity - Count;

#region ICollection<T> Implementation

        /// <summary>
        ///     Gets the number of elements contained in this deque.
        /// </summary>
        /// <returns>The number of elements contained in this deque.</returns>
        public int Count { get; private set; }

        /// <summary>
        ///     Removes all items from this deque.
        /// </summary>
        public void Clear()
        {
            _offset = 0;
            Count = 0;
        }

#endregion

        /// <summary>
        ///     Inserts a single element at the right of this deque.
        /// </summary>
        /// <param name="value">The element to insert.</param>
        public void AddToRight(T value)
        {
            EnsureCapacityForOneElement();
            DoAddToRight(value);
        }

        /// <summary>
        ///     Inserts a single element at the begin of this deque.
        /// </summary>
        /// <param name="value">The element to insert.</param>
        public void AddBegin(T value)
        {
            EnsureCapacityForOneElement();
            DoAddToBegin(value);
        }

        /// <summary>
        ///     Inserts a collection of elements into this deque.
        /// </summary>
        /// <param name="index">The index at which the collection is inserted.</param>
        /// <param name="list">The collection of elements to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is not a valid index to an insertion point for
        ///     the source.
        /// </exception>
        public void InsertRange<TList>(int index, TList list)
            where TList : class, IReadOnlyList<T>
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            CheckNewIndexArgument(Count, index);

            int collectionCount = list.Count;

            if (collectionCount == 0)
            {
                return;
            }

            EnsureCapacity(Count + collectionCount);

            DoInsertRange(index, list);
        }

        /// <summary>
        ///     Removes a range of elements from this deque.
        /// </summary>
        /// <param name="offset">The index into the deque at which the range begins.</param>
        /// <param name="count">The number of elements to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Either <paramref name="offset" /> or <paramref name="count" /> is less
        ///     than 0.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     The range [<paramref name="offset" />, <paramref name="offset" /> +
        ///     <paramref name="count" />) is not within the range [0, <see cref="Count" />).
        /// </exception>
        public void RemoveRange(int offset, int count)
        {
            CheckRangeArguments(Count, offset, count);

            if (count == 0)
            {
                return;
            }

            DoRemoveRange(offset, count);
        }

        /// <summary>
        ///     Searches element in sorted deque by binary search algorithm.
        /// </summary>
        /// <param name="index">Offset from zero element.</param>
        /// <param name="length">Count of elements.</param>
        /// <param name="target">Value to find.</param>
        /// <param name="comparer">Comparer for compare operations.</param>
        /// <typeparam name="TComparer">Type of comparer</typeparam>
        /// <returns>Index of first found element</returns>
        public int BinarySearchFirst<TComparer>(int index, int length, in T target, in TComparer comparer)
            where TComparer : IComparer<T>
        {
            int lower = index;
            int upper = index + length - 1;
            int rIdx = -1;

            while (lower <= upper)
            {
                int middle = lower + ((upper - lower) >> 1);
                int compareTo = comparer.Compare(_buffer[DequeIndexToBufferIndex(middle)], target);

                if (compareTo == 0)
                {
                    rIdx = middle;
                    upper = middle - 1;
                }
                else if (compareTo < 0)
                {
                    lower = middle + 1;
                }
                else
                {
                    upper = middle - 1;
                }
            }

            return rIdx == -1 ? ~lower : rIdx;
        }

        /// <summary>
        ///     Searches element in sorted deque by binary search algorithm.
        /// </summary>
        /// <param name="index">Offset from zero element.</param>
        /// <param name="length">Count of elements.</param>
        /// <param name="target">Value to find.</param>
        /// <param name="comparer">Comparer for compare operations.</param>
        /// <typeparam name="TComparer">Type of comparer</typeparam>
        /// <returns>Index of last found element</returns>
        public int BinarySearchLast<TComparer>(int index, int length, in T target, in TComparer comparer)
            where TComparer : IComparer<T>
        {
            //TODO: Do assert element ordering in passed range
            int lower = index;
            int upper = index + length - 1;
            int rIdx = -1;

            while (lower <= upper)
            {
                int middle = lower + ((upper - lower) >> 1);
                int compareTo = comparer.Compare(_buffer[DequeIndexToBufferIndex(middle)], target);

                if (compareTo == 0)
                {
                    rIdx = middle;
                    lower = middle + 1;
                }
                else if (compareTo < 0)
                {
                    lower = middle + 1;
                }
                else
                {
                    upper = middle - 1;
                }
            }

            return rIdx == -1 ? ~lower : rIdx;
        }

        /// <summary>
        ///     Searches element in sorted deque by binary search algorithm.
        /// </summary>
        /// <param name="target">Value to find.</param>
        /// <param name="comparer">Comparer for compare operations.</param>
        /// <typeparam name="TComparer">Type of comparer</typeparam>
        /// <returns>Index of found element</returns>
        public int BinarySearchFirst<TComparer>(in T target, in TComparer comparer)
            where TComparer : IComparer<T>
        {
            return BinarySearchFirst(0, Count, target, comparer);
        }

        /// <summary>
        ///     Searches element in sorted deque by binary search algorithm.
        /// </summary>
        /// <param name="target">Value to find.</param>
        /// <param name="comparer">Comparer for compare operations.</param>
        /// <typeparam name="TComparer">Type of comparer</typeparam>
        /// <returns>Index of found element</returns>
        public int BinarySearchLast<TComparer>(in T target, in TComparer comparer)
            where TComparer : IComparer<T>
        {
            return BinarySearchLast(0, Count, target, comparer);
        }

        /// <summary>
        ///     Removes and returns the right element of this deque.
        /// </summary>
        /// <returns>The former last element.</returns>
        /// <exception cref="InvalidOperationException">The deque is empty.</exception>
        public T PopRight()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("The deque is empty.");
            }

            return DoRemoveFromRight();
        }

        /// <summary>
        ///     Removes the <paramref name="count" /> elements of this deque from the right side.
        /// </summary>
        /// <param name="count"></param>
        /// <exception cref="InvalidOperationException">The deque is empty</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     The deque has less element than passed <paramref name="count" />
        /// </exception>
        public void RemoveRight(int count)
        {
            if (count > Count || count < 0)
            {
                if (IsEmpty)
                {
                    throw new InvalidOperationException("The deque is empty.");
                }

                throw new ArgumentOutOfRangeException(nameof(count), "Argument must be less than deque count.");
            }

            DoRemoveFromRight(count);
        }

        /// <summary>
        ///     Peek the right element of this deque
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">The deque is empty.</exception>
        public ref T PeekRight()
        {
            ThrowDequeIfEmpty();

            return ref _buffer[DequeIndexToBufferIndex(Count - 1)];
        }

        /// <summary>
        ///     Peek the right element of this deque by offset from right side
        /// </summary>
        /// <param name="rightIndex">offset from right side</param>
        public ref T PeekRight(int rightIndex)
        {
            ThrowDequeIfEmpty();
            CheckExistingIndexArgument(Count, rightIndex);
            return ref _buffer[DequeIndexToBufferIndex(Count - 1 - rightIndex)];
        }

        /// <summary>
        ///     Try peek the right element of this deque
        /// </summary>
        /// <param name="value">Last element</param>
        /// <returns>True if not empty</returns>
        public bool TryPeekRight(out T value)
        {
            if (IsEmpty)
            {
                value = default;
                return false;
            }

            value = _buffer[DequeIndexToBufferIndex(Count - 1)];
            return true;
        }

        /// <summary>
        ///     Removes and returns the begin element of this deque.
        /// </summary>
        /// <returns>The former first element.</returns>
        /// <exception cref="InvalidOperationException">The deque is empty.</exception>
        public T PopLeft()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("The deque is empty.");
            }

            return DoRemoveFromBegin();
        }

        /// <summary>
        ///     Removes the <paramref name="count" /> elements of this deque from the begin side.
        /// </summary>
        /// <param name="count"></param>
        /// <exception cref="InvalidOperationException">The deque is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     The deque has less element than passed <paramref name="count" />
        /// </exception>
        public void RemoveLeft(int count)
        {
            if (count > Count || count < 0)
            {
                if (IsEmpty)
                {
                    throw new InvalidOperationException("The deque is empty.");
                }

                throw new ArgumentOutOfRangeException(nameof(count), "Argument must be less than deque count.");
            }

            DoRemoveFromBegin(count);
        }

        /// <summary>
        ///     Peek the begin element of this deque
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">The deque is empty.</exception>
        public ref T PeekLeft()
        {
            ThrowDequeIfEmpty();

            return ref _buffer[_offset];
        }

        /// <summary>
        ///     Peek the begin element of this deque by offset from begin side
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ref T PeekLeft(int index)
        {
            return ref PeekItem(index);
        }

        /// <summary>
        ///     Try peek the begin element of this deque
        /// </summary>
        /// <param name="value">First element</param>
        /// <returns>True if not empty</returns>
        public bool TryPeekLeft(out T value)
        {
            if (IsEmpty)
            {
                value = default;
                return false;
            }

            value = _buffer[_offset];
            return true;
        }

        /// <summary>
        ///     Peek item by index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ref T PeekItem(int index)
        {
            CheckExistingIndexArgument(Count, index);
            return ref _buffer[DequeIndexToBufferIndex(index)];
        }

        /// <summary>
        ///     Creates and returns a new array containing the elements in this deque.
        /// </summary>
        public T[] ToArray()
        {
            var result = new T[Count];
            ((ICollection<T>)this).CopyTo(result, 0);
            return result;
        }

        /// <summary>
        ///     Checks empty deque and throw exception
        /// </summary>
        /// <exception cref="InvalidOperationException">The deque is empty.</exception>
        private void ThrowDequeIfEmpty()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("The deque is empty.");
            }
        }

        /// <summary>
        ///     Applies the offset to <paramref name="index" />, resulting in a buffer index.
        /// </summary>
        /// <param name="index">The deque index.</param>
        /// <returns>The buffer index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int DequeIndexToBufferIndex(int index)
        {
            return (index + _offset) % Capacity;
        }

        /// <summary>
        ///     Gets an element at the specified view index.
        /// </summary>
        /// <param name="index">The zero-based view index of the element to get. This index is guaranteed to be valid.</param>
        /// <returns>The element at the specified index.</returns>
        private ref T DoGetItem(int index)
        {
            return ref _buffer[DequeIndexToBufferIndex(index)];
        }

        /// <summary>
        ///     Sets an element at the specified view index.
        /// </summary>
        /// <param name="index">The zero-based view index of the element to get. This index is guaranteed to be valid.</param>
        /// <param name="item">The element to store in the list.</param>
        private void DoSetItem(int index, T item)
        {
            _buffer[DequeIndexToBufferIndex(index)] = item;
            _version++;
        }

        /// <summary>
        ///     Inserts an element at the specified view index.
        /// </summary>
        /// <param name="index">
        ///     The zero-based view index at which the element should be inserted. This index is guaranteed to be
        ///     valid.
        /// </param>
        /// <param name="item">The element to store in the list.</param>
        private void DoInsert(int index, T item)
        {
            EnsureCapacityForOneElement();

            if (index == 0)
            {
                DoAddToBegin(item);
                return;
            }

            if (index == Count)
            {
                DoAddToRight(item);
                return;
            }

            DoExpandSpaceIn(index, 1);

            // Copy new items into place
            _buffer[DequeIndexToBufferIndex(index)] = item;

            // Adjust valid count
            Count++;
            _version++;
        }

        /// <summary>
        ///     Removes an element at the specified view index.
        /// </summary>
        /// <param name="index">The zero-based view index of the element to remove. This index is guaranteed to be valid.</param>
        private void DoRemoveAt(int index)
        {
            if (index == 0)
            {
                DoRemoveFromBegin();
            }
            else if (index == Count - 1)
            {
                DoRemoveFromRight();
            }
            else
            {
                DoRemoveRange(index, 1);
            }
        }

        /// <summary>
        ///     Increments <see cref="_offset" /> by <paramref name="value" /> using modulo-<see cref="Capacity" /> arithmetic.
        /// </summary>
        /// <param name="value">The value by which to increase <see cref="_offset" />. May not be negative.</param>
        /// <returns>The value of <see cref="_offset" /> after it was incremented.</returns>
        private int PostIncrement(int value)
        {
            int ret = _offset;
            _offset += value;
            _offset %= Capacity;
            return ret;
        }

        /// <summary>
        ///     Decrements <see cref="_offset" /> by <paramref name="value" /> using modulo-<see cref="Capacity" /> arithmetic.
        /// </summary>
        /// <param name="value">
        ///     The value by which to reduce <see cref="_offset" />. May not be negative or greater than
        ///     <see cref="Capacity" />.
        /// </param>
        /// <returns>The value of <see cref="_offset" /> before it was decremented.</returns>
        private int PreDecrement(int value)
        {
            _offset -= value;
            if (_offset < 0)
            {
                _offset += Capacity;
            }

            return _offset;
        }

        /// <summary>
        ///     Inserts a single element to the right of the view. <see cref="IsFull" /> must be false when this method is called.
        /// </summary>
        /// <param name="value">The element to insert.</param>
        private void DoAddToRight(T value)
        {
            _buffer[DequeIndexToBufferIndex(Count)] = value;
            Count++;
            _version++;
        }

        /// <summary>
        ///     Inserts a single element to the begin of the view. <see cref="IsFull" /> must be false when this method is called.
        /// </summary>
        /// <param name="value">The element to insert.</param>
        private void DoAddToBegin(T value)
        {
            _buffer[PreDecrement(1)] = value;
            Count++;
            _version++;
        }

        /// <summary>
        ///     Removes and returns the last element in the view. <see cref="IsEmpty" /> must be false when this method is called.
        /// </summary>
        /// <returns>The former last element.</returns>
        private T DoRemoveFromRight()
        {
            T ret = _buffer[DequeIndexToBufferIndex(Count - 1)];
            Count--;
            _version++;
            return ret;
        }

        /// <summary>
        ///     Removes the last
        ///     <param name="count" />
        ///     elements.
        /// </summary>
        /// <param name="count">Count of elements. Must be less than <see cref="Count" /></param>
        private void DoRemoveFromRight(int count)
        {
            Count -= count;
            _version++;
        }

        /// <summary>
        ///     Removes and returns the first element in the view. <see cref="IsEmpty" /> must be false when this method is called.
        /// </summary>
        /// <returns>The former first element.</returns>
        private T DoRemoveFromBegin()
        {
            Count--;
            _version++;
            return _buffer[PostIncrement(1)];
        }

        /// <summary>
        ///     Remove the first
        ///     <param name="count" />
        ///     elements.
        /// </summary>
        /// <param name="count">Count of elements. Must be less than <see cref="Count" /></param>
        private void DoRemoveFromBegin(int count)
        {
            Count -= count;
            _version++;
            PostIncrement(count);
        }

        /// <summary>
        ///     Inserts a range of elements into the view.
        /// </summary>
        /// <param name="index">The index into the view at which the elements are to be inserted.</param>
        /// <param name="list">
        ///     The elements to insert. The sum of <c>collection.Count</c> and <see cref="Count" /> must be
        ///     less than or equal to <see cref="Capacity" />.
        /// </param>
        private void DoInsertRange<TList>(int index, TList list)
            where TList : class, IReadOnlyList<T>
        {
            int collectionCount = list.Count;
            DoExpandSpaceIn(index, collectionCount);

            // Copy new items into place
            int i = index;
            foreach (T item in list)
            {
                _buffer[DequeIndexToBufferIndex(i)] = item;
                ++i;
            }

            // Adjust valid count
            Count += collectionCount;
            _version++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoExpandSpaceIn(int index, int count)
        {
            // Make room in the existing list
            if (index < Count / 2)
            {
                // Inserting into the first half of the list

                // Move lower items down: [0, index) -> [Capacity - collectionCount, Capacity - collectionCount + index)
                // This clears out the low "index" number of items, moving them "collectionCount" places down;
                //   after rotation, there will be a "collectionCount"-sized hole at "index".
                int copyCount = index;
                int writeIndex = Capacity - count;
                for (var j = 0; j != copyCount; ++j)
                {
                    _buffer[DequeIndexToBufferIndex(writeIndex + j)] = _buffer[DequeIndexToBufferIndex(j)];
                }

                // Rotate to the new view
                PreDecrement(count);
            }
            else
            {
                // Inserting into the second half of the list

                // Move higher items up: [index, count) -> [index + collectionCount, collectionCount + count)
                int copyCount = Count - index;
                int writeIndex = index + count;
                for (int j = copyCount - 1; j != -1; --j)
                {
                    _buffer[DequeIndexToBufferIndex(writeIndex + j)] = _buffer[DequeIndexToBufferIndex(index + j)];
                }
            }
        }

        /// <summary>
        ///     Removes a range of elements from the view.
        /// </summary>
        /// <param name="index">The index into the view at which the range begins.</param>
        /// <param name="collectionCount">
        ///     The number of elements in the range. This must be greater than 0 and less than or equal
        ///     to <see cref="Count" />.
        /// </param>
        private void DoRemoveRange(int index, int collectionCount)
        {
            if (index == 0)
            {
                // Removing from the beginning: rotate to the new view
                PostIncrement(collectionCount);
            }
            else if (index == Count - collectionCount)
            {
                // Removing from the ending: trim the existing view
            }
            else if (index + collectionCount / 2 < Count / 2)
            {
                // Removing from first half of list

                // Move lower items up: [0, index) -> [collectionCount, collectionCount + index)
                int copyCount = index;
                int writeIndex = collectionCount;
                for (int j = copyCount - 1; j != -1; --j)
                {
                    _buffer[DequeIndexToBufferIndex(writeIndex + j)] = _buffer[DequeIndexToBufferIndex(j)];
                }

                // Rotate to new view
                PostIncrement(collectionCount);
            }
            else
            {
                // Removing from second half of list

                // Move higher items down: [index + collectionCount, count) -> [index, count - collectionCount)
                int copyCount = Count - collectionCount - index;
                int readIndex = index + collectionCount;
                for (var j = 0; j != copyCount; ++j)
                {
                    _buffer[DequeIndexToBufferIndex(index + j)] = _buffer[DequeIndexToBufferIndex(readIndex + j)];
                }
            }

            // Adjust valid count
            Count -= collectionCount;
            _version++;
        }

        /// <summary>
        ///     Doubles the capacity if necessary to make room for one more element. When this method returns,
        ///     <see cref="IsFull" /> is false.
        /// </summary>
        private void EnsureCapacityForOneElement()
        {
            if (IsFull)
            {
                Capacity = Capacity == 0 ? 1 : Capacity * 2;
            }
        }

        private void EnsureCapacity(int min)
        {
#if DEBUG
            if (Capacity <= 0)
            {
                throw new InvalidOperationException($"{nameof(Capacity)} must be grater than zero, Capacity = {Capacity}");
            }
#endif

            int newCapacity = Capacity * 2;
            if ((uint)newCapacity > MaximumCapacity)
            {
                newCapacity = MaximumCapacity;
            }

            if (newCapacity < min)
            {
                newCapacity = min;
            }

            Capacity = newCapacity;
        }

        [DebuggerNonUserCode]
        private sealed class DebugView
        {
            private readonly FpDeque<T> _deque;

            public DebugView(FpDeque<T> deque)
            {
                _deque = deque;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] Items => _deque.ToArray();
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly FpDeque<T> _deque;
            private readonly int _version;
            private readonly bool _isSplit;
            private readonly int _offset;

            private int _index;

            public Enumerator(FpDeque<T> deque)
            {
                _deque = deque;
                _version = deque._version;
                _isSplit = deque.IsSplit;
                _offset = deque._offset;
                Current = default;
                _index = 0;
            }

#region IDisposable Implementation

            public void Dispose() { }

#endregion

#region IEnumerator Implementation

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_version != _deque._version)
                {
                    throw new InvalidOperationException("Failed version");
                }

                if (_index < _deque.Count)
                {
                    if (_isSplit)
                    {
                        int length = _deque.Capacity - _offset;
                        Current = _deque._buffer[_index < length ? _offset + _index : _index - length];
                    }
                    else
                    {
                        Current = _deque._buffer[_offset + _index];
                    }

                    _index++;
                    return true;
                }

                Current = default;
                return false;
            }

            public void Reset()
            {
                if (_version != _deque._version)
                {
                    throw new InvalidOperationException("Failed version");
                }

                _index = 0;
                Current = default;
            }

#endregion

#region IEnumerator<T> Implementation

            public T Current { get; private set; }

#endregion
        }

#region GenericListImplementations

        /// <summary>
        ///     Gets a value indicating whether this list is read-only. This implementation always returns <c>false</c>.
        /// </summary>
        /// <returns>true if this list is read-only; otherwise, false.</returns>
        bool ICollection<T>.IsReadOnly => false;

        /// <summary>
        ///     Gets or sets the item at the specified index.
        /// </summary>
        /// <param name="index">The index of the item to get or set.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> is not a valid index in this list.</exception>
        /// <exception cref="T:System.NotSupportedException">This property is set and the list is read-only.</exception>
        public T this[int index]
        {
            get
            {
                CheckExistingIndexArgument(Count, index);
                return DoGetItem(index);
            }
            set
            {
                CheckExistingIndexArgument(Count, index);
                DoSetItem(index, value);
            }
        }

        /// <summary>
        ///     Inserts an item to this list at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert into this list.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is not a valid index in this list.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        ///     This list is read-only.
        /// </exception>
        public void Insert(int index, T item)
        {
            CheckNewIndexArgument(Count, index);
            DoInsert(index, item);
        }

        /// <summary>
        ///     Removes the item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is not a valid index in this list.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        ///     This list is read-only.
        /// </exception>
        public void RemoveAt(int index)
        {
            CheckExistingIndexArgument(Count, index);
            DoRemoveAt(index);
        }

        /// <summary>
        ///     Determines the index of a specific item in this list.
        /// </summary>
        /// <param name="item">The object to locate in this list.</param>
        /// <returns>The index of <paramref name="item" /> if found in this list; otherwise, -1.</returns>
        public int IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            var ret = 0;
            foreach (T sourceItem in this)
            {
                if (comparer.Equals(item, sourceItem))
                {
                    return ret;
                }

                ++ret;
            }

            return -1;
        }

        /// <summary>
        ///     Adds an item to the right side of this deque.
        /// </summary>
        /// <param name="item">The object to add to this list.</param>
        /// <exception cref="T:System.NotSupportedException">
        ///     This list is read-only.
        /// </exception>
        void ICollection<T>.Add(T item)
        {
            DoInsert(Count, item);
        }

        /// <summary>
        ///     Determines whether this list contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in this list.</param>
        /// <returns>
        ///     true if <paramref name="item" /> is found in this list; otherwise, false.
        /// </returns>
        bool ICollection<T>.Contains(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            foreach (T entry in this)
            {
                if (comparer.Equals(item, entry))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Copies the elements of this list to an <see cref="T:System.Array" />, starting at a particular
        ///     <see cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array">
        ///     The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied
        ///     from this slice. The <see cref="T:System.Array" /> must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///     <paramref name="array" /> is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="arrayIndex" /> is less than 0.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        ///     <paramref name="arrayIndex" /> is equal to or greater than the length of <paramref name="array" />.
        ///     -or-
        ///     The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1" /> is greater than the
        ///     available space from <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />.
        /// </exception>
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            int count = Count;
            CheckRangeArguments(array.Length, arrayIndex, count);
            CopyToArray(array, arrayIndex);
        }

        /// <summary>
        ///     Copies the deque elements into an array. The resulting array always has all the deque elements contiguously.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The optional index in the destination array at which to begin writing.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CopyToArray(Array array, int arrayIndex = 0)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (IsSplit)
            {
                // The existing buffer is split, so we have to copy it in parts
                int length = Capacity - _offset;
                Array.Copy(_buffer, _offset, array, arrayIndex, length);
                Array.Copy(_buffer, 0, array, arrayIndex + length, Count - length);
            }
            else
            {
                // The existing buffer is whole
                Array.Copy(_buffer, _offset, array, arrayIndex, Count);
            }
        }

        /// <summary>
        ///     Removes the first occurrence of a specific object from this list.
        /// </summary>
        /// <param name="item">The object to remove from this list.</param>
        /// <returns>
        ///     true if <paramref name="item" /> was successfully removed from this list; otherwise, false. This method also
        ///     returns false if <paramref name="item" /> is not found in this list.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">
        ///     This list is read-only.
        /// </exception>
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index == -1)
            {
                return false;
            }

            DoRemoveAt(index);
            return true;
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#endregion

#region ObjectListImplementations

        private static bool IsT(object value)
        {
            if (value is T)
            {
                return true;
            }

            if (value != null)
            {
                return false;
            }

            return default(T) == null;
        }

        int IList.Add(object value)
        {
            if (value == null && default(T) != null)
            {
                throw new ArgumentNullException(nameof(value), "Value cannot be null.");
            }

            if (!IsT(value))
            {
                throw new ArgumentException("Value is of incorrect type.", nameof(value));
            }

            AddToRight((T)value);
            return Count - 1;
        }

        bool IList.Contains(object value)
        {
            return IsT(value) && ((ICollection<T>)this).Contains((T)value);
        }

        int IList.IndexOf(object value)
        {
            return IsT(value) ? IndexOf((T)value) : -1;
        }

        void IList.Insert(int index, object value)
        {
            if (value == null && default(T) != null)
            {
                throw new ArgumentNullException(nameof(value), "Value cannot be null.");
            }

            if (!IsT(value))
            {
                throw new ArgumentException("Value is of incorrect type.", nameof(value));
            }

            Insert(index, (T)value);
        }

        bool IList.IsFixedSize => false;

        bool IList.IsReadOnly => false;

        void IList.Remove(object value)
        {
            if (IsT(value))
            {
                Remove((T)value);
            }
        }

        object IList.this[int index]
        {
            get => this[index];

            set
            {
                if (value == null && default(T) != null)
                {
                    throw new ArgumentNullException(nameof(value), "Value cannot be null.");
                }

                if (!IsT(value))
                {
                    throw new ArgumentException("Value is of incorrect type.", nameof(value));
                }

                this[index] = (T)value;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array), "Destination array cannot be null.");
            }

            CheckRangeArguments(array.Length, index, Count);

            try
            {
                CopyToArray(array, index);
            }
            catch (ArrayTypeMismatchException ex)
            {
                throw new ArgumentException("Destination array is of incorrect type.", nameof(array), ex);
            }
            catch (RankException ex)
            {
                throw new ArgumentException("Destination array must be single dimensional.", nameof(array), ex);
            }
        }

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

#endregion

#region GenericListHelpers

        /// <summary>
        ///     Checks the <paramref name="index" /> argument to see if it refers to a valid insertion point in a source of a given
        ///     length.
        /// </summary>
        /// <param name="sourceLength">The length of the source. This parameter is not checked for validity.</param>
        /// <param name="index">The index into the source.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is not a valid index to an insertion point for
        ///     the source.
        /// </exception>
        private static void CheckNewIndexArgument(int sourceLength, int index)
        {
            if (index < 0 || index > sourceLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index),
                                                      "Invalid new index " + index + " for source length " + sourceLength);
            }
        }

        /// <summary>
        ///     Checks the <paramref name="index" /> argument to see if it refers to an existing element in a source of a given
        ///     length.
        /// </summary>
        /// <param name="sourceLength">The length of the source. This parameter is not checked for validity.</param>
        /// <param name="index">The index into the source.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is not a valid index to an existing element for
        ///     the source.
        /// </exception>
        private static void CheckExistingIndexArgument(int sourceLength, int index)
        {
            if (index < 0 || index >= sourceLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Invalid existing index " + index + " for source length " + sourceLength);
            }
        }

        /// <summary>
        ///     Checks the <paramref name="offset" /> and <paramref name="count" /> arguments for validity when applied to a source
        ///     of a given length. Allows 0-element ranges, including a 0-element range at the end of the source.
        /// </summary>
        /// <param name="sourceLength">The length of the source. This parameter is not checked for validity.</param>
        /// <param name="offset">The index into source at which the range begins.</param>
        /// <param name="count">The number of elements in the range.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Either <paramref name="offset" /> or <paramref name="count" /> is less
        ///     than 0.
        /// </exception>
        /// <exception cref="ArgumentException">The range [offset, offset + count) is not within the range [0, sourceLength).</exception>
        private static void CheckRangeArguments(int sourceLength, int offset, int count)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Invalid offset " + offset);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Invalid count " + count);
            }

            if (sourceLength - offset < count)
            {
                throw new ArgumentException("Invalid offset (" + offset + ") or count + (" + count +
                                            ") for source length " + sourceLength);
            }
        }

#endregion
    }
}