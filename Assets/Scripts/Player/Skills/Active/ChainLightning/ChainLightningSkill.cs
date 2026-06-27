using System.Collections.Generic;
using System.EffectSystem;
using Agents.CombatSystem;
using Enemy;
using JWLib.ObjectPool.Runtime;
using UnityEngine;

namespace Player.Skills.Active
{
    // 발동형 — 가장 가까운 적부터 시작해 주변 적으로 번개가 옮겨붙는다.
    // 체인 단계마다 damage *= (1 - damageFalloff)로 감쇠한다.
    public class ChainLightningSkill : AbstractTriggeredSkill<ChainLightningLevelData>
    {
        private const int MaxHitBuffer = 16;

        [SerializeField] private LayerMask targetLayer;

        private readonly Collider[] _hitBuffer = new Collider[MaxHitBuffer];
        private readonly HashSet<Collider> _chained = new();
        private GameObject _vfxObject;

        // 체인 라이트닝은 발동 시점에 CurrentLevelData를 그대로 사용하므로 캐시할 상태가 없음
        protected override void OnLevelDataChanged(ChainLightningLevelData levelData) { }

        protected override void Activate()
        {
            var levelData = CurrentLevelData;
            _chained.Clear();

            Vector3 origin = Player.transform.position;
            float damage = levelData.damage;

            for (int i = 0; i < levelData.chainCount; i++)
            {
                Collider target = FindNearestTarget(origin, levelData.chainRange);
                if (target == null) break;

                _chained.Add(target);

                if (target.TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(new DamageData(damage, target.transform.position, Vector3.up, Player, false, HitType.Light));

                if (levelData.stunDuration > 0f
                    && target.GetComponentInParent<IStatusEffectModule>() is { } statusEffect)
                    statusEffect.ApplyStun(levelData.stunDuration);

                PlayChainVfx(target.transform.position);

                origin = target.transform.position;
                damage *= 1f - levelData.damageFalloff;
            }
        }

        private Collider FindNearestTarget(Vector3 origin, float range)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, range, _hitBuffer, targetLayer);
            Collider nearest = null;
            float minSqDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider col = _hitBuffer[i];
                if (_chained.Contains(col)) continue;

                float sqDist = (col.transform.position - origin).sqrMagnitude;
                if (sqDist < minSqDist)
                {
                    minSqDist = sqDist;
                    nearest = col;
                }
            }

            return nearest;
        }

        private void PlayChainVfx(Vector3 position)
        {
            if (PopVisual() is not PoolableVFX vfx) return;
            
            vfx.OnVfxEnd += HandleVfxEnd;
            vfx.PlayVfx(position, Quaternion.identity);
        }

        private void HandleVfxEnd(PoolableVFX vfx)
        {
            vfx.OnVfxEnd -= HandleVfxEnd;
            PushVisual(vfx);
        }
    }
}
