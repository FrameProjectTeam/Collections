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

        /// <summary>
        ///     Swap values internally without version changes
        /// </summary>
        /// <param name="first">First node of this sequence</param>
        /// <param name="second">Second node of this sequence</param>
        void SwapValuesSilent(in FpLinkedListNode<T> first, in FpLinkedListNode<T> second);

        /// <summary>
        ///     Swap values and refresh nodes to valid state
        /// </summary>
        /// <param name="first">First node of this sequence</param>
        /// <param name="second">Second node of this sequence</param>
        void SwapValues(ref FpLinkedListNode<T> first, ref FpLinkedListNode<T> second);
    }
}