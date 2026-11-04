namespace HarryUtils.ObjectPool {
    public interface IPoolable {
        void OnGetFromPool();
        void OnReleaseToPool();
    }
}
