namespace Player.Skills.Active
{
    // Aura 레벨별 수치.
    [System.Serializable]
    public struct AuraLevelData
    {
        public float radius;
        public float tickDamage;
        // 0이면 미적용. 0보다 크면 범위 내 적의 이동속도를 (1 - slowPercent)배로 감소 (Lv5)
        public float slowPercent;
        public float slowDuration;
        // 버스트 사이 대기 시간(초) — 이 시간이 지나면 VFX가 나타나고 피해 판정이 시작된다
        public float cooldown;
        // VFX가 유지되며 피해를 주는 시간(초) — 지나면 VFX가 사라지고 다시 cooldown 대기
        public float activeDuration;
    }
}
