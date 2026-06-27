using UnityEngine;

namespace System.UpgradeSystem
{
    // 궁극기(얼티밋) 스킬의 업그레이드 티어를 한 단계 적용
    [CreateAssetMenu(fileName = "New Ultimate Upgrade", menuName = "SO/Upgrade/UltimateUpgradeUpgradeSO", order = 0)]
    public class UltimateUpgradeUpgradeSO : SkillLevelUpgradeSO
    {
        public override UpgradeType Type => UpgradeType.UltimateUpgrade;
    }
}
