using Agents.Modules;
using Player.Skills;
using UnityEngine;

namespace System.UpgradeSystem
{
    // 잠겨 있는 액티브 스킬을 해금하는 업그레이드.
    [CreateAssetMenu(fileName = "New Active Skill Unlock Upgrade", menuName = "SO/Upgrade/ActiveSkillUnlockUpgradeSO", order = 0)]
    public class ActiveSkillUnlockUpgradeSO : ActiveSkillLevelUpgradeBaseSO
    {
        public override UpgradeType Type => UpgradeType.ActiveSkillUnlock;

        public override bool CanApply(ModuleOwner owner, IUpgradeProgress progress)
            => GetSkill(owner) is ILockableSkill { IsUnlocked: false };

        public override void Apply(ModuleOwner owner, IUpgradeProgress progress)
        {
            if (GetSkill(owner) is ILockableSkill lockable)
                lockable.Unlock();
        }
    }
}
