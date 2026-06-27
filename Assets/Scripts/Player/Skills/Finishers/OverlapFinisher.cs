using Agents.Modules;
using UnityEngine;

namespace Player.Skills.Finishers
{
    // 범위형 피니셔 — 잔상 베기(Lv5)·충격파(Lv6) 공용.
    // 둘은 "OverlapDamageCaster + 전용 VFX + 계수"만 다른 동일 구조이므로 한 클래스로 통합한다.
    // (잔상 = 좁은 박스/구, 충격파 = 넓은 구 — 차이는 caster 설정·vfx·계수·해금 레벨 데이터뿐)
    public class OverlapFinisher : AbstractFinisherEffect
    {
        [Header("범위 판정")]
        [SerializeField] private OverlapDamageCaster caster;

        public override bool Execute(in FinisherContext ctx)
        {
            if (caster == null) return false;

            caster.CurrentHitType = ctx.HitType;
            caster.SetDamageOverride(ctx.Damage(damageCoefficient));
            caster.CastOnce();
            PlayCastFeedback(ctx.Origin, ctx.Rotation);
            return false; // 스택 소모 없음
        }
    }
}
