using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fp.Collections
{
    public static class BisectUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int InSortRight<T>(
            this IList<T> list,
            T value,
            int leftIdx,
            int rightIdx)
        {
            return InSortRight(list, value, leftIdx, rightIdx, Comparer<T>.Default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int InSortRight<T>(
            this IList<T> list,
            T value,
            int leftIdx,
            int rightIdx,
            IComparer<T> comparer)
        {
            /*
            Assert.IsNotNull(list);
            Assert.IsNotNull(comparer);
            Assert.IsTrue(leftIdx >= 0, "leftIdx >= 0");
            Assert.IsTrue(rightIdx >= 0, "rightIdx >= 0");
            Assert.IsTrue(leftIdx <= rightIdx, "leftIdx <= rightIdx");
            */

            while (leftIdx < rightIdx)
            {
                int mid = (leftIdx + rightIdx) / 2;
                int compare = comparer.Compare(value, list[mid]);

                if (compare < 0)
                {
                    rightIdx = mid;
                }
                else
                {
                    leftIdx = mid + 1;
                }
            }

            return leftIdx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int InSortLeft<T>(
            this IList<T> list,
            T value,
            int leftIdx,
            int rightIdx)
        {
            return InSortLeft(list, value, leftIdx, rightIdx, Comparer<T>.Default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int InSortLeft<T>(
            this IList<T> list,
            T value,
            int leftIdx,
            int rightIdx,
            IComparer<T> comparer)
        {
            /*
            Assert.IsNotNull(list);
            Assert.IsNotNull(comparer);
            Assert.IsTrue(leftIdx >= 0, "idx >= 0");
            Assert.IsTrue(rightIdx >= 0, "count >= 0");
            Assert.IsTrue(leftIdx <= rightIdx, "leftIdx <= rightIdx");
            */

            while (leftIdx < rightIdx)
            {
                int mid = (leftIdx + rightIdx) / 2;
                int compare = comparer.Compare(list[mid], value);

                if (compare < 0)
                {
                    leftIdx = mid + 1;
                }
                else
                {
                    rightIdx = mid;
                }
            }

            return leftIdx;
        }
    }
}