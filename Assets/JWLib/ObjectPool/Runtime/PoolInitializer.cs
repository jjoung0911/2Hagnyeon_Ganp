using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JWLib.ObjectPool.Runtime
{
    public class PoolInitializer : MonoBehaviour
    {
        [field: SerializeField] public PoolManagerSO PoolManagerAsset { get; private set; }

        private bool _isOwner;

        private void Awake()
        {
            PoolInitializer[] initializers = FindObjectsByType<PoolInitializer>(FindObjectsSortMode.None);
            if (initializers.Length > 1)
                return;

            _isOwner = true;
            PoolManagerAsset.InitializePool(transform);
            DontDestroyOnLoad(gameObject);

            // 풀 루트가 DontDestroyOnLoad라 씬 재로딩 시 사용 중이던 오브젝트가 남는다.
            // 씬이 언로드되는 시점에 모든 사용 중 오브젝트를 풀로 회수한다.
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        private void OnDestroy()
        {
            if (_isOwner)
                SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            PoolManagerAsset.ReleaseAll();
        }
    }
}