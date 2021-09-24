using System;

namespace Fp.Collections
{
    public readonly struct FpLinkedListNode<T>
    {
        public static FpLinkedListNode<T> Invalid = new FpLinkedListNode<T>(null, -1, -1);

        internal readonly FpLinkedList<T> List;
        internal readonly int Idx;
        internal readonly int Version;

        internal FpLinkedListNode(FpLinkedList<T> list, int idx, int version)
        {
            List = list;
            Idx = idx;
            Version = version;
        }

        public T Value => IsValid ? List.GetValue(in this) : throw new InvalidOperationException("This node is invalid");

        public bool IsValid => List?.IsValidNode(in this) ?? false;
    }
}