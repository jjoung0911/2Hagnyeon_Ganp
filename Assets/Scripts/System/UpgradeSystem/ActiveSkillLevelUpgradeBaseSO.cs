using Agents.Modules;
using Player.Skills.Active;
using UnityEngine;

namespace System.UpgradeSystem
{
    // 액티브 스킬(해금/레벨업) 업그레이드 카드의 공통 로직.
    // 대상 스킬 인덱스/목표 레벨을 인스펙터에서 지정하고, ActiveSkillModule에서 스킬을 조회한다.
    public abstract class ActiveSkillLevelUpgradeBaseSO : UpgradeDataSO
    {
        [SerializeField] protected ActiveSkillEnum skillIndex;
        [SerializeField, Range(1, ActiveSkillDataSO.MaxLevel)] protected int targetLevel = 1;

        private IActiveSkillModule _skillModule;

        public override bool IsSameTarget(UpgradeDataSO other)
            => other is ActiveSkillLevelUpgradeBaseSO o && skillIndex == o.skillIndex;

        protected AbstractActiveSkill GetSkill(ModuleOwner owner)
        {
            _skillModule ??= owner.GetModule<IActiveSkillModule>();
            return _skillModule?.GetSkill(skillIndex);
        }
    }
}
