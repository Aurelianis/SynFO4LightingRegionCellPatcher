//This file is from radj307 UniqueRegionNamesPatcher
//https://github.com/radj307/UniqueRegionNamesPatcher

using Noggog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightingAndRegionPatcher.Extensions
{
    public static class ListExtensions
    {
        /// <summary>
        /// Adds <paramref name="item"/> to the given <paramref name="list"/> if the list doesn't already contain the item.
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="list">List to add unique items into</param>
        /// <param name="item"></param>
        /// <returns><see langword="true"/> when <paramref name="item"/> is unique and was inserted into <paramref name="list"/>; otherwise <see langword="false"/>.</returns>
        public static bool AddIfUnique<T>(this IList<T> list, T? item)
        {
            if (item is not null && !list.Contains(item))
            {
                list.Add(item);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Adds any number of <paramref name="items"/> to the given <paramref name="list"/> if the list doesn't already contain them.
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="list">List to add unique items into</param>
        /// <param name="items">Enumerable items</param>
        /// <returns>The number of unique <paramref name="items"/> that were added to the <paramref name="list"/>.</returns>
        public static int AddRangeIfUnique<T>(this IList<T> list, IEnumerable<T> items)
        {
            int count = 0;
            items.ForEach(item => count += (list.AddIfUnique(item) ? 1 : 0));
            return count;
        }
        public static bool Contains<T>(this IReadOnlyList<T> list, T? item)
            => item is not null && list.Any(i => item.Equals(i));
        public static bool ContainsAll<T>(this IReadOnlyList<T> list, IEnumerable<T> items)
        {
            foreach (var item in items)
                if (!list.Contains(item))
                    return false;
            return true;
        }
    }
}
