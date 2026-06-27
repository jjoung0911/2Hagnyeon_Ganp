using UnityEngine;

namespace Agents.CombatSystem
{
    [CreateAssetMenu(fileName = "new Skill Data", menuName = "SO/Skill/SkillDataSO", order = 0)]
    public class SkillDataSO : ScriptableObject
    {
        public string skillName;
        public float cooldown;
        public int skillIndex;
        [Tooltip("HUD 슬롯 표시 순서. skillIndex는 인풋 바인딩(Scale factor)과 묶여 있어 그대로 정렬 기준으로 쓸 수 없다 (예: R=5 < Q=9 < E=10)")]
        public int hudOrder;
        public float damage;
        public Sprite icon;

        [Header("업그레이드 티어 — 인덱스 0 = 1단계")]
        public SkillUpgradeSO[] upgradeTiers;

        public int MaxUpgradeLevel => upgradeTiers?.Length ?? 0;

        public SkillUpgradeSO GetTier(int level)
            => level > 0 && upgradeTiers != null && level <= upgradeTiers.Length
                ? upgradeTiers[level - 1]
                : null;
    }
}