using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HarryUtils.ObjectPool {
    public sealed class Pool<T> : IDisposable where T : Component {
        private sealed class Entry {
            public bool isActive;
            public IPoolable[] poolables;
            public LinkedListNode<T> node;
        }

        // ==================== VARIABLES ===================
        #region Private Variables
        private readonly T prefab;
        private readonly int maxSize;
        private readonly PoolOverflow overflow;
        private readonly Transform parent;

        private readonly Stack<T> free = new();
        private readonly LinkedList<T> active = new();
        private readonly Dictionary<T, Entry> entries = new();
        #endregion

        #region Public Variables
        public event Action<T> Created;
        public event Action<T> Got;
        public event Action<T> Released;
        public event Action<T> Destroyed;

        public int CountActive => active.Count;
        public int CountInactive => free.Count;
        public int CountAll => entries.Count;
        // Oldest to newest.
        public IReadOnlyCollection<T> Active => active;
        public T Oldest => PeekOldestActive();
        #endregion

        // ==================== START ====================
        public Pool(T _prefab, int _prewarm = 0, int _maxSize = 0, PoolOverflow _overflow = PoolOverflow.Grow, Transform _parent = null) {
            prefab = _prefab;
            maxSize = _maxSize;
            overflow = _overflow;
            parent = _parent;

            if (_prewarm > 0) Prewarm(_prewarm);
        }

        // ==================== METHODS ====================
        public bool TryGet(out T _item) {
            _item = Get();
            return _item != null;
        }

        public T Get() {
            T _item = PopFree();

            if (_item == null) {
                if (IsFull() && overflow == PoolOverflow.Fail) PurgeDestroyedActive();

                if (IsFull()) {
                    if (overflow == PoolOverflow.Fail) return null;

                    T _oldest = PeekOldestActive();
                    if (_oldest != null) {
                        Release(_oldest);
                        _item = PopFree();
                    }
                }

                if (_item == null) _item = CreateInstance();
            }

            Entry _entry = entries[_item];
            _entry.isActive = true;
            active.AddLast(_entry.node);

            _item.gameObject.SetActive(true);
            Got?.Invoke(_item);
            for (int i = 0; i < _entry.poolables.Length; i++) _entry.poolables[i].OnGetFromPool();

            return _item;
        }

        public bool Release(T _item) {
            if (_item == null) {
                if (!ReferenceEquals(_item, null) && entries.TryGetValue(_item, out Entry _dead)) {
                    DropEntry(_item, _dead);
                    return false;
                }
                Debug.LogWarning("[Pool] Cannot release a null or foreign destroyed object.");
                return false;
            }

            if (!entries.TryGetValue(_item, out Entry _entry)) {
                Debug.LogWarning($"[Pool] '{_item.name}' does not belong to this pool.");
                return false;
            }

            if (!_entry.isActive) {
                Debug.LogWarning($"[Pool] '{_item.name}' was already released.");
                return false;
            }

            // Bookkeeping first: a throwing callback must not leave the instance lost
            _entry.isActive = false;
            active.Remove(_entry.node);
            free.Push(_item);

            for (int i = 0; i < _entry.poolables.Length; i++) _entry.poolables[i].OnReleaseToPool();
            Released?.Invoke(_item);
            _item.gameObject.SetActive(false);
            return true;
        }

        public void Prewarm(int _count) {
            if (maxSize > 0 && overflow != PoolOverflow.Grow) {
                _count = Mathf.Min(_count, maxSize - CountAll);
            }

            for (int i = 0; i < _count; i++) free.Push(CreateInstance());
        }

        public void ReleaseAll() {
            while (active.Count > 0) {
                T _item = active.Last.Value;
                if (_item == null) {
                    DropEntry(_item, entries[_item]);
                    continue;
                }
                Release(_item);
            }
        }

        public void Clear() {
            while (free.Count > 0) {
                T _item = free.Pop();
                entries.Remove(_item);
                DestroyInstance(_item);
            }
        }

        public void Dispose() {
            Clear();
            foreach (T _item in active) DestroyInstance(_item);
            active.Clear();
            entries.Clear();
        }

        // =================================================

        private bool IsFull() {
            return maxSize > 0 && overflow != PoolOverflow.Grow && CountAll >= maxSize;
        }

        // Pops free instances until a live one is found; destroyed ones are dropped.
        private T PopFree() {
            while (free.Count > 0) {
                T _item = free.Pop();
                if (_item != null) return _item;
                entries.Remove(_item);
            }
            return null;
        }

        // Drops destroyed instances at the front so the oldest live one is returned.
        private T PeekOldestActive() {
            while (active.Count > 0) {
                T _item = active.First.Value;
                if (_item != null) return _item;
                DropEntry(_item, entries[_item]);
            }
            return null;
        }

        private void PurgeDestroyedActive() {
            LinkedListNode<T> _node = active.First;
            while (_node != null) {
                LinkedListNode<T> _next = _node.Next;
                if (_node.Value == null) DropEntry(_node.Value, entries[_node.Value]);
                _node = _next;
            }
        }

        private void DropEntry(T _item, Entry _entry) {
            if (_entry.isActive) {
                active.Remove(_entry.node);
                _entry.isActive = false;
            }
            entries.Remove(_item);
        }

        private T CreateInstance() {
            T _item = Object.Instantiate(prefab, parent);
            entries[_item] = new Entry { poolables = _item.GetComponents<IPoolable>(), node = new LinkedListNode<T>(_item) };
            Created?.Invoke(_item);
            _item.gameObject.SetActive(false);
            return _item;
        }

        private void DestroyInstance(T _item) {
            if (_item == null) return;
            Destroyed?.Invoke(_item);
            if (Application.isPlaying) Object.Destroy(_item.gameObject);
            else Object.DestroyImmediate(_item.gameObject);
        }
    }
}
