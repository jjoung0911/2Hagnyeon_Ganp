using System.Collections.Generic;
using Agents;
using Agents.CombatSystem;
using JWLib.ObjectPool.Runtime;
using UnityEngine;

namespace Player
{
    // 낙하검 진화("천검우")가 남기는 지속 피해 장판.
    // duration 동안 tickInterval마다 범위 내 적에게 고정 피해를 입힌다.
    public class PlayerDamageField : AbstractMonoPoolable
    {
        [SerializeField] private float radius = 2f;
        [SerializeField] private float tickInterval = 0.5f;
        [SerializeField] private LayerMask hitLayer;
        [SerializeField] private PoolManagerSO poolManagerAsset;

        private readonly Collider[] _hitBuffer = new Collider[8];
        private readonly HashSet<IDamageable> _frameHitTargets = new();

        private float _damagePerTick;
        private float _duration;
        private float _elapsed;
        private float _tickTimer;
        private Agent _owner;
        private HitType _hitType;

        public override void ResetItem()
        {
            base.ResetItem();
            transform.localScale = Vector3.one;
            _elapsed = 0f;
        }

        public void Activate(Agent owner, float damagePerTick, float duration, float radiusMultiplier, HitType hitType)
        {
            _owner = owner;
            _damagePerTick = damagePerTick;
            _duration = duration;
            _hitType = hitType;
            _elapsed = 0f;
            _tickTimer = 0f;
            transform.localScale = Vector3.one * radiusMultiplier;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _duration)
            {
                ReturnToPool();
                return;
            }

            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f) return;
            _tickTimer = tickInterval;

            float effectiveRadius = radius * transform.localScale.x;
            _frameHitTargets.Clear();
            int count = Physics.OverlapSphereNonAlloc(transform.position, effectiveRadius, _hitBuffer, hitLayer);
            for (int i = 0; i < count; i++)
            {
                if (!_hitBuffer[i].TryGetComponent<IDamageable>(out var damageable)) continue;
                if (!_frameHitTargets.Add(damageable)) continue;

                damageable.TakeDamage(new DamageData(_damagePerTick, transform.position, Vector3.up, _owner, false, _hitType));
            }
        }

        private void ReturnToPool()
        {
            if (poolManagerAsset != null)
                poolManagerAsset.Push(this);
            else
                gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, radius * transform.localScale.x);
        }
#endif
    }
}
