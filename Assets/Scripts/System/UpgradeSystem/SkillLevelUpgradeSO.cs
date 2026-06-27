using Agents.CombatSystem;
using Agents.Modules;
using Player.Skills;
using UnityEngine;

namespace System.UpgradeSystem
{
    // 특정 스킬의 SkillUpgradeSO 티어를 한 단계 적용하는 공통 로직.
    // SkillUpgrade / UltimateUpgrade 타입은 적용 대상이 다를 뿐 동작이 동일하므로
    // 이 클래스를 상속해 Type만 다르게 오버라이드한다 (OCP).
    public abstract class SkillLevelUpgradeSO : UpgradeDataSO
    {
        // SkillEnum으로 인스펙터에서 스킬 이름을 직접 선택 (raw int 인덱스 입력 방지)
        [SerializeField] protected SkillEnum skillIndex;
        // 이 카드가 가리키는 SkillData.upgradeTiers의 단계 (1 = 1티어)
        [SerializeField, Min(1)] protected int targetLevel = 1;

        public SkillEnum SkillIndex => skillIndex;

        // 직전 단계(targetLevel - 1)에서만 후보로 노출 — 업그레이드 단계가 순서대로만 등장하도록 보장
        public override bool CanApply(ModuleOwner owner, IUpgradeProgress progress)
        {
            var skill = owner.GetModule<PlayerSkillModule>()?.GetSkill((int)skillIndex);
            return skill != null && skill.IsUnlocked && skill.UpgradeLevel == targetLevel - 1 && targetLevel <= skill.SkillData.MaxUpgradeLevel;
        }

        public override bool IsSameTarget(UpgradeDataSO other)
            => other is SkillLevelUpgradeSO o && skillIndex == o.skillIndex;

        public override void Apply(ModuleOwner owner, IUpgradeProgress progress)
        {
            var skill = owner.GetModule<PlayerSkillModule>()?.GetSkill((int)skillIndex);
            if (skill == null) return;
            skill.ApplyUpgrade(targetLevel);
        }
    }
}
