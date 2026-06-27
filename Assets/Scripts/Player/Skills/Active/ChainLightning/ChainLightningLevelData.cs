namespace Player.Skills.Active
{
    // Chain Lightning 레벨별 수치.
    [System.Serializable]
    public struct ChainLightningLevelData
    {
        public int chainCount;
        public float chainRange;
        public float damage;
        // 0~1. 체인이 진행될수록 데미지에 곱해지는 감쇠율
        public float damageFalloff;
        // 0이면 미적용. 0보다 크면 감전된 적을 stunDuration초 동안 기절시킴 (Lv5)
        public float stunDuration;
    }
}
