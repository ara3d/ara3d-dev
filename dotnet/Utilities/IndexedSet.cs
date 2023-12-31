﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Ara3D
{
    /// <summary>
    /// This class is similar to a hash set, but it allows us to create an ordered
    /// list for each value added in the order it was first seen. 
    /// </summary>
    public class IndexedSet<K> : ConcurrentDictionary<K, int>
    {
        public IndexedSet()
        { }

        public IndexedSet(IEnumerable<K> values)
        {
            foreach (var x in values)
                Add(x);
        }

        public int Add(K k)
        {
            return GetOrAdd(k, (k2) => Count);
        }

        public IEnumerable<K> OrderedKeys()
            => this.OrderBy(kv => kv.Value).Select(kv => kv.Key);

        public List<K> ToOrderedList()
            => OrderedKeys().ToList();

        public K[] ToOrderedArray()
            => OrderedKeys().ToArray();
    }
}
