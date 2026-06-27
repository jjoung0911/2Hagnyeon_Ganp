using Agents.Modules;
using Player.Skills.Active;
using UnityEngine;

namespace System.UpgradeSystem
{
    // 최대 레벨(MaxLevel)에 도달한 액티브 스킬을 진화시키는 업그레이드 카드.
    [CreateAssetMenu(fileName = "New Active Skill Evolve Upgrade",
                     menuName = "SO/Upgrade/ActiveSkillEvolveUpgradeSO", order = 0)]
    public class ActiveSkillEvolveUpgradeSO : ActiveSkillLevelUpgradeBaseSO
    {
        public override UpgradeType Type => UpgradeType.ActiveSkillEvolve;

        public override bool CanApply(ModuleOwner owner, IUpgradeProgress progress)
            => GetSkill(owner) is { CanEvolve: true };

        public override void Apply(ModuleOwner owner, IUpgradeProgress progress)
            => GetSkill(owner)?.Evolve();
    }
}
