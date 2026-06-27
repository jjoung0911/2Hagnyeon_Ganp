namespace System.UpgradeSystem
{
    // 업그레이드 효과의 분류. 각 UpgradeDataSO 서브클래스가 자신의 Type을 고정 반환한다.
    public enum UpgradeType
    {
        Passive,
        SkillUnlock,
        SkillUpgrade,
        UltimateUpgrade,
        ActiveSkillUnlock,
        ActiveSkillUpgrade,
        ActiveSkillEvolve
    }
}
