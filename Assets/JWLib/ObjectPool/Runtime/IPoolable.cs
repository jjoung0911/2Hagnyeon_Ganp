using UnityEngine;

namespace JWLib.ObjectPool.Runtime
{
    public interface IPoolable
    {
        PoolItemSO Item { get; set; }
        GameObject GameObject { get; }
        void ResetItem();
    }
}