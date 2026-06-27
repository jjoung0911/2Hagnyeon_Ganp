using Agents.Modules;
using UnityEngine;

namespace Player.Skills.Finishers
{
    // 차원참(Lv8) — 검신 스택이 최대치(기본 10)일 때만 발동하는 전방 광역 관통 공격.
    // OverlapDamageCaster(Box, 전방)로 범위 내 모든 적을 1회 관통 타격한다.
    // 발동 시 true를 반환해 스킬이 스택을 소모하도록 신호한다.
    public class DimensionSlashFinisher : AbstractFinisherEffect
    {
        [Header("전방 광역 관통 판정")]
        [SerializeField] private OverlapDamageCaster caster;

        public override bool Execute(in FinisherContext ctx)
        {
            // 스택이 최대치에 도달했을 때만 발동
            if (ctx.MaxStacks <= 0 || ctx.CurrentStacks < ctx.MaxStacks) return false;
            if (caster == null) return false;

            caster.CurrentHitType = ctx.HitType;
            caster.SetDamageOverride(ctx.Damage(damageCoefficient));
            caster.CastOnce();
            PlayCastFeedback(ctx.Origin, ctx.Rotation);
            return true; // 스택 소모 신호
        }
    }
}
