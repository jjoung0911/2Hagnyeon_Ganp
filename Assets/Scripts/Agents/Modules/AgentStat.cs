using System.Collections.Generic;
using System.Linq;
using System.StatSystem;
using UnityEngine;

namespace Agents.Modules
{
    public class AgentStat : MonoBehaviour, IModule, IAgentStat
    {
        [SerializeField] private StatOverride[] statOverrides;
        [SerializeField] private StatSO test;

        private Dictionary<int, StatSO> _statDict = new();
        
        private Agent _owner;
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner as Agent;
            _statDict = statOverrides.ToDictionary(data => data.StatData.Index, stat => stat.CreateStat());
        }

        [ContextMenu("TEST")]
        public void Test()
        {
            AddModifier(test.Index, this, 10);
        }
        
        public StatSO GetStat(int assetIndex) => _statDict.GetValueOrDefault(assetIndex);
        
        public bool TryGetStat(int index, out StatSO outStat) => _statDict.TryGetValue(index, out outStat);

        public void AddModifier(int assetIndex, object key, float val)
        {
            if (_statDict.TryGetValue(assetIndex, out var so))
            {
                so.AddModifier(key, val);
                return;
            }
        }
        public void RemoveModifier(int assetIndex, object key)
        {
            if (_statDict.TryGetValue(assetIndex, out var so))
            {
                so.RemoveModifier(key);
                return;
            }
        }

        public float SubscribeStat(int assetIndex, StatSO.ValueChangeHandler handler, float defaultVal)
        {
            if (_statDict.TryGetValue(assetIndex, out var so))
            {
                so.OnValueChanged += handler;
                return so.Value;
            }

            return defaultVal;
        }
        public void UnSubscribeStat(int assetIndex, StatSO.ValueChangeHandler handler)
        {
            if (_statDict.TryGetValue(assetIndex, out var so))
            {
                so.OnValueChanged -= handler;
            }

        }
    }
}