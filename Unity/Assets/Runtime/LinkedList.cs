using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Fp.Collections
{
	[DebuggerDisplay("Count = {Count}, Capacity = {Capacity}")]
	public sealed class LinkedList<T> : ILinkedList<T>, IEnumerable<T>
	{
		private int _freeList = -1;
		private int _freeCount;

		private int _count;

		private int[] _entriesNext;
		private int[] _entriesPrev;
		private T[] _values;
		
		private int _version;
		private int _lastIdx = -1;
		private int _firstIdx = -1;

		public LinkedList(int initialCapacity = 8)
		{
			Grow(Math.Max(initialCapacity, 1));
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

		public void Remove(int nodeIdx)
		{
			int next = _entriesNext[nodeIdx];
			int prev = _entriesPrev[nodeIdx];

			if(next >= 0)
			{
				_entriesPrev[next] = prev;
			}
			else
			{
				_lastIdx = prev;
			}

			if(prev >= 0)
			{
				_entriesNext[prev] = next;
			}
			else
			{
				_firstIdx = next;
			}

			GivePlace(nodeIdx);
		}

		public int AddFirst(T value)
		{
			AddFirst(value, out int nodeIdx);
			return nodeIdx;
		}

		public void AddFirst(T value, out int nodeIdx)
		{
			nodeIdx = default;

			TakePlace(out int idx);

			if(InitFirstIndex(value, idx))
			{
				return;
			}

			_entriesPrev[FirstIdx] = idx;

			_values[idx] = value;
			_entriesPrev[idx] = -1;
			_entriesNext[idx] = FirstIdx;
			++_version;

			_firstIdx = idx;

			nodeIdx = idx;
		}

		public int AddLast(T value)
		{
			AddLast(value, out int nodeIdx);
			return nodeIdx;
		}

		public void AddLast(T value, out int node)
		{
			node = default;

			TakePlace(out int idx);

			if(InitFirstIndex(value, idx))
			{
				return;
			}

			_entriesNext[LastIdx] = idx;

			_values[idx] = value;
			_entriesPrev[idx] = LastIdx;
			_entriesNext[idx] = -1;
			++_version;

			_lastIdx = idx;

			node = idx;
		}

		public void AddAfter(int nodeIdx, T value, out int inserted)
		{
			TakePlace(out int idx);

			if(LastIdx == nodeIdx)
			{
				_lastIdx = idx;
			}

			_values[idx] = value;
			_entriesPrev[idx] = nodeIdx;
			_entriesNext[idx] = _entriesNext[nodeIdx];
			++_version;

			_entriesNext[nodeIdx] = idx;

			inserted = idx;
		}

		public void AddBefore(int nodeIdx, T value, out int inserted)
		{
			TakePlace(out int idx);

			if(FirstIdx == nodeIdx)
			{
				_firstIdx = idx;
			}

			_values[idx] = value;
			_entriesPrev[idx] = _entriesPrev[nodeIdx];
			_entriesNext[idx] = nodeIdx;
			++_version;

			_entriesPrev[nodeIdx] = idx;

			inserted = idx;
		}

		public int AddAfter(int nodeIdx, T value)
		{
			AddAfter(nodeIdx, value, out int result);
			return result;
		}

		public int AddBefore(int nodeIdx, T value)
		{
			AddBefore(nodeIdx, value, out int result);
			return result;
		}

		public void SetValue(int nodeIdx, T value)
		{
			_values[nodeIdx] = value;
			_version++;
		}

        /// <summary>
        ///     Swap values and refresh nodes to valid state
        /// </summary>
        /// <param name="firstIdx">First node index</param>
        /// <param name="secondIdx">Second node index</param>
        public void Swap(int firstIdx, int secondIdx)
		{
			(_values[firstIdx], _values[secondIdx]) = (_values[secondIdx], _values[firstIdx]);
			_version++;
		}

		public void ChainClear()
		{
			_count = 0;
			_freeCount = 0;

			int targetIdx = FirstIdx;

			while(targetIdx >= 0)
			{
				int next = _entriesNext[targetIdx];
				
				_values[targetIdx] = default;
				_entriesNext[targetIdx] = -1;
				_entriesPrev[targetIdx] = -1;
				
				targetIdx = next;
			}
			
			_firstIdx = _lastIdx = _freeList = -1;
			_version++;
		}

		public void Clear()
		{
			_count = 0;
			_freeCount = 0;

			for(int i = 0; i < Capacity; i++)
			{
				_values[i] = default;
				_entriesNext[i] = -1;
				_entriesPrev[i] = -1;
			}
			
			_firstIdx = _lastIdx = _freeList = -1;
			_version++;
		}
		
#endregion

#region IReadOnlyLinkedList<T> Implementation

		public int Count => _count - _freeCount;
		public int Capacity { get; private set; }

		public int FirstIdx => _firstIdx;

		public int LastIdx => _lastIdx;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T GetValue(int nodeIdx)
		{
			return GetValue(ref nodeIdx);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T GetValue(ref int nodeIdx)
		{
			return GetValueRef(ref nodeIdx);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref T GetValueRef(ref int nodeIdx)
		{
			return ref _values[nodeIdx];
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool HasValue(int nodeIdx)
		{
			return HasValue(ref nodeIdx);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool HasValue(ref int nodeIdx)
		{
			return nodeIdx >= 0 && nodeIdx < Capacity && NodeHasValue(ref nodeIdx);
		}

		private bool NodeHasValue(ref int nodeIdx)
		{
			return nodeIdx == FirstIdx || _entriesPrev[nodeIdx] != -1;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetNext(int nodeIdx, out int nextIdx)
		{
			nextIdx = GetNext(nodeIdx);
			return nextIdx >= 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetNext(int nodeIdx)
		{
			return GetNext(ref nodeIdx);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetNext(ref int nodeIdx)
		{
			return _entriesNext[nodeIdx];
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void MoveNext(ref int nodeIdx)
		{
			nodeIdx = _entriesNext[nodeIdx];
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetPrevious(int nodeIdx, out int previousIdx)
		{
			previousIdx = GetPrevious(nodeIdx);
			return previousIdx >= 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetPrevious(int nodeIdx)
		{
			return GetPrevious(ref nodeIdx);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetPrevious(ref int nodeIdx)
		{
			return _entriesPrev[nodeIdx];
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void MovePrevious(ref int nodeIdx)
		{
			nodeIdx = _entriesPrev[nodeIdx];
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetFirst(out int nodeIdx)
		{
			nodeIdx = FirstIdx;
			return FirstIdx >= 0;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetLast(out int nodeIdx)
		{
			nodeIdx = LastIdx;
			return LastIdx >= 0;
		}

#endregion

		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void GrowIfFull()
		{
			if(!IsFull)
			{
				return;
			}

			Grow(Capacity * 2 + 1);
		}

		private void Grow(int capacity)
		{
			Array.Resize(ref _values, capacity);
			Array.Resize(ref _entriesNext, capacity);
			Array.Resize(ref _entriesPrev, capacity);
			
			//Fill new element with default values(needed for initializing new nodes with -1 indices)
			for (int i = Capacity; i < capacity; i++)
			{
				_entriesNext[i] = -1;
				_entriesPrev[i] = -1;
			}

			Capacity = capacity;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void GivePlace(int idx)
		{
			_values[idx] = default;
			_entriesNext[idx] = _freeList;
			++_version;

			_freeList = idx;

			_freeCount++;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void TakePlace(out int idx)
		{
			if(_freeCount > 0)
			{
				idx = _freeList;
				_freeList = _entriesNext[idx];
				_freeCount--;
			}
			else
			{
				GrowIfFull();
				idx = _count++;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool InitFirstIndex(T value, int idx)
		{
			if(FirstIdx >= 0)
			{
				return false;
			}

			_firstIdx = idx;
			_lastIdx = idx;

			_values[idx] = value;
			++_version;
			_entriesNext[idx] = -1;
			_entriesPrev[idx] = -1;

			return true;
		}

		private struct LinkedListEntry
		{
			public static readonly LinkedListEntry Empty = new LinkedListEntry() { Value = default, NextIdx = -1, PreviousIdx = -1 }; 
			
			public T Value;

			public int NextIdx;
			public int PreviousIdx;
		}

		public struct Enumerator : IEnumerator<T>
		{
			private readonly LinkedList<T> _list;
			private readonly int _version;
			private int _index;
			private T _current;

			public Enumerator(LinkedList<T> list)
			{
				_list = list;
				_version = list._version;
				Reset(_list, out _current, out _index);
			}

#region IDisposable Implementation

			public void Dispose()
			{
				_index = -1;
			}

#endregion

#region IEnumerator Implementation

			object IEnumerator.Current => _current;

			public bool MoveNext()
			{
				if(_version != _list._version)
				{
					throw new InvalidOperationException("List version validation error, list has been changed and this iterator being invalidate");
				}

				if(_index < 0)
				{
					return false;
				}

				_current = _list._values[_index];
				_index = _list._entriesNext[_index];

				return true;
			}

			public void Reset()
			{
				Reset(_list, out _current, out _index);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static void Reset(IReadOnlyLinkedList<T> list, out T value, out int index)
			{
				value = default;
				index = list.FirstIdx;
			}
			
#endregion

#region IEnumerator<T> Implementation

			public T Current => _current;

#endregion
		}
	}
}