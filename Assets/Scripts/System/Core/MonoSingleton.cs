using UnityEngine;

namespace System.Core
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = FindFirstObjectByType<T>();
                    if (!_instance)
                    {
                        GameObject obj = new GameObject();
                        obj.name = typeof(T).Name;
                        _instance = obj.AddComponent<T>();
                    }
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            T[] instances = FindObjectsByType<T>(FindObjectsSortMode.None);
            if(instances.Length > 1)
                Destroy(gameObject);
        }
        
    }
}