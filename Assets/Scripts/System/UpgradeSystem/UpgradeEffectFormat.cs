using System.StatSystem;

namespace System.UpgradeSystem
{
    // 스탯 모디파이어 수치를 툴팁에 표시할 문자열로 변환하는 공용 포맷터
    public static class UpgradeEffectFormat
    {
        public static string FormatStatModifier(StatSO stat, float modifyAmount)
        {
            if (stat == null) return null;

            string label = string.IsNullOrEmpty(stat.DisplayName) ? stat.StatName : stat.DisplayName;
            string sign = modifyAmount >= 0 ? "+" : "";
            string suffix = stat.IsPercent ? "%" : "";
            return $"{label} {sign}{modifyAmount:0.##}{suffix}";
        }
    }
}
