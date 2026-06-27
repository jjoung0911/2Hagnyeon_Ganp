using Agents.Modules;
using UnityEngine;

namespace System.UpgradeSystem
{
    // 스탯 모디파이어를 추가하는 패시브 업그레이드
    [CreateAssetMenu(fileName = "New Passive Upgrade", menuName = "SO/Upgrade/PassiveUpgradeSO", order = 0)]
    public class PassiveUpgradeSO : UpgradeDataSO
    {
        [SerializeField] private int statIndex;
        [SerializeField] private float modifyAmount;
        [Tooltip("0이면 무제한 중첩")]
        [SerializeField] private int maxStack;

        public override UpgradeType Type => UpgradeType.Passive;

        public override bool CanApply(ModuleOwner owner, IUpgradeProgress progress)
            => maxStack <= 0 || progress.GetStack(Index) < maxStack;

        public override void Apply(ModuleOwner owner, IUpgradeProgress progress)
        {
            // 중첩마다 고유한 모디파이어 키가 필요하므로 (this, 적용 횟수) 튜플을 키로 사용
            owner.GetModule<IAgentStat>()?.AddModifier(statIndex, (this, progress.GetStack(Index)), modifyAmount * UpgradeAmplifier);
        }

        public override string GetEffectDetail(ModuleOwner owner)
        {
            var stat = owner?.GetModule<IAgentStat>();
            return stat != null && stat.TryGetStat(statIndex, out var statSo)
                ? UpgradeEffectFormat.FormatStatModifier(statSo, modifyAmount * UpgradeAmplifier)
                : null;
        }
    }
}
