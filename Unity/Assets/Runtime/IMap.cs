using System.Collections;
using System.Collections.Generic;

namespace Fp.Collections
{
	public interface IMap<TKey, TValue> : IReadOnlyMap<TKey, TValue>, IDictionary, IDictionary<TKey, TValue>
	{
		new TValue this[in TKey key] { get; set; }
		int Add(in TKey key, TValue value);
		bool Remove(in TKey key);
		bool TryAdd(in TKey key, TValue value);
		void SetAllValuesTo(TValue value = default);
		bool TryRemove(in TKey key, out TValue value);
		TValue GetValueOrDefault(in TKey key);
	}
	
	public interface IMap<TKey, TValue, out TComparer> : IMap<TKey, TValue>
		where TComparer : IEqualityComparer<TKey>
	{
		TComparer Comparer { get; }
	}
}