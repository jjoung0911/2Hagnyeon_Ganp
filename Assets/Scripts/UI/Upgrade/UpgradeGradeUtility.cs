using System.UpgradeSystem;
using UnityEngine;

namespace UI.Upgrade
{
    // 업그레이드 등급별 표시 색상을 제공
    public static class UpgradeGradeUtility
    {
        public static Color GetColor(UpgradeGrade grade)
        {
            switch (grade)
            {
                case UpgradeGrade.Common: return Color.white;
                case UpgradeGrade.Rare: return new Color(0.3f, 0.6f, 1f);
                case UpgradeGrade.Epic: return new Color(0.7f, 0.3f, 1f);
                case UpgradeGrade.Legendary: return new Color(1f, 0.65f, 0f);
                default: return Color.white;
            }
        }
    }
}
