using UnityEngine;

namespace System.StatSystem
{
    [Serializable]
    public class StatOverride
    {
        [field: SerializeField] public StatSO StatData { get; private set; }
        [SerializeField] private bool isUseOverride;
        [SerializeField] private float overrideValue;

        public StatOverride(StatSO stat) => StatData = stat;

        public StatSO CreateStat()
        {
            StatSO stat = StatData.Clone() as StatSO;

            if (isUseOverride)
                stat.BaseValue = overrideValue;
            return stat;
        }
    }
}