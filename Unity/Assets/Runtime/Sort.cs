using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fp.Collections
{
    public static class Sort
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void QuickSort<T>(this IList<T> arr, IComparer<T> comparer, int lo, int hi)
        {
            QuickSort<T, IComparer<T>>(arr, comparer, lo, hi);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void QuickSort<T, TCmp>(this IList<T> arr, TCmp comparer, int lo, int hi)
            where TCmp : IComparer<T>
        {
            if (lo >= hi)
            {
                return;
            }

            int pi = Partition(arr, comparer, lo, hi);

            QuickSort(arr, comparer, lo, pi - 1);
            QuickSort(arr, comparer, pi + 1, hi);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Partition<T>(IList<T> arr, IComparer<T> comparer, int lo, int hi)
        {
            return Partition<T, IComparer<T>>(arr, comparer, lo, hi);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Partition<T, TCmp>(IList<T> arr, TCmp comparer, int lo, int hi)
            where TCmp : IComparer<T>
        {
            T pivot = arr[lo];
            while (true)
            {
                while (comparer.Compare(arr[lo], pivot) < 0)
                {
                    lo++;
                }

                while (comparer.Compare(arr[hi], pivot) > 0)
                {
                    hi--;
                }

                if (lo < hi)
                {
                    if (comparer.Compare(arr[lo], arr[hi]) == 0)
                    {
                        return hi;
                    }

                    Swap(arr, lo, hi);
                }
                else
                {
                    return hi;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap<T>(IList<T> arr, int a, int b)
        {
            (arr[a], arr[b]) = (arr[b], arr[a]);
        }
    }
}