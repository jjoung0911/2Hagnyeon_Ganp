namespace Enemy
{
    // 난이도 스케일링 대상 — DifficultyManager가 스폰 시점에 경과 시간 기반 배율을 적용한다.
    public interface IDifficultyScalable
    {
        // statMult: HP 등 스탯 배율, damageMult: 가하는 데미지 배율 (둘 다 1 = 변화 없음)
        void ApplyDifficultyScale(float statMult, float damageMult);
    }
}
