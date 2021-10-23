namespace Fp.Collections
{
    public interface ILinkedList<T>
    {
        FpLinkedListNode<T> First { get; }
        FpLinkedListNode<T> Last { get; }
        int Count { get; }
        int Capacity { get; }

        FpLinkedListNode<T> AddFirst(T value);
        void AddFirst(T value, out FpLinkedListNode<T> node);

        FpLinkedListNode<T> AddLast(T value);
        void AddLast(T value, out FpLinkedListNode<T> node);

        void AddAfter(in FpLinkedListNode<T> node, T value, out FpLinkedListNode<T> insertedNode);
        FpLinkedListNode<T> AddAfter(in FpLinkedListNode<T> node, T value);
        void AddBefore(in FpLinkedListNode<T> node, T value, out FpLinkedListNode<T> insertedNode);
        FpLinkedListNode<T> AddBefore(in FpLinkedListNode<T> node, T value);
        
        void Remove(in FpLinkedListNode<T> node);

        T GetValue(in FpLinkedListNode<T> node);
        void SetValue(in FpLinkedListNode<T> node, T value);

        bool TryGetNextNode(in FpLinkedListNode<T> node, out FpLinkedListNode<T> next);
        bool TryGetPreviousNode(in FpLinkedListNode<T> node, out FpLinkedListNode<T> previous);
        void Clear();
        bool TryGetFirst(out FpLinkedListNode<T> node);
        bool TryGetLast(out FpLinkedListNode<T> node);
    }
}