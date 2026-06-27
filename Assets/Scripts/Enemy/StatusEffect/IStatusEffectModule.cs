namespace Enemy
{
    // 기절/둔화/출혈 등 상태이상 디버프를 적용/조회하기 위한 인터페이스.
    public interface IStatusEffectModule
    {
        bool IsStunned { get; }
        bool IsBleeding { get; }
        float SpeedMultiplier { get; }

        void ApplyStun(float duration);
        void ApplySlow(float ratio, float duration);
        // 출혈 — duration 동안 tickInterval마다 damagePerTick의 고정 피해를 입힌다 (무적 무시)
        void ApplyBleed(float damagePerTick, float tickInterval, float duration);
        void ResetEffects();
    }
}
