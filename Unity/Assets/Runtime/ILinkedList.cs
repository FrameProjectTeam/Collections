namespace Fp.Collections
{
	public interface ILinkedList<T> : IReadOnlyLinkedList<T>
	{
		int AddFirst(T value);
		void AddFirst(T value, out int nodeIdx);

		int AddLast(T value);
		void AddLast(T value, out int node);

		void AddAfter(int nodeIdx, T value, out int inserted);
		int AddAfter(int nodeIdx, T value);
		void AddBefore(int nodeIdx, T value, out int inserted);
		int AddBefore(int nodeIdx, T value);

		void Remove(int nodeIdx);

		ref T GetValueRef(ref int nodeIdx);

		void SetValue(int node, T value);

		/// <summary>
		///     Clear uses chain information. More effective if capacity much greater than count of exist element in linked list
		/// </summary>
		void ChainClear();

		/// <summary>
		///     Clear uses capacity of internal buffers
		/// </summary>
		void Clear();

		/// <summary>
		///     Swap values by node indices
		/// </summary>
		/// <param name="firstIdx">First node index</param>
		/// <param name="secondIdx">Second node index</param>
		void Swap(int firstIdx, int secondIdx);
	}
}