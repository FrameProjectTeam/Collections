namespace Fp.Collections
{
	public interface IReadOnlyLinkedList<T>
	{
		int FirstIdx { get; }
		int LastIdx { get; }
		
		int Count { get; }
		int Capacity { get; }
        
		T GetValue(int nodeIdx);
        
		bool HasValue(int nodeIdx);
		bool HasValue(ref int nodeIdx);
		
		bool TryGetFirst(out int nodeIdx);
		bool TryGetLast(out int nodeIdx);
		
		bool TryGetNext(int nodeIdx, out int nextIdx);
		bool TryGetPrevious(int nodeIdx, out int previousIdx);
        
		int GetNext(int nodeIdx);
		int GetPrevious(int nodeIdx);
		
		int GetNext(ref int nodeIdx);
		int GetPrevious(ref int nodeIdx);
		
		void MoveNext(ref int nodeIdx);
		void MovePrevious(ref int nodeIdx);
	}
}