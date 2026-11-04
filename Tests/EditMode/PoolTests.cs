using System.Text.RegularExpressions;
using HarryUtils.ObjectPool;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;

namespace HarryUtils.Tests {
    public class PoolTests {
        private class TestPoolable : MonoBehaviour, IPoolable {
            public int gets;
            public int releases;
            public void OnGetFromPool() => gets++;
            public void OnReleaseToPool() => releases++;
        }

        // ==================== VARIABLES ===================
        #region Private Variables
        private TestPoolable prefab;
        private Pool<TestPoolable> pool;
        #endregion

        // ==================== SETUP ====================
        [SetUp]
        public void SetUp() {
            var _go = new GameObject("PoolTestPrefab");
            _go.SetActive(false);
            prefab = _go.AddComponent<TestPoolable>();
        }

        [TearDown]
        public void TearDown() {
            pool?.Dispose();
            pool = null;
            if (prefab != null) Object.DestroyImmediate(prefab.gameObject);
        }

        // ==================== TESTS ====================
        [Test]
        public void Prewarm_CreatesInactiveInstances() {
            pool = new Pool<TestPoolable>(prefab, 3);
            Assert.AreEqual(3, pool.CountInactive);
            Assert.AreEqual(0, pool.CountActive);
        }

        // GC.GetAllocatedBytesForCurrentThread returns 0 on Unity Mono, so the profiler recorder is used instead
        private static int CountGcAllocBlocks(System.Action _action) {
            Recorder _recorder = Recorder.Get("GC.Alloc");
            _recorder.FilterToCurrentThread();
            _recorder.enabled = true;
            _action();
            _recorder.enabled = false;
            return _recorder.sampleBlockCount;
        }

        [Test]
        public void AllocationProbe_DetectsAllocation() {
            object _sink = null;
            int _blocks = CountGcAllocBlocks(() => {
                for (int i = 0; i < 100; i++) _sink = new object();
            });

            Assert.Greater(_blocks, 0);
            Assert.IsNotNull(_sink);
        }

        [Test]
        public void Get_SteadyState_DoesNotAllocate() {
            pool = new Pool<TestPoolable>(prefab, 4);
            for (int i = 0; i < 10; i++) pool.Release(pool.Get());

            int _blocks = CountGcAllocBlocks(() => {
                for (int i = 0; i < 100; i++) pool.Release(pool.Get());
            });

            Assert.AreEqual(0, _blocks);
        }

        [Test]
        public void Release_CallbackThrows_InstanceStillReusable() {
            pool = new Pool<TestPoolable>(prefab, 1);
            bool _throw = true;
            pool.Released += _ => {
                if (!_throw) return;
                _throw = false;
                throw new System.InvalidOperationException("boom");
            };

            var _item = pool.Get();
            Assert.Throws<System.InvalidOperationException>(() => pool.Release(_item));

            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(1, pool.CountInactive);
            Assert.AreEqual(1, pool.CountAll);
            Assert.IsNotNull(pool.Get());
            Assert.AreEqual(1, pool.CountAll);
        }

        [Test]
        public void Get_ActivatesAndCallsCallbacks() {
            pool = new Pool<TestPoolable>(prefab);
            int _got = 0;
            pool.Got += _ => _got++;

            var _item = pool.Get();

            Assert.AreEqual(1, _got);
            Assert.AreEqual(1, _item.gets);
            Assert.IsTrue(_item.gameObject.activeSelf);
        }

        [Test]
        public void Release_DeactivatesAndCallsCallbacks() {
            pool = new Pool<TestPoolable>(prefab);
            int _released = 0;
            pool.Released += _ => _released++;
            var _item = pool.Get();

            Assert.IsTrue(pool.Release(_item));

            Assert.AreEqual(1, _released);
            Assert.AreEqual(1, _item.releases);
            Assert.IsFalse(_item.gameObject.activeSelf);
            Assert.AreEqual(1, pool.CountInactive);
        }

        [Test]
        public void Release_Twice_ReturnsFalse() {
            pool = new Pool<TestPoolable>(prefab);
            var _item = pool.Get();
            pool.Release(_item);

            LogAssert.Expect(LogType.Warning, new Regex("already released"));
            Assert.IsFalse(pool.Release(_item));
            Assert.AreEqual(1, pool.CountInactive);
            Assert.AreEqual(0, pool.CountActive);
        }

        [Test]
        public void Release_Foreign_ReturnsFalse() {
            pool = new Pool<TestPoolable>(prefab);
            var _foreign = Object.Instantiate(prefab);

            LogAssert.Expect(LogType.Warning, new Regex("does not belong"));
            Assert.IsFalse(pool.Release(_foreign));
            Assert.AreEqual(0, pool.CountAll);

            Object.DestroyImmediate(_foreign.gameObject);
        }

        [Test]
        public void Overflow_Grow_IgnoresMax() {
            pool = new Pool<TestPoolable>(prefab, 0, 2, PoolOverflow.Grow);
            pool.Get();
            pool.Get();
            pool.Get();
            Assert.AreEqual(3, pool.CountAll);
        }

        [Test]
        public void Overflow_ReuseOldest_ReturnsFirstActive() {
            pool = new Pool<TestPoolable>(prefab, 0, 2, PoolOverflow.ReuseOldest);
            int _released = 0;
            pool.Released += _ => _released++;

            var _a = pool.Get();
            var _b = pool.Get();
            var _c = pool.Get();

            Assert.AreSame(_a, _c);
            Assert.AreEqual(1, _released);
            Assert.AreEqual(2, pool.Active.Count);
            CollectionAssert.AreEqual(new[] { _b, _a }, pool.Active);
            Assert.AreSame(_b, pool.Oldest);
        }

        [Test]
        public void Overflow_Fail_ReturnsNull() {
            pool = new Pool<TestPoolable>(prefab, 0, 1, PoolOverflow.Fail);
            Assert.IsTrue(pool.TryGet(out var _first));
            Assert.IsNotNull(_first);

            Assert.IsFalse(pool.TryGet(out var _second));
            Assert.IsTrue(_second == null);
            Assert.IsTrue(pool.Get() == null);
        }

        [Test]
        public void Fail_ReleaseThenGet_Succeeds() {
            pool = new Pool<TestPoolable>(prefab, 0, 1, PoolOverflow.Fail);
            Assert.IsTrue(pool.TryGet(out var _first));
            Assert.IsFalse(pool.TryGet(out _));

            Assert.IsTrue(pool.Release(_first));

            Assert.IsTrue(pool.TryGet(out var _again));
            Assert.AreSame(_first, _again);
        }

        [Test]
        public void Release_DestroyedActive_FreesCapacity() {
            pool = new Pool<TestPoolable>(prefab, 0, 1, PoolOverflow.Fail);
            var _item = pool.Get();
            Object.DestroyImmediate(_item.gameObject);

            Assert.IsFalse(pool.Release(_item));

            Assert.AreEqual(0, pool.CountActive);
            Assert.IsTrue(pool.TryGet(out var _next));
            Assert.IsTrue(_next != null);
            Assert.AreEqual(1, pool.CountAll);
        }

        [Test]
        public void Get_FullFailPool_WithDestroyedActive_Recovers() {
            pool = new Pool<TestPoolable>(prefab, 0, 1, PoolOverflow.Fail);
            var _item = pool.Get();
            Object.DestroyImmediate(_item.gameObject);

            Assert.IsTrue(pool.TryGet(out var _next));
            Assert.IsTrue(_next != null);
            Assert.AreEqual(1, pool.CountAll);
        }

        [Test]
        public void Get_SkipsDestroyedInstances() {
            pool = new Pool<TestPoolable>(prefab, 1);
            var _instance = pool.Get();
            pool.Release(_instance);
            Object.DestroyImmediate(_instance.gameObject);

            var _item = pool.Get();

            Assert.IsTrue(_item != null);
            Assert.AreNotSame(_instance, _item);
            Assert.AreEqual(1, pool.CountAll);
        }

        [Test]
        public void Parent_IsApplied() {
            var _parent = new GameObject("PoolParent").transform;
            pool = new Pool<TestPoolable>(prefab, 1, 0, PoolOverflow.Grow, _parent);

            var _item = pool.Get();

            Assert.AreSame(_parent, _item.transform.parent);
            pool.Dispose();
            pool = null;
            Object.DestroyImmediate(_parent.gameObject);
        }

        [Test]
        public void Dispose_DestroysAll() {
            pool = new Pool<TestPoolable>(prefab, 2);
            var _active = pool.Get();
            var _inactive = pool.Get();
            pool.Release(_inactive);

            pool.Dispose();

            Assert.IsTrue(_active == null);
            Assert.IsTrue(_inactive == null);
            Assert.AreEqual(0, pool.CountAll);
        }
    }
}
