using System.Linq;
using Agents.Modules;
using UnityEngine;

namespace System.UpgradeSystem
{
    // 여러 스탯에 동시에 모디파이어를 추가하는 패시브 업그레이드 카드
    // (예: 검술 숙련 - 공격 속도 + 공격 범위 동시 증가)
    [CreateAssetMenu(fileName = "New Multi Stat Passive Upgrade", menuName = "SO/Upgrade/MultiStatPassiveUpgradeSO", order = 0)]
    public class MultiStatPassiveUpgradeSO : UpgradeDataSO
    {
        [Serializable]
        public struct StatModifier
        {
            public int statIndex;
            public float modifyAmount;
        }

        [SerializeField] private StatModifier[] modifiers;
        [Tooltip("0이면 무제한 중첩")]
        [SerializeField] private int maxStack;

        public override UpgradeType Type => UpgradeType.Passive;

        public override bool CanApply(ModuleOwner owner, IUpgradeProgress progress)
            => maxStack <= 0 || progress.GetStack(Index) < maxStack;

        public override void Apply(ModuleOwner owner, IUpgradeProgress progress)
        {
            var stat = owner.GetModule<IAgentStat>();
            if (stat == null) return;

            int stack = progress.GetStack(Index);
            float amplifier = UpgradeAmplifier;
            foreach (var modifier in modifiers)
                stat.AddModifier(modifier.statIndex, (this, modifier.statIndex, stack), modifier.modifyAmount * amplifier);
        }

        public override string GetEffectDetail(ModuleOwner owner)
        {
            var stat = owner?.GetModule<IAgentStat>();
            if (stat == null) return null;

            float amplifier = UpgradeAmplifier;
            var lines = modifiers
                .Select(modifier => stat.TryGetStat(modifier.statIndex, out var statSo)
                    ? UpgradeEffectFormat.FormatStatModifier(statSo, modifier.modifyAmount * amplifier)
                    : null)
                .Where(line => line != null);

            string result = string.Join("\n", lines);
            return string.IsNullOrEmpty(result) ? null : result;
        }
    }
}
