using System;
using Agents.CombatSystem;
using Agents.Modules;
using UnityEngine;

namespace Player.Skills
{
    // Player 하위 GameObject에 부착하는 IModule.
    // 업그레이드 포인트를 관리하고 스킬에 업그레이드를 적용한다.
    // 포인트 지급: AddPoints(int) 호출 (레벨업 이벤트, 킬 보상 등에서 연결)
    public class SkillUpgradeModule : MonoBehaviour, IModule, IAfterInit
    {
        [SerializeField] private int startingPoints = 0;

        public int UpgradePoints { get; private set; }

        public event Action OnPointsChanged;
        // (skillIndex, newLevel)
        public event Action<int, int> OnSkillUpgraded;

        private ModuleOwner _owner;
        private PlayerSkillModule _skillModule;

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            UpgradePoints = startingPoints;
        }

        public void AfterInit()
        {
            _skillModule = _owner.GetModule<PlayerSkillModule>();
        }

        // 레벨업·보상 시스템에서 호출
        public void AddPoints(int amount)
        {
            if (amount <= 0) return;
            UpgradePoints += amount;
            OnPointsChanged?.Invoke();
        }

        // 해당 스킬을 다음 단계로 업그레이드할 수 있는지 확인
        public bool CanUpgrade(int skillIndex)
        {
            var skill = _skillModule?.GetSkill(skillIndex);
            if (skill == null || skill.SkillData == null) return false;
            int nextLevel = skill.UpgradeLevel + 1;
            if (nextLevel > skill.SkillData.MaxUpgradeLevel) return false;
            return UpgradePoints >= skill.SkillData.GetTier(nextLevel).upgradeCost;
        }

        // 업그레이드 시도. 성공 시 true 반환
        public bool TryUpgrade(int skillIndex)
        {
            if (!CanUpgrade(skillIndex)) return false;

            var skill = _skillModule.GetSkill(skillIndex);
            int nextLevel = skill.UpgradeLevel + 1;
            int cost = skill.SkillData.GetTier(nextLevel).upgradeCost;

            UpgradePoints -= cost;
            skill.ApplyUpgrade(nextLevel);

            OnPointsChanged?.Invoke();
            OnSkillUpgraded?.Invoke(skillIndex, nextLevel);
            return true;
        }

        // UI용 조회 메서드

        public int GetLevel(int skillIndex)
            => _skillModule?.GetSkill(skillIndex)?.UpgradeLevel ?? 0;

        public int GetMaxLevel(int skillIndex)
            => _skillModule?.GetSkill(skillIndex)?.SkillData?.MaxUpgradeLevel ?? 0;

        // 다음 단계 비용. 최대 단계거나 스킬 없으면 -1
        public int GetNextCost(int skillIndex)
        {
            var skill = _skillModule?.GetSkill(skillIndex);
            if (skill == null || skill.SkillData == null) return -1;
            int nextLevel = skill.UpgradeLevel + 1;
            if (nextLevel > skill.SkillData.MaxUpgradeLevel) return -1;
            return skill.SkillData.GetTier(nextLevel).upgradeCost;
        }

        public SkillUpgradeSO GetNextTierData(int skillIndex)
        {
            var skill = _skillModule?.GetSkill(skillIndex);
            if (skill == null || skill.SkillData == null) return null;
            return skill.SkillData.GetTier(skill.UpgradeLevel + 1);
        }
    }
}
