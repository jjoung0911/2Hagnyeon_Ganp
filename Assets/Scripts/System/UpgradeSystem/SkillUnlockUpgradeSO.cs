using Agents.CombatSystem;
using Agents.Modules;
using Player.Skills;
using UnityEngine;

namespace System.UpgradeSystem
{
    // 잠겨 있는 스킬을 해금하는 업그레이드. 대상 스킬은 ILockableSkill을 구현해야 한다.
    [CreateAssetMenu(fileName = "New Skill Unlock Upgrade", menuName = "SO/Upgrade/SkillUnlockUpgradeSO", order = 0)]
    public class SkillUnlockUpgradeSO : UpgradeDataSO
    {
        // SkillEnum으로 인스펙터에서 스킬 이름을 직접 선택 (raw int 인덱스 입력 방지)
        [SerializeField] private SkillEnum skillIndex;

        public override UpgradeType Type => UpgradeType.SkillUnlock;

        // 같은 스킬의 레벨업 카드(SkillLevelUpgradeSO)와 대상을 공유 — 해금 후 레벨업 시 아이콘이 분리되지 않도록 함
        public override bool IsSameTarget(UpgradeDataSO other) => other switch
        {
            SkillUnlockUpgradeSO o => skillIndex == o.skillIndex,
            SkillLevelUpgradeSO o => skillIndex == o.SkillIndex,
            _ => false
        };

        public override bool CanApply(ModuleOwner owner, IUpgradeProgress progress)
            => owner.GetModule<PlayerSkillModule>()?.GetSkill((int)skillIndex) is ILockableSkill { IsUnlocked: false };

        public override void Apply(ModuleOwner owner, IUpgradeProgress progress)
        {
            if (owner.GetModule<PlayerSkillModule>()?.GetSkill((int)skillIndex) is ILockableSkill lockable)
                lockable.Unlock();
        }
    }
}
