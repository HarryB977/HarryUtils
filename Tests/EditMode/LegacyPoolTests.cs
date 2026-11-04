using HarryUtils.ObjectPool;
using NUnit.Framework;
using UnityEngine;

namespace HarryUtils.Tests {
    public class LegacyPoolTests {
        private class TestComponent : MonoBehaviour { }

        // ==================== VARIABLES ===================
        #region Private Variables
        private TestComponent prefab;
        #endregion

        // ==================== SETUP ====================
        [SetUp]
        public void SetUp() {
            var _go = new GameObject("LegacyPoolTestPrefab");
            _go.SetActive(false);
            prefab = _go.AddComponent<TestComponent>();
        }

        [TearDown]
        public void TearDown() {
            foreach (var _obj in Object.FindObjectsByType<TestComponent>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                Object.DestroyImmediate(_obj.gameObject);
            }
        }

        // ==================== TESTS ====================
        [Test]
        public void LegacyPool_ExternallyDeactivated_IsReused() {
            var _pool = new ObjectPool<TestComponent>(prefab, 1);

            var _first = _pool.GetObject();
            _first.gameObject.SetActive(false);
            var _second = _pool.GetObject();

            Assert.AreSame(_first, _second);
        }

        [Test]
        public void LegacyLimited_AtMax_ReturnsOldest() {
            var _pool = new LimitedObjectPool<TestComponent>(prefab, 0, 2);

            var _a = _pool.GetObject();
            _pool.GetObject();
            var _c = _pool.GetObject();

            Assert.AreSame(_a, _c);
        }
    }
}
