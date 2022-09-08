using System.Collections.Generic;

namespace Fp.Collections
{
	public interface IReadOnlyMap<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
	{
		TValue this[in TKey key] { get; }
		bool ContainsKey(in TKey key);
		bool TryGetValue(in TKey key, out TValue value, TValue failure = default);
		bool TryGetAndCast<T>(in TKey key, out T value, T failure = default);
		ref TValue GetValue(in TKey key);
		ref TValue GetValueByEntry(int entry);
		bool ContainsValue(TValue value);
		int FindEntry(in TKey key);
	}
}