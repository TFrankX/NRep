using System.Collections;
using System.Collections.Concurrent;

namespace WebServer.Data
{
    /// <summary>
    /// Thread-safe wrapper around ConcurrentDictionary that exposes List-like interface.
    /// Provides O(1) lookups by Id while maintaining compatibility with existing code.
    /// </summary>
    public class ThreadSafeList<T> : IEnumerable<T> where T : class
    {
        private readonly ConcurrentDictionary<ulong, T> _items = new();
        private readonly Func<T, ulong> _keySelector;

        public ThreadSafeList(Func<T, ulong> keySelector)
        {
            _keySelector = keySelector;
        }

        public int Count => _items.Count;

        public void Add(T item)
        {
            var key = _keySelector(item);
            _items.TryAdd(key, item);
        }

        public bool TryAdd(T item)
        {
            var key = _keySelector(item);
            return _items.TryAdd(key, item);
        }

        public void AddOrUpdate(T item)
        {
            var key = _keySelector(item);
            _items[key] = item;
        }

        public bool Remove(T item)
        {
            var key = _keySelector(item);
            return _items.TryRemove(key, out _);
        }

        public bool RemoveById(ulong id)
        {
            return _items.TryRemove(id, out _);
        }

        public T? GetById(ulong id)
        {
            _items.TryGetValue(id, out var item);
            return item;
        }

        public bool ContainsId(ulong id)
        {
            return _items.ContainsKey(id);
        }

        public int FindIndex(Predicate<T> match)
        {
            // For compatibility - returns 0 if found, -1 if not
            // Note: actual index is meaningless in concurrent dictionary
            foreach (var item in _items.Values)
            {
                if (match(item))
                    return 0; // Found
            }
            return -1; // Not found
        }

        public T? FirstOrDefault(Func<T, bool> predicate)
        {
            foreach (var item in _items.Values)
            {
                if (predicate(item))
                    return item;
            }
            return null;
        }

        /// <summary>
        /// Gets item by Id and applies action to it. Thread-safe update pattern.
        /// Returns true if item was found and action applied.
        /// </summary>
        public bool UpdateById(ulong id, Action<T> updateAction)
        {
            if (_items.TryGetValue(id, out var item))
            {
                updateAction(item);
                return true;
            }
            return false;
        }

        public T this[int index]
        {
            get
            {
                // For compatibility with code that uses [0] after FindIndex
                // This is O(n) but maintains compatibility
                return _items.Values.ElementAt(index);
            }
        }

        public IEnumerable<T> Where(Func<T, bool> predicate)
        {
            return _items.Values.Where(predicate);
        }

        public List<T> ToList()
        {
            return _items.Values.ToList();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Clear()
        {
            _items.Clear();
        }
    }
}
