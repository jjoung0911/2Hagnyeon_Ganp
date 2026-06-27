using Agents.Modules;
using Player.Skills.Passive;
using UnityEngine;

namespace System.UpgradeSystem
{
    // 패시브 스킬(BerserkerSoul, BloodFeast 등)의 레벨을 targetLevel까지 끌어올리는 업그레이드.
    [CreateAssetMenu(fileName = "New Passive Skill Level Upgrade", menuName = "SO/Upgrade/PassiveSkillLevelUpgradeSO", order = 0)]
    public class PassiveSkillLevelUpgradeSO : UpgradeDataSO
    {
        [SerializeField] private PassiveSkillEnum skillIndex;
        [SerializeField, Range(1, AbstractPassiveSkill.MaxLevel)] private int targetLevel = 1;

        public override UpgradeType Type => UpgradeType.Passive;

        public override bool IsSameTarget(UpgradeDataSO other)
            => other is PassiveSkillLevelUpgradeSO o && skillIndex == o.skillIndex;

        private IPassiveSkillModule _skillModule;
        // 직전 레벨(targetLevel - 1)에서만 후보로 노출 — 업그레이드 단계가 순서대로만 등장하도록 보장
        // IsUnlocked(Level>=1)는 체크하지 않음 — 패시브 스킬은 Level 0(미보유)에서 처음 획득 가능해야 하기 때문
        public override bool CanApply(ModuleOwner owner, IUpgradeProgress progress)
        {
            var skill = GetSkill(owner);
            return skill != null && skill.Level == targetLevel - 1;
        }

        public override void Apply(ModuleOwner owner, IUpgradeProgress progress)
        {
            var skill = GetSkill(owner);
            if (skill == null) return;
            skill.ApplyUpgrade(targetLevel);
        }

        public override string GetEffectDetail(ModuleOwner owner)
            => GetSkill(owner)?.GetEffectDetail(targetLevel);

        private AbstractPassiveSkill GetSkill(ModuleOwner owner)
        {
            _skillModule ??= owner?.GetModule<IPassiveSkillModule>();
            return _skillModule?.GetSkill(skillIndex);
        }
    }
}
