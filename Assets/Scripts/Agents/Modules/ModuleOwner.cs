using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Agents.Modules
{
    public abstract class ModuleOwner : MonoBehaviour
    {
        private Dictionary<Type, IModule> _compos = new();

        protected virtual void Awake()
        {
            _compos = GetComponentsInChildren<IModule>().ToDictionary(compo => compo.GetType());

            InitializeModules();
            AfterInit();
        }
        protected virtual void InitializeModules()
        {
            foreach (var compo in _compos.Values)
            {
                compo.Initialize(this);
            }
        }
        protected virtual void AfterInit()
        {
            var afterInit = _compos.Values.OfType<IAfterInit>();
            foreach (var init in afterInit)
            {
                init.AfterInit();
            }
        }

        public T GetModule<T>()
        {
            if (_compos.TryGetValue(typeof(T), out var value))
                return (T)value;
            IModule findModule = _compos.Values.FirstOrDefault(module => module is T);
            if (findModule != null && findModule is T castedModule)
            {
                return castedModule;
            }

            return default;
        }
    }
}
