using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Fp.Collections
{
	public class Map<TKey, TValue> : Map<TKey, TValue, IEqualityComparer<TKey>>
	{
		public Map() : base(EqualityComparer<TKey>.Default) { }
		public Map(IEqualityComparer<TKey> comparer) : base(comparer ?? EqualityComparer<TKey>.Default) { }
		public Map(int capacity, IEqualityComparer<TKey> comparer = null) : base(capacity, comparer ?? EqualityComparer<TKey>.Default) { }

		public Map(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer = null) : base(
			dictionary, comparer ?? EqualityComparer<TKey>.Default
		) { }
	}

	public class Map<TKey, TValue, TComparer> : IDictionary, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
		where TComparer : IEqualityComparer<TKey>
	{
		[NonSerialized]
		private int[] _buckets;

		[NonSerialized]
		private Entry[] _entries;

		[NonSerialized]
		private int _count;

		[NonSerialized]
		private int _version;

		[NonSerialized]
		private int _freeList;

		[NonSerialized]
		private int _freeCount;

		[NonSerialized]
		private KeyCollection _keys;

		[NonSerialized]
		private ValueCollection _values;

		[NonSerialized]
		private object _syncRoot;

		public Map(TComparer comparer) : this(0, comparer) { }

		public Map(int capacity, TComparer comparer)
		{
			if(capacity < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(capacity));
			}

			if(capacity > 0)
			{
				Initialize(capacity);
			}

			Comparer = comparer;
		}

		public Map(IDictionary<TKey, TValue> dictionary, TComparer comparer) : this(dictionary?.Count ?? 0, comparer)
		{
			if(dictionary == null)
			{
				throw new ArgumentNullException(nameof(dictionary));
			}

			foreach(KeyValuePair<TKey, TValue> pair in dictionary)
			{
				Add(pair.Key, pair.Value);
			}
		}

		public TComparer Comparer { get; }

		public KeyCollection Keys => _keys ??= new KeyCollection(this);

		public ValueCollection Values => _values ??= new ValueCollection(this);

		public TValue this[in TKey key]
		{
			get
			{
				if(TryGetValue(in key, out TValue value))
				{
					return value;
				}

				throw new KeyNotFoundException();
			}
			set => ReplaceOrInsert(key, value);
		}

#region ICollection Implementation

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot
		{
			get
			{
				if(_syncRoot == null)
				{
					Interlocked.CompareExchange<object>(ref _syncRoot, new object(), null);
				}

				return _syncRoot;
			}
		}

		public int Count => _count - _freeCount;

		void ICollection.CopyTo(Array array, int index)
		{
			if(array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if(array.Rank != 1)
			{
				throw new ArgumentException("Arg_RankMultiDimNotSupported");
			}

			if(array.GetLowerBound(0) != 0)
			{
				throw new ArgumentException("Arg_NonZeroLowerBound");
			}

			if(index < 0 || index > array.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			if(array.Length - index < Count)
			{
				throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
			}

			switch(array)
			{
				case KeyValuePair<TKey, TValue>[] pairs:
					CopyTo(pairs, index);
					break;
				case DictionaryEntry[] dictEntryArray:
				{
					Entry[] entries = _entries;
					for(var i = 0; i < _count; i++)
					{
						if(entries[i].HashCode >= 0)
						{
							dictEntryArray[index++] = new DictionaryEntry(entries[i].Key, entries[i].Value);
						}
					}

					break;
				}
				default:
				{
					if(!(array is object[] objects))
					{
						throw new ArgumentException("Argument_InvalidArrayType");
					}

					try
					{
						int count = _count;
						Entry[] entries = _entries;
						for(var i = 0; i < count; i++)
						{
							if(entries[i].HashCode >= 0)
							{
								objects[index++] = new KeyValuePair<TKey, TValue>(entries[i].Key, entries[i].Value);
							}
						}
					}
					catch(ArrayTypeMismatchException)
					{
						throw new ArgumentException("Argument_InvalidArrayType");
					}

					break;
				}
			}
		}

#endregion

#region ICollection<KeyValuePair<TKey,TValue>> Implementation

		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
		{
			Add(keyValuePair.Key, keyValuePair.Value);
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
		{
			int i = FindEntry(keyValuePair.Key);
			return i >= 0 && EqualityComparer<TValue>.Default.Equals(_entries[i].Value, keyValuePair.Value);
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
		{
			int i = FindEntry(keyValuePair.Key);
			if(i < 0 || !EqualityComparer<TValue>.Default.Equals(_entries[i].Value, keyValuePair.Value))
			{
				return false;
			}

			Remove(keyValuePair.Key);
			return true;
		}

		void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
		{
			CopyTo(array, index);
		}

#endregion

#region IDictionary Implementation

		bool IDictionary.IsFixedSize => false;

		bool IDictionary.IsReadOnly => false;

		ICollection IDictionary.Keys => Keys;

		ICollection IDictionary.Values => Values;

		public virtual void Clear()
		{
			if(_count <= 0)
			{
				return;
			}

			for(var i = 0; i < _buckets.Length; i++)
			{
				_buckets[i] = -1;
			}

			Array.Clear(_entries, 0, _count);
			_freeList = -1;
			_count = 0;
			_freeCount = 0;
			_version++;
		}

		void IDictionary.Add(object key, object value)
		{
			if(key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			if(value == null && default(TValue) != null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			var tempKey = (TKey)key;
			Add(tempKey, (TValue)value);
		}

		bool IDictionary.Contains(object key)
		{
			return IsCompatibleKey(key) && ContainsKey((TKey)key);
		}

		IDictionaryEnumerator IDictionary.GetEnumerator()
		{
			return new Enumerator(this, Enumerator.DictEntry);
		}

		void IDictionary.Remove(object key)
		{
			if(IsCompatibleKey(key))
			{
				Remove((TKey)key);
			}
		}

		object IDictionary.this[object key]
		{
			get
			{
				if(!IsCompatibleKey(key))
				{
					return null;
				}

				int i = FindEntry((TKey)key);
				if(i >= 0)
				{
					return _entries[i].Value;
				}

				return null;
			}
			set
			{
				if(key == null)
				{
					throw new ArgumentNullException(nameof(key));
				}

				if(value == null && default(TValue) != null)
				{
					throw new ArgumentNullException(nameof(value));
				}

				var tempKey = (TKey)key;
				this[tempKey] = (TValue)value;
			}
		}

#endregion

#region IDictionary<TKey,TValue> Implementation

		ICollection<TKey> IDictionary<TKey, TValue>.Keys => _keys = _keys ??= new KeyCollection(this);

		ICollection<TValue> IDictionary<TKey, TValue>.Values => _values ??= new ValueCollection(this);

		public void Add(TKey key, TValue value)
		{
			Add(in key, value);
		}

		public bool ContainsKey(TKey key)
		{
			return ContainsKey(in key);
		}

		public bool Remove(TKey key)
		{
			return Remove(in key);
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			return TryGetValue(in key, out value);
		}

		public TValue this[TKey key]
		{
			get => this[in key];
			set => this[in key] = value;
		}

#endregion

#region IEnumerable Implementation

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(this, Enumerator.KeyValuePair);
		}

#endregion

#region IEnumerable<KeyValuePair<TKey,TValue>> Implementation

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			return new Enumerator(this, Enumerator.KeyValuePair);
		}

#endregion

#region IReadOnlyDictionary<TKey,TValue> Implementation

		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _keys ??= new KeyCollection(this);

		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _values ??= new ValueCollection(this);

		bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
		{
			return TryGetValue(in key, out value);
		}

		bool IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key)
		{
			return ContainsKey(in key);
		}

		TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key] => this[in key];

#endregion

		public int Add(in TKey key, TValue value)
		{
			Insert(in key, value, out int entry);
			return entry;
		}

		public virtual bool TryAdd(in TKey key, TValue value)
		{
			return TryInsert(in key, value, out _);
		}

		public bool ContainsKey(in TKey key)
		{
			return FindEntry(in key) >= 0;
		}

		public bool Remove(in TKey key)
		{
			return TryRemove(key, out _);
		}

		public bool TryGetValue(in TKey key, out TValue value, TValue failure = default)
		{
			return TryGetAndCast(in key, out value, failure);
		}

		public virtual bool TryGetAndCast<T>(in TKey key, out T value, T failure = default)
		{
			int i = FindEntry(key);
			if(i >= 0 && _entries[i].Value is T val)
			{
				value = val;
				return true;
			}

			value = failure;
			return false;
		}

		public void SetDefaultValues(TValue value = default)
		{
			for(var i = 0; i < _entries.Length; i++)
			{
				if(_entries[i].HashCode > 0)
				{
					_entries[i].Value = value;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual bool TryRemove(in TKey key, out TValue value)
		{
			if(key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			if(_buckets == null)
			{
				value = default;
				return false;
			}

			int hashCode = Comparer.GetHashCode(key) & 0x_7FFF_FFFF;
			int bucket = hashCode % _buckets.Length;
			int last = -1;
			for(int i = _buckets[bucket]; i >= 0; last = i, i = _entries[i].Next)
			{
				if(_entries[i].HashCode != hashCode || !Comparer.Equals(_entries[i].Key, key))
				{
					continue;
				}

				if(last < 0)
				{
					_buckets[bucket] = _entries[i].Next;
				}
				else
				{
					_entries[last].Next = _entries[i].Next;
				}

				value = _entries[i].Value;

				_entries[i].HashCode = -1;
				_entries[i].Next = _freeList;
				_entries[i].Key = default;
				_entries[i].Value = default;
				_freeList = i;
				_freeCount++;
				_version++;
				return true;
			}

			value = default;
			return false;
		}

		public ref TValue GetValue(in TKey key)
		{
			int i = FindEntry(key);
			if(i >= 0)
			{
				return ref _entries[i].Value;
			}

			throw new KeyNotFoundException();
		}

		public ref TValue GetValueByEntry(int idx)
		{
			return ref _entries[idx].Value;
		}

		public bool ContainsValue(TValue value)
		{
			if(value == null)
			{
				for(var i = 0; i < _count; i++)
				{
					if(_entries[i].HashCode >= 0 && _entries[i].Value == null)
					{
						return true;
					}
				}
			}
			else
			{
				var c = EqualityComparer<TValue>.Default;
				for(var i = 0; i < _count; i++)
				{
					if(_entries[i].HashCode >= 0 && c.Equals(_entries[i].Value, value))
					{
						return true;
					}
				}
			}

			return false;
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(this, Enumerator.KeyValuePair);
		}

		public TValue GetValueOrDefault(in TKey key)
		{
			int i = FindEntry(in key);
			return i >= 0 ? _entries[i].Value : default;
		}

		public int FindEntry(in TKey key)
		{
			if(key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			if(_buckets == null)
			{
				return -1;
			}

			int hashCode = Comparer.GetHashCode(key) & 0x7FFFFFFF;
			int bucket = hashCode % _buckets.Length;
			for(int i = _buckets[bucket]; i >= 0; i = _entries[i].Next)
			{
				if(_entries[i].HashCode == hashCode && Comparer.Equals(_entries[i].Key, key))
				{
					return i;
				}
			}

			return -1;
		}

		protected void CopyTo<T>(T[] array, int index)
			where T : struct, IKeyValueType
		{
			Assert.IsNotNull(array, $"{nameof(array)} != null");

			Assert.IsFalse(index < 0, $"{nameof(index)}({index}) < 0");
			Assert.IsFalse(index > array.Length, $"{nameof(index)}({index}) > {nameof(array)}.{nameof(Array.Length)}({array.Length})");

			Assert.IsFalse(
				array.Length - index < Count,
				$"{nameof(array)}.{nameof(array.Length)}({array.Length}) - {nameof(index)}({index}) < {nameof(Count)}({Count})"
			);

			int count = _count;
			Entry[] entries = _entries;
			for(var i = 0; i < count; i++)
			{
				if(entries[i].HashCode >= 0)
				{
					array[index++] = new T
					{
						Key = entries[i].Key,
						Value = entries[i].Value
					};
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected bool TryInsert(in TKey key, TValue value, out int entryIdx)
		{
			if(key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			int hashCode = Comparer.GetHashCode(key) & 0x7FFFFFFF;
			int targetBucket = hashCode % _buckets.Length;

			for(int i = _buckets[targetBucket]; i >= 0; i = _entries[i].Next)
			{
				if(_entries[i].HashCode != hashCode || !Comparer.Equals(_entries[i].Key, key))
				{
					continue;
				}

				entryIdx = i;
				return false;
			}

			if(_freeCount > 0)
			{
				entryIdx = _freeList;
				_freeList = _entries[entryIdx].Next;
				_freeCount--;
			}
			else
			{
				if(_count == _entries.Length)
				{
					Resize();
					targetBucket = hashCode % _buckets.Length;
				}

				entryIdx = _count;
				_count++;
			}

			_entries[entryIdx].HashCode = hashCode;
			_entries[entryIdx].Next = _buckets[targetBucket];
			_entries[entryIdx].Key = key;
			_entries[entryIdx].Value = value;
			_buckets[targetBucket] = entryIdx;
			_version++;

			return true;
		}

		protected virtual void Insert(in TKey key, TValue value, out int entry)
		{
			if(TryInsert(key, value, out entry))
			{
				return;
			}

			throw new ArgumentException($"Cant add duplicate key {key}");
		}

		protected virtual void ReplaceOrInsert(in TKey key, TValue value)
		{
			if(TryInsert(key, value, out int entry))
			{
				return;
			}

			ReplaceValueByEntry(ref entry, ref value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void ReplaceValueByEntry(ref int entry, ref TValue value)
		{
			_entries[entry].Value = value;
			_version++;
		}

		private void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
		{
			if(array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if(index < 0 || index > array.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			if(array.Length - index < Count)
			{
				throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
			}

			int count = _count;
			Entry[] entries = _entries;
			for(var i = 0; i < count; i++)
			{
				if(entries[i].HashCode >= 0)
				{
					array[index++] = new KeyValuePair<TKey, TValue>(entries[i].Key, entries[i].Value);
				}
			}
		}

		private void Initialize(int capacity)
		{
			int size = HashHelpers.GetPrime(capacity);
			_buckets = new int[size];
			for(var i = 0; i < _buckets.Length; i++)
			{
				_buckets[i] = -1;
			}

			_entries = new Entry[size];
			_freeList = -1;
		}

		private void Resize()
		{
			Resize(HashHelpers.ExpandPrime(_count), false);
		}

		private void Resize(int newSize, bool forceNewHashCodes)
		{
			Contract.Assert(newSize >= _entries.Length);
			var newBuckets = new int[newSize];
			for(var i = 0; i < newBuckets.Length; i++)
			{
				newBuckets[i] = -1;
			}

			var newEntries = new Entry[newSize];
			Array.Copy(_entries, 0, newEntries, 0, _count);
			if(forceNewHashCodes)
			{
				for(var i = 0; i < _count; i++)
				{
					if(newEntries[i].HashCode != -1)
					{
						newEntries[i].HashCode = Comparer.GetHashCode(newEntries[i].Key) & 0x7FFFFFFF;
					}
				}
			}

			for(var i = 0; i < _count; i++)
			{
				if(newEntries[i].HashCode < 0)
				{
					continue;
				}

				int bucket = newEntries[i].HashCode % newSize;
				newEntries[i].Next = newBuckets[bucket];
				newBuckets[bucket] = i;
			}

			_buckets = newBuckets;
			_entries = newEntries;
		}

		private static bool IsCompatibleKey(object key)
		{
			if(key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			return key is TKey;
		}

		protected interface IKeyValueType
		{
			TKey Key { get; set; }
			TValue Value { get; set; }
		}

		private struct Entry
		{
			public int HashCode; // Lower 31 bits of hash code, -1 if unused
			public int Next; // Index of next entry, -1 if last
			public TKey Key; // Key of entry
			public TValue Value; // Value of entry
		}

		[Serializable]
		public struct Enumerator
			: IEnumerator<KeyValuePair<TKey, TValue>>,
			  IDictionaryEnumerator
		{
			internal const int DictEntry = 1;
			internal const int KeyValuePair = 2;
			private readonly Map<TKey, TValue, TComparer> _map;
			private readonly int _getEnumeratorRetType; // What should Enumerator.Current return?
			private readonly int _version;
			private KeyValuePair<TKey, TValue> _current;
			private int _index;

			internal Enumerator(Map<TKey, TValue, TComparer> map, int getEnumeratorRetType)
			{
				_map = map;
				_index = 0;
				_version = map._version;
				_getEnumeratorRetType = getEnumeratorRetType;
				_current = new KeyValuePair<TKey, TValue>();
			}

			public TKey CurrentKey => _current.Key;
			public TValue CurrentValue => _current.Value;

			public ref TValue CurrentValueRef => ref _map._entries[_index - 1].Value;

#region IDictionaryEnumerator Implementation

			DictionaryEntry IDictionaryEnumerator.Entry
			{
				get
				{
					if(_index == 0 || _index == _map._count + 1)
					{
						throw new InvalidOperationException();
					}

					return new DictionaryEntry(_current.Key, _current.Value);
				}
			}

			object IDictionaryEnumerator.Key
			{
				get
				{
					if(_index == 0 || _index == _map._count + 1)
					{
						throw new InvalidOperationException();
					}

					return _current.Key;
				}
			}

			object IDictionaryEnumerator.Value
			{
				get
				{
					if(_index == 0 || _index == _map._count + 1)
					{
						throw new InvalidOperationException();
					}

					return _current.Value;
				}
			}

#endregion

#region IDisposable Implementation

			public void Dispose() { }

#endregion

#region IEnumerator Implementation

			object IEnumerator.Current
			{
				get
				{
					if(_index == 0 || _index == _map._count + 1)
					{
						throw new InvalidOperationException();
					}

					if(_getEnumeratorRetType == DictEntry)
					{
						return new DictionaryEntry(_current.Key, _current.Value);
					}

					return new KeyValuePair<TKey, TValue>(_current.Key, _current.Value);
				}
			}

			public bool MoveNext()
			{
				if(_version != _map._version)
				{
					throw new InvalidOperationException("Failed version");
				}

				// Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
				// dictionary.count+1 could be negative if dictionary.count is Int32.MaxValue
				while((uint)_index < (uint)_map._count)
				{
					if(_map._entries[_index].HashCode >= 0)
					{
						_current = new KeyValuePair<TKey, TValue>(_map._entries[_index].Key, _map._entries[_index].Value);
						_index++;
						return true;
					}

					_index++;
				}

				_index = _map._count + 1;
				_current = new KeyValuePair<TKey, TValue>();
				return false;
			}

			void IEnumerator.Reset()
			{
				if(_version != _map._version)
				{
					throw new InvalidOperationException("Failed version");
				}

				_index = 0;
				_current = new KeyValuePair<TKey, TValue>();
			}

#endregion

#region IEnumerator<KeyValuePair<TKey,TValue>> Implementation

			public KeyValuePair<TKey, TValue> Current => _current;

#endregion
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[Serializable]
		public sealed class KeyCollection : ICollection<TKey>, ICollection, IReadOnlyCollection<TKey>
		{
			private readonly Map<TKey, TValue, TComparer> _map;

			public KeyCollection(Map<TKey, TValue, TComparer> map)
			{
				_map = map ?? throw new ArgumentNullException(nameof(map));
			}

#region ICollection Implementation

			bool ICollection.IsSynchronized => false;

			object ICollection.SyncRoot => ((ICollection)_map).SyncRoot;

			void ICollection.CopyTo(Array array, int index)
			{
				if(array == null)
				{
					throw new ArgumentNullException(nameof(array));
				}

				if(array.Rank != 1)
				{
					throw new ArgumentException("Arg_RankMultiDimNotSupported");
				}

				if(array.GetLowerBound(0) != 0)
				{
					throw new ArgumentException("Arg_NonZeroLowerBound");
				}

				if(index < 0 || index > array.Length)
				{
					throw new ArgumentOutOfRangeException(nameof(index));
				}

				if(array.Length - index < _map.Count)
				{
					throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
				}

				if(array is TKey[] keys)
				{
					CopyTo(keys, index);
				}
				else
				{
					if(!(array is object[] objects))
					{
						throw new ArgumentException("Argument_InvalidArrayType");
					}

					int count = _map._count;
					Entry[] entries = _map._entries;
					try
					{
						for(var i = 0; i < count; i++)
						{
							if(entries[i].HashCode >= 0)
							{
								objects[index++] = entries[i].Key;
							}
						}
					}
					catch(ArrayTypeMismatchException)
					{
						throw new ArgumentException("Argument_InvalidArrayType");
					}
				}
			}

#endregion

#region ICollection<TKey> Implementation

			public int Count => _map.Count;

			bool ICollection<TKey>.IsReadOnly => true;

			public void CopyTo(TKey[] array, int index)
			{
				if(array == null)
				{
					throw new ArgumentNullException(nameof(array));
				}

				if(index < 0 || index > array.Length)
				{
					throw new ArgumentOutOfRangeException(nameof(index));
				}

				if(array.Length - index < _map.Count)
				{
					throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
				}

				int count = _map._count;
				Entry[] entries = _map._entries;
				for(var i = 0; i < count; i++)
				{
					if(entries[i].HashCode >= 0)
					{
						array[index++] = entries[i].Key;
					}
				}
			}

			void ICollection<TKey>.Add(TKey item)
			{
				throw new NotSupportedException();
			}

			void ICollection<TKey>.Clear()
			{
				throw new NotSupportedException();
			}

			bool ICollection<TKey>.Contains(TKey item)
			{
				return _map.ContainsKey(item);
			}

			bool ICollection<TKey>.Remove(TKey item)
			{
				throw new NotSupportedException();
			}

#endregion

#region IEnumerable Implementation

			IEnumerator IEnumerable.GetEnumerator()
			{
				return new Enumerator(_map);
			}

#endregion

#region IEnumerable<TKey> Implementation

			IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
			{
				return new Enumerator(_map);
			}

#endregion

			public Enumerator GetEnumerator()
			{
				return new Enumerator(_map);
			}

			[Serializable]
			public struct Enumerator : IEnumerator<TKey>
			{
				private readonly Map<TKey, TValue, TComparer> _map;
				private readonly int _version;
				private int _index;

				internal Enumerator(Map<TKey, TValue, TComparer> map)
				{
					_map = map;
					_index = 0;
					_version = map._version;
					Current = default;
				}

#region IDisposable Implementation

				public void Dispose() { }

#endregion

#region IEnumerator Implementation

				object IEnumerator.Current
				{
					get
					{
						if(_index == 0 || _index == _map._count + 1)
						{
							throw new InvalidOperationException();
						}

						return Current;
					}
				}

				public bool MoveNext()
				{
					if(_version != _map._version)
					{
						throw new InvalidOperationException("Failed version");
					}

					while((uint)_index < (uint)_map._count)
					{
						if(_map._entries[_index].HashCode >= 0)
						{
							Current = _map._entries[_index].Key;
							_index++;
							return true;
						}

						_index++;
					}

					_index = _map._count + 1;
					Current = default;
					return false;
				}

				void IEnumerator.Reset()
				{
					if(_version != _map._version)
					{
						throw new InvalidOperationException("Failed version");
					}

					_index = 0;
					Current = default;
				}

#endregion

#region IEnumerator<TKey> Implementation

				public TKey Current { get; private set; }

#endregion
			}
		}

		[DebuggerDisplay("Count = {" + nameof(Count) + "}")]
		[Serializable]
		public sealed class ValueCollection : ICollection<TValue>, ICollection, IReadOnlyCollection<TValue>
		{
			private readonly Map<TKey, TValue, TComparer> _map;

			public ValueCollection(Map<TKey, TValue, TComparer> map)
			{
				_map = map ?? throw new ArgumentNullException(nameof(map));
			}

#region ICollection Implementation

			bool ICollection.IsSynchronized => false;

			object ICollection.SyncRoot => ((ICollection)_map).SyncRoot;

			void ICollection.CopyTo(Array array, int index)
			{
				if(array == null)
				{
					throw new ArgumentNullException(nameof(array));
				}

				if(array.Rank != 1)
				{
					throw new ArgumentException("Arg_RankMultiDimNotSupported");
				}

				if(array.GetLowerBound(0) != 0)
				{
					throw new ArgumentException("Arg_NonZeroLowerBound");
				}

				if(index < 0 || index > array.Length)
				{
					throw new ArgumentOutOfRangeException(nameof(index));
				}

				if(array.Length - index < _map.Count)
				{
					throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
				}

				if(array is TValue[] values)
				{
					CopyTo(values, index);
				}
				else
				{
					if(!(array is object[] objects))
					{
						throw new ArgumentException("Argument_InvalidArrayType");
					}

					int count = _map._count;
					Entry[] entries = _map._entries;
					try
					{
						for(var i = 0; i < count; i++)
						{
							if(entries[i].HashCode >= 0)
							{
								objects[index++] = entries[i].Value;
							}
						}
					}
					catch(ArrayTypeMismatchException)
					{
						throw new ArgumentException("Argument_InvalidArrayType");
					}
				}
			}

#endregion

#region ICollection<TValue> Implementation

			public int Count => _map.Count;

			bool ICollection<TValue>.IsReadOnly => true;

			public void CopyTo(TValue[] array, int index)
			{
				if(array == null)
				{
					throw new ArgumentNullException(nameof(array));
				}

				if(index < 0 || index > array.Length)
				{
					throw new ArgumentOutOfRangeException(nameof(index));
				}

				if(array.Length - index < _map.Count)
				{
					throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
				}

				int count = _map._count;
				Entry[] entries = _map._entries;
				for(var i = 0; i < count; i++)
				{
					if(entries[i].HashCode >= 0)
					{
						array[index++] = entries[i].Value;
					}
				}
			}

			void ICollection<TValue>.Add(TValue item)
			{
				throw new NotSupportedException();
			}

			bool ICollection<TValue>.Remove(TValue item)
			{
				throw new NotSupportedException();
			}

			void ICollection<TValue>.Clear()
			{
				throw new NotSupportedException();
			}

			bool ICollection<TValue>.Contains(TValue item)
			{
				return _map.ContainsValue(item);
			}

#endregion

#region IEnumerable Implementation

			IEnumerator IEnumerable.GetEnumerator()
			{
				return new Enumerator(_map);
			}

#endregion

#region IEnumerable<TValue> Implementation

			IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
			{
				return new Enumerator(_map);
			}

#endregion

			public Enumerator GetEnumerator()
			{
				return new Enumerator(_map);
			}

			[Serializable]
			public struct Enumerator : IEnumerator<TValue>
			{
				private readonly Map<TKey, TValue, TComparer> _map;
				private readonly int _version;
				private int _index;

				internal Enumerator(Map<TKey, TValue, TComparer> map)
				{
					_map = map;
					_index = 0;
					_version = map._version;
					Current = default;
				}

#region IDisposable Implementation

				public void Dispose() { }

#endregion

#region IEnumerator Implementation

				object IEnumerator.Current
				{
					get
					{
						if(_index == 0 || _index == _map._count + 1)
						{
							throw new InvalidOperationException();
						}

						return Current;
					}
				}

				public bool MoveNext()
				{
					if(_version != _map._version)
					{
						throw new InvalidOperationException("Failed version");
					}

					while((uint)_index < (uint)_map._count)
					{
						if(_map._entries[_index].HashCode >= 0)
						{
							Current = _map._entries[_index].Value;
							_index++;
							return true;
						}

						_index++;
					}

					_index = _map._count + 1;
					Current = default;
					return false;
				}

				void IEnumerator.Reset()
				{
					if(_version != _map._version)
					{
						throw new InvalidOperationException("Failed version");
					}

					_index = 0;
					Current = default;
				}

#endregion

#region IEnumerator<TValue> Implementation

				public TValue Current { get; private set; }

#endregion
			}
		}
	}
}