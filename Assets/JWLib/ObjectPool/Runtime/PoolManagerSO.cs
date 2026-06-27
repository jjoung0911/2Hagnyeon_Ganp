using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

namespace JWLib.ObjectPool.Runtime
{
    [CreateAssetMenu(fileName = "PoolManager", menuName = "Lib/Pool/PoolManager", order = 0)]
    public class PoolManagerSO : ScriptableObject
    {
        public List<PoolItemSO> itemList = new();
        private Dictionary<PoolItemSO, Pool> _pools;
        private Transform _rootTrm;

        public void InitializePool(Transform rootTrm)
        {
            _rootTrm = rootTrm;
            _pools = new Dictionary<PoolItemSO, Pool>();
            
            foreach (PoolItemSO item in itemList)
            {
                // 비어있는 항목이나 prefab이 없는 항목은 건너뛴다 (한 항목 누락이 풀 전체 초기화를 막지 않도록 방어)
                if (item == null || item.prefab == null)
                {
                    Debug.LogWarning("PoolManagerSO: itemList에 비어있는 항목 또는 prefab이 없는 항목이 있어 건너뜁니다.");
                    continue;
                }

                IPoolable poolable = item.prefab.GetComponent<IPoolable>();
                Debug.Assert(poolable != null, $"PoolItem은 반드시 IPoolable을 가져야합니다. {item.prefab.gameObject}");

                Pool pool = new Pool(poolable,_rootTrm,  item.initCount);
                _pools.Add(item, pool);
            }
        }

        public T Pop<T>(PoolItemSO item) where T : IPoolable
        {
            Debug.Assert(_rootTrm != null, "풀 매니저는 초기화 후 사용해야합니다");

            if (_pools.TryGetValue(item, out Pool pool))
            {
                return (T)pool.Pop();
            }

            return default;
        }

        public void Push(IPoolable item)
        {
            Debug.Assert(_rootTrm != null, "풀 매니저는 초기화 후 사용해야 합니다");
            if (_pools.TryGetValue(item.Item, out Pool pool))
            {
                pool.Push(item);
            }
        }

        // 모든 풀의 사용 중 오브젝트를 일괄 회수한다 (씬 재로딩 시 잔존 방지)
        public void ReleaseAll()
        {
            if (_pools == null) return;
            foreach (Pool pool in _pools.Values)
                pool.ReleaseAll();
        }
    }
}