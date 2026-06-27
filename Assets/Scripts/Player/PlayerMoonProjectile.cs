using System.Collections.Generic;
using Agents;
using Agents.CombatSystem;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using UnityEngine;

namespace Player
{
    // 초승달베기가 발사하는 관통형 투사체.
    // 진행 중 OverlapSphere로 적을 검사하고, 관통 횟수가 남아있으면 계속 전진한다.
    // 명중 시 폭발(옵션) 및 2차 투사체 생성(진화 "월광참")을 처리한다.
    public class PlayerMoonProjectile : AbstractMonoPoolable
    {
        [SerializeField] private float speed = 18f;
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private float hitRadius = 0.6f;
        [SerializeField] private LayerMask hitLayer;

        [Header("이펙트")]
        [SerializeField] private PoolItemSO hitVfx;
        [SerializeField] private PoolItemSO explosionVfx;
        [SerializeField] private EventChannelSO createChannel;

        [SerializeField] private PoolManagerSO poolManagerAsset;

        [Header("진화 — 2차 투사체")]
        [SerializeField] private PoolItemSO secondaryProjectileItem;
        // 2차 투사체 데미지 배율 (1차 대비)
        [SerializeField, Range(0f, 1f)] private float secondaryDamageRatio = 0.5f;

        private readonly Collider[] _hitBuffer = new Collider[8];
        private readonly HashSet<IDamageable> _hitTargets = new();

        private float _damage;
        private Agent _owner;
        private HitType _hitType;
        private int _pierceRemaining;
        // 0 = 폭발 없음
        private float _explosionRadius;
        private bool _spawnsSecondary;
        private float _spawnTime;

        public override void ResetItem()
        {
            base.ResetItem();
            _hitTargets.Clear();
            transform.localScale = Vector3.one;
        }

        // sizeMultiplier: 1 = 기본 크기. spawnsSecondary: 명중 시 2차 투사체를 생성할지 (진화 전용)
        public void Launch(Agent owner, float damage, int pierceCount, float explosionRadius,
            HitType hitType, float sizeMultiplier = 1f, float speedMultiplier = 1f, bool spawnsSecondary = false)
        {
            _owner = owner;
            _damage = damage;
            _pierceRemaining = pierceCount;
            _explosionRadius = explosionRadius;
            _hitType = hitType;
            _spawnsSecondary = spawnsSecondary;
            _spawnTime = Time.time;
            _hitTargets.Clear();

            transform.localScale = Vector3.one * sizeMultiplier;
            _runtimeSpeed = speed * speedMultiplier;
        }

        private float _runtimeSpeed;

        private void Update()
        {
            if (Time.time - _spawnTime > lifetime)
            {
                ReturnToPool();
                return;
            }

            int count = Physics.OverlapSphereNonAlloc(transform.position, hitRadius, _hitBuffer, hitLayer);
            for (int i = 0; i < count; i++)
            {
                Collider col = _hitBuffer[i];
                if (!col.TryGetComponent<IDamageable>(out var damageable)) continue;
                if (!_hitTargets.Add(damageable)) continue;

                Vector3 hitPoint = Physics.ClosestPoint(transform.position, col, col.transform.position, col.transform.rotation);
                Vector3 hitNormal = -transform.forward;
                OnTargetHit(col, damageable, hitPoint, hitNormal);

                if (_pierceRemaining <= 0)
                {
                    ReturnToPool();
                    return;
                }
                _pierceRemaining--;
            }

            transform.position += transform.forward * (_runtimeSpeed * Time.deltaTime);
        }

        private void OnTargetHit(Collider col, IDamageable damageable, Vector3 hitPoint, Vector3 hitNormal)
        {
            damageable.TakeDamage(new DamageData(_damage, hitPoint, hitNormal, _owner, false, _hitType));

            if (hitVfx != null)
                createChannel?.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(hitVfx, hitPoint, Quaternion.LookRotation(hitNormal)));

            if (_explosionRadius > 0f)
                Explode(hitPoint);

            if (_spawnsSecondary && secondaryProjectileItem != null && poolManagerAsset != null)
                SpawnSecondary(hitPoint);
        }

        private void Explode(Vector3 point)
        {
            int count = Physics.OverlapSphereNonAlloc(point, _explosionRadius, _hitBuffer, hitLayer);
            for (int i = 0; i < count; i++)
            {
                Collider col = _hitBuffer[i];
                if (!col.TryGetComponent<IDamageable>(out var damageable)) continue;
                if (!_hitTargets.Add(damageable)) continue;

                Vector3 hitPoint = Physics.ClosestPoint(point, col, col.transform.position, col.transform.rotation);
                damageable.TakeDamage(new DamageData(_damage, hitPoint, Vector3.up, _owner, false, _hitType));
            }

            if (explosionVfx != null)
                createChannel?.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(explosionVfx, point, Quaternion.identity));
        }

        // 명중 지점에서 같은 방향으로 약화된 2차 투사체를 발사 (진화 "월광참")
        private void SpawnSecondary(Vector3 point)
        {
            PlayerMoonProjectile secondary = poolManagerAsset.Pop<PlayerMoonProjectile>(secondaryProjectileItem);
            secondary.transform.SetPositionAndRotation(point, transform.rotation);
            secondary.Launch(_owner, _damage * secondaryDamageRatio, 0, 0f, _hitType);
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
            Gizmos.color = new Color(0.6f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, hitRadius);
        }
#endif
    }
}
