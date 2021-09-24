using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fp.Collections
{
    public static class CollectionUtils
    {
        private static readonly Random Random = new Random();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Count<TEnumerator>(this TEnumerator enumerable)
            where TEnumerator : class, IEnumerable
        {
            /*Assert.IsNotNull(enumerable);*/

            switch (enumerable)
            {
                case ICollection nonGenericCollection:
                {
                    return nonGenericCollection.Count;
                }
                default:
                {
                    var count = 0;
                    IEnumerator iterator = enumerable.GetEnumerator();
                    while (iterator.MoveNext())
                    {
                        count++;
                    }

                    return count;
                }
            }
        }

        /// <summary>
        ///     Get a random value from list
        /// </summary>
        /// <param name="list">Target list</param>
        /// <typeparam name="T">Type of list arguments</typeparam>
        /// <returns>Random value</returns>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetRandom<T>(this IReadOnlyList<T> list)
        {
            /*Assert.IsNotNull(list);*/
            int count = list.Count;

            switch (count)
            {
                case 0:
                {
                    return default;
                }
                case 1:
                {
                    return list[0];
                }
                default:
                {
                    return list[Random.Next(0, count)];
                }
            }
        }

        public static List<TValue> SafeGetList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key)
        {
            return dictionary.GetCollection<TKey, TValue, List<TValue>>(key);
        }

        /// <summary>
        ///     Swaps two element in list
        /// </summary>
        /// <param name="list">Target list</param>
        /// <param name="firstIndex">First element index</param>
        /// <param name="secondIndex">Second element index</param>
        /// <typeparam name="T">Generic type of List element</typeparam>
        /// <returns>Fluent interface</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IList<T> Swap<T>(this IList<T> list, int firstIndex, int secondIndex)
        {
            /*
            Assert.IsNotNull(list);
            Assert.IsTrue(firstIndex >= 0, $"{nameof(firstIndex)} >= 0, {firstIndex}");
            Assert.IsTrue(firstIndex < list.Count, $"{nameof(firstIndex)} < {nameof(list)}.{nameof(List<T>.Count)}, {firstIndex}, {list.Count}");
            Assert.IsTrue(secondIndex >= 0, $"{nameof(secondIndex)} >= 0, {secondIndex}");
            Assert.IsTrue(secondIndex < list.Count, $"{nameof(secondIndex)} <  {nameof(list)}.{nameof(List<T>.Count)}, {secondIndex}, {list.Count}");
            */

            if (firstIndex == secondIndex)
            {
                return list;
            }

            (list[firstIndex], list[secondIndex]) = (list[secondIndex], list[firstIndex]);

            return list;
        }

        /// <summary>
        ///     Fisher–Yates shuffle
        /// </summary>
        /// <param name="list">Target list</param>
        /// <param name="offset">Shuffle with offset</param>
        /// <param name="count">Shuffle count elements</param>
        /// <param name="random">Shuffle based on this random</param>
        /// <typeparam name="T">List generic argument</typeparam>
        public static void Shuffle<T>(this IList<T> list, int offset, int count, Random random)
        {
            /*
            Assert.IsNotNull(list);
            Assert.IsTrue(offset >= 0, $"{nameof(offset)} >= 0, {offset}");
            Assert.IsTrue(offset < list.Count,
                          $"{nameof(offset)} < {nameof(list)}.{nameof(List<T>.Count)}, {offset}, {list.Count}");
            Assert.IsTrue(count >= 0, $"{nameof(count)} > 0");
            Assert.IsTrue(offset + count <= list.Count,
                          $"{nameof(offset)} + {nameof(count)} <= {nameof(list)}.{nameof(List<T>.Count)}, {offset}, {count}, {list.Count}");
            */

            if (count == 0)
            {
                return;
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            for (int i = offset + count - 1; i > offset; i--)
            {
                // Pick a random index
                int j = random.Next(offset, i + 1);

                // Swap arr[i] with the
                // element at random index
                Swap(list, i, j);
            }
        }

        /// <summary>
        ///     Removes item from the middle of list and copy last item to removed place
        /// </summary>
        /// <param name="list">Target list</param>
        /// <param name="removedItemIdx">Removing element index</param>
        /// <typeparam name="T">Type of list element</typeparam>
        /// <returns>Fluent interface</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IList<T> SwapAndRemove<T>(this IList<T> list, int removedItemIdx)
        {
            /*
            Assert.IsNotNull(list);
            Assert.IsTrue(removedItemIdx >= 0 && removedItemIdx < list.Count);
            */

            int lastIdx = list.Count - 1;

            if (removedItemIdx != lastIdx)
            {
                list[removedItemIdx] = list[lastIdx];
            }

            list.RemoveAt(lastIdx);
            return list;
        }

        /// <summary>
        ///     Add element to list from dictionary lists.
        ///     Previously it's creating list and adding to dictionary if it's needed.
        /// </summary>
        /// <param name="dictionary">Target dictionary</param>
        /// <param name="key">The key of the list object</param>
        /// <param name="value">The object to use as the value of the element to add</param>
        /// <typeparam name="TKey">Dictionary key type</typeparam>
        /// <typeparam name="TValue">Dictionary list value type</typeparam>
        /// <typeparam name="TCollection">Collections contains in dictionary</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddToCollection<TKey, TValue, TCollection>(
            this IDictionary<TKey, TCollection> dictionary,
            TKey key,
            TValue value)
            where TCollection : ICollection<TValue>, new()
        {
            dictionary.GetCollection<TKey, TValue, TCollection>(key).Add(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TCollection GetCollection<TKey, TValue, TCollection>(this IDictionary<TKey, TCollection> dictionary, TKey key)
            where TCollection : ICollection<TValue>, new()
        {
            if (dictionary.TryGetValue(key, out TCollection collection))
            {
                return collection;
            }

            collection = new TCollection();
            dictionary[key] = collection;

            return collection;
        }

        /// <summary>
        ///     Removes element from list in dictionary
        /// </summary>
        /// <param name="dictionary">Target dictionary</param>
        /// <param name="key">The key of the list object</param>
        /// <param name="value">The object to use as the value of the element to remove</param>
        /// <param name="removeEmptyCollection">When list is empty after element removing it also remove list too</param>
        /// <typeparam name="TKey">Dictionary key type</typeparam>
        /// <typeparam name="TValue">Dictionary list value type</typeparam>
        /// <typeparam name="TCollection">Collections contains in dictionary</typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveAtCollection<TKey, TValue, TCollection>(
            this IDictionary<TKey, TCollection> dictionary,
            TKey key,
            TValue value,
            bool removeEmptyCollection = false)
            where TCollection : ICollection<TValue>, new()
        {
            bool result = dictionary.TryGetValue(key, out TCollection values) && values.Remove(value);
            if (result && removeEmptyCollection && values.Count <= 0)
            {
                dictionary.Remove(key);
            }

            return result;
        }

        public static void EnqueueToQueue<TDictionary, TKey, TValue, TQueue>(
            this TDictionary dictionary,
            TKey key,
            TValue value)
            where TQueue : Queue<TValue>, new()
            where TDictionary : IDictionary<TKey, TQueue>
        {
            if (!dictionary.TryGetValue(key, out TQueue values))
            {
                values = new TQueue();
                dictionary[key] = values;
            }

            values.Enqueue(value);
        }

        public static bool TryPeekQueue<TDictionary, TKey, TValue, TQueue>(this TDictionary dictionary, TKey key, out TValue value)
            where TQueue : Queue<TValue>
            where TDictionary : IDictionary<TKey, TQueue>
        {
            if (dictionary.TryGetValue(key, out TQueue values) && values.Count > 0)
            {
                value = values.Peek();
                return true;
            }

            value = default;
            return false;
        }

        public static bool TryDequeueQueue<TDictionary, TKey, TValue, TQueue>(
            this TDictionary dictionary,
            TKey key,
            out TValue value)
            where TQueue : Queue<TValue>
            where TDictionary : IDictionary<TKey, TQueue>
        {
            return dictionary.TryDequeueQueue<TDictionary, TKey, TValue, TQueue>(key, false, out value);
        }

        public static bool TryDequeueQueue<TDictionary, TKey, TValue, TQueue>(
            this TDictionary dictionary,
            TKey key,
            bool removeEmptyQueue,
            out TValue value)
            where TQueue : Queue<TValue>
            where TDictionary : IDictionary<TKey, TQueue>
        {
            if (dictionary.TryGetValue(key, out TQueue values) && values.Count > 0)
            {
                value = values.Dequeue();
                if (removeEmptyQueue && values.Count <= 0)
                {
                    dictionary.Remove(key);
                }

                return true;
            }

            value = default;
            return false;
        }

        public static void SwapAndShiftElements<T>(this T[] array, int oldIndex, int newIndex)
        {
            if (oldIndex == newIndex)
            {
                return;
            }

            T tmp = array[oldIndex];
            if (newIndex < oldIndex)
            {
                Array.Copy(array, newIndex, array, newIndex + 1, oldIndex - newIndex);
            }
            else
            {
                Array.Copy(array, oldIndex + 1, array, oldIndex, newIndex - oldIndex);
            }

            array[newIndex] = tmp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetBoundsInSort<T>(
            this IList<T> list,
            T minValue,
            T maxValue,
            ref int leftIdx,
            ref int rightIdx)
        {
            return GetBoundsInSort(list, minValue, maxValue, ref leftIdx, ref rightIdx, Comparer<T>.Default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetBoundsInSort<T>(
            this IList<T> list,
            T minValue,
            T maxValue,
            ref int leftIdx,
            ref int rightIdx,
            IComparer<T> comparer)
        {
            /*
            Assert.IsNotNull(list);
            Assert.IsNotNull(comparer);

            Assert.IsTrue(leftIdx >= 0, "leftIdx >= 0");
            Assert.IsTrue(rightIdx >= 0, "rightIdx >= 0");
            Assert.IsTrue(leftIdx <= rightIdx, "leftIdx <= rightIdx");

            Assert.IsTrue(list.Count >= leftIdx + rightIdx, "list.Count >= index + count");
            Assert.IsTrue(IsOrdered(list, leftIdx, rightIdx), "IsOrdered(list)");
            Assert.IsTrue(Comparer<T>.Default.Compare(minValue, maxValue) <= 0, "minValue < maxValue");
            */

            //Find left bound
            int li = list.InSortLeft(minValue, 0, rightIdx, comparer);
            //Find right bound
            int ri = list.InSortRight(maxValue, li, rightIdx, comparer);

            if (li == ri && (li == 0 || ri == rightIdx))
            {
                return false;
            }

            leftIdx = li;
            rightIdx = ri;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOrdered<T>(this IList<T> list, int startIdx, int endIdx)
        {
            return IsOrdered(list, startIdx, endIdx, Comparer<T>.Default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOrdered<T>(this IList<T> list, int startIdx, int endIdx, IComparer<T> comparer)
        {
            /*
            Assert.IsNotNull(list);
            Assert.IsNotNull(comparer);

            Assert.IsTrue(startIdx >= 0, "startIdx >= 0");
            Assert.IsTrue(endIdx >= 0, "endIdx >= 0");
            Assert.IsTrue(startIdx < list.Count, "startIdx < list.Count");
            Assert.IsTrue(endIdx <= list.Count, "endIdx <= list.Count");
            Assert.IsTrue(startIdx <= endIdx, "startIdx <= endIdx");
            */

            if (endIdx - startIdx <= 1)
            {
                return true;
            }

            for (int i = startIdx + 1; i < endIdx; i++)
            {
                if (comparer.Compare(list[i - 1], list[i]) > 0)
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T>(this IReadOnlyList<T> array, int index, int length, T target)
            where T : struct, IComparable<T>
        {
            return BinarySearch(array, index, length, target, ComparerDefault<T>.Instance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T, TComparer>(
            this IReadOnlyList<T> array,
            int index,
            int length,
            T target,
            TComparer comparer)
            where T : struct
            where TComparer : IComparer<T>
        {
            int lower = index;
            int upper = index + length - 1;
            while (lower <= upper)
            {
                int middle = lower + ((upper - lower) >> 1);
                int compareTo = comparer.Compare(array[middle], target);
                if (compareTo == 0)
                {
                    return middle;
                }

                if (compareTo < 0)
                {
                    lower = middle + 1;
                }
                else
                {
                    upper = middle - 1;
                }
            }

            return ~lower;
        }

        private readonly struct ComparerDefault<T> : IComparer<T>
            where T : struct, IComparable<T>
        {
            public static readonly ComparerDefault<T> Instance = new ComparerDefault<T>();

#region IComparer<T> Implementation

            public int Compare(T x, T y)
            {
                return x.CompareTo(y);
            }

#endregion
        }
    }
}