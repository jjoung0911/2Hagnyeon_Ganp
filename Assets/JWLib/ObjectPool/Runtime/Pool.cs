using System.Collections.Generic;
using UnityEngine;

namespace JWLib.ObjectPool.Runtime
{
    public class Pool
    {
        private readonly Stack<IPoolable> _pool;
        private readonly HashSet<IPoolable> _active = new(); // 꺼내져 사용 중인 오브젝트 추적 (씬 재로딩 시 회수용)
        private readonly Transform _parentTrm;
        private readonly GameObject _prefab;

        public Pool(IPoolable poolable, Transform parent, int initCount)
        {
            _pool = new Stack<IPoolable>(initCount);
            _parentTrm = parent;
            _prefab = poolable.GameObject;
            for (int i = 0; i < initCount; ++i)
            {
                GameObject go = Object.Instantiate(_prefab, _parentTrm);
                go.SetActive(false);
                IPoolable item = go.GetComponent<IPoolable>();
                Debug.Assert(item != null, $"Poolable item은 반드시 IPoolable을 구현해야함 {_prefab}");
                _pool.Push(item);
            }
        }
        public IPoolable Pop()
        {
            IPoolable item;
            if (_pool.Count > 0)
            {
                item = _pool.Pop();
                item.GameObject.SetActive(true);
            }
            else
            {
                GameObject go = Object.Instantiate(_prefab, _parentTrm);
                item = go.GetComponent<IPoolable>();
            }
            _active.Add(item);
            item.ResetItem();
            return item;
        }

        public void Push(IPoolable item)
        {
            _active.Remove(item);
            item.GameObject.SetActive(false);
            _pool.Push(item);
        }

        // 사용 중인 모든 오브젝트를 풀로 되돌린다 (풀 루트가 DontDestroyOnLoad라 씬 재로딩 시 잔존하는 것을 방지)
        public void ReleaseAll()
        {
            foreach (IPoolable item in _active)
            {
                if (item?.GameObject == null) continue; // 이미 파괴된 오브젝트는 건너뜀
                item.GameObject.SetActive(false);
                _pool.Push(item);
            }
            _active.Clear();
        }
    }
}