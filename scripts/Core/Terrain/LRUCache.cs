using System.Collections.Generic;

namespace Wild.Core.Terrain
{
    /// <summary>
    /// Implementación genérica de una cache con estrategia LRU (Least Recently Used).
    /// Cuando se alcanza la capacidad, se elimina el elemento que lleva más tiempo sin usarse.
    /// </summary>
    public class LRUCache<TKey, TValue>
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
        private readonly LinkedList<CacheItem> _itemList;
        private readonly object _lock = new();

        public LRUCache(int capacity)
        {
            _capacity = capacity;
            _cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
            _itemList = new LinkedList<CacheItem>();
        }

        public TValue Get(TKey key)
        {
            lock (_lock)
            {
                if (!_cache.TryGetValue(key, out var node))
                {
                    return default;
                }

                // Mover al inicio (más reciente)
                try {
                    _itemList.Remove(node);
                    _itemList.AddFirst(node);
                } catch { }

                return node.Value.Value;
            }
        }

        public void Add(TKey key, TValue value)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var node))
                {
                    // Actualizar valor y mover al inicio
                    _itemList.Remove(node);
                    node.Value.Value = value;
                    _itemList.AddFirst(node);
                }
                else
                {
                    if (_cache.Count >= _capacity)
                    {
                        RemoveOldest();
                    }

                    var newItem = new CacheItem { Key = key, Value = value };
                    var newNode = _itemList.AddFirst(newItem);
                    _cache[key] = newNode;
                }
            }
        }

        public bool Contains(TKey key)
        {
            lock (_lock) return _cache.ContainsKey(key);
        }

        public void Clear()
        {
            lock (_lock)
            {
                _cache.Clear();
                _itemList.Clear();
            }
        }

        private void RemoveOldest()
        {
            var oldest = _itemList.Last;
            if (oldest != null)
            {
                _cache.Remove(oldest.Value.Key);
                _itemList.RemoveLast();
            }
        }

        private class CacheItem
        {
            public TKey Key;
            public TValue Value;
        }
    }
}
