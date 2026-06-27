using Agents.Modules;
using UnityEngine;

namespace System.UpgradeSystem
{
    // 이미 해금된 액티브 스킬의 레벨을 targetLevel까지 끌어올리는 업그레이드.
    [CreateAssetMenu(fileName = "New Active Skill Level Upgrade", menuName = "SO/Upgrade/ActiveSkillLevelUpgradeSO", order = 0)]
    public class ActiveSkillLevelUpgradeSO : ActiveSkillLevelUpgradeBaseSO
    {
        public override UpgradeType Type => UpgradeType.ActiveSkillUpgrade;

        // 직전 레벨(targetLevel - 1)에서만 후보로 노출 — 업그레이드 단계가 순서대로만 등장하도록 보장
        public override bool CanApply(ModuleOwner owner, IUpgradeProgress progress)
        {
            var skill = GetSkill(owner);
            return skill != null && skill.IsUnlocked && skill.Level == targetLevel - 1;
        }

        public override void Apply(ModuleOwner owner, IUpgradeProgress progress)
        {
            var skill = GetSkill(owner);
            if (skill == null) return;
            skill.ApplyUpgrade(Mathf.Max(skill.Level, targetLevel));
        }
    }
}
