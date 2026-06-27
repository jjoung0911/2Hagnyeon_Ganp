using JWLib.ObjectPool.Runtime;
using UnityEngine;

namespace Player.Skills.Finishers
{
    // 검기(Lv4) — 전방 직선으로 날아가는 검기 발사(관통 없음).
    // 프로젝트가 원래 소환하던 PlayerSwordWave를 그대로 재사용해 발사한다.
    public class SwordWaveFinisher : AbstractFinisherEffect
    {
        [Header("검기 발사")]
        [SerializeField] private PoolManagerSO poolManager;
        [SerializeField] private PoolItemSO swordWaveItem;

        public override bool Execute(in FinisherContext ctx)
        {
            if (poolManager == null || swordWaveItem == null) return false;

            PlayerSwordWave wave = poolManager.Pop<PlayerSwordWave>(swordWaveItem);
            if (wave == null) return false;

            wave.transform.SetPositionAndRotation(ctx.Origin, ctx.Rotation);
            wave.Launch(ctx.Owner, ctx.Damage(damageCoefficient), ctx.HitType);
            PlayCastFeedback(ctx.Origin, ctx.Rotation);
            return false; // 스택 소모 없음
        }
    }
}
