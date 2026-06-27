using UnityEngine;

namespace System.UpgradeSystem
{
    // 일반 스킬의 업그레이드 티어를 한 단계 적용
    [CreateAssetMenu(fileName = "New Skill Upgrade Upgrade", menuName = "SO/Upgrade/SkillUpgradeUpgradeSO", order = 0)]
    public class SkillUpgradeUpgradeSO : SkillLevelUpgradeSO
    {
        public override UpgradeType Type => UpgradeType.SkillUpgrade;
    }
}
