using Agents;
using Agents.CombatSystem;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using UnityEngine;

namespace Player
{
    // 플레이어가 발사하는 검기.
    // 전방으로 이동하다 hitLayer에 닿으면 데미지 + 히트 VFX 후 풀로 반환된다.
    public class PlayerSwordWave : AbstractMonoPoolable
    {
        [SerializeField] private float speed = 15f;
        [SerializeField] private float lifetime = 4f;
        [SerializeField] private float hitRadius = 0.4f;
        [SerializeField] private LayerMask hitLayer;

        [Header("이펙트")]
        [SerializeField] private PoolItemSO hitVfx;
        [SerializeField] private EventChannelSO createChannel;

        [Header("풀링")]
        [SerializeField] private PoolManagerSO poolManagerAsset;

        private float _damage;
        private Agent _owner;
        private HitType _hitType;
        private float _spawnTime;
        private bool _hasHit;

        public override void ResetItem()
        {
            base.ResetItem();
            _hasHit = false;
        }

        public void Launch(Agent owner, float damage, HitType hitType = HitType.Light)
        {
            _owner = owner;
            _damage = damage;
            _hitType = hitType;
            _spawnTime = Time.time;
            _hasHit = false;
        }

        private void Update()
        {
            if (_hasHit) return;

            if (Time.time - _spawnTime > lifetime)
            {
                ReturnToPool();
                return;
            }

            float moveDelta = speed * Time.deltaTime;

            if (Physics.SphereCast(transform.position, hitRadius, transform.forward,
                    out RaycastHit hit, moveDelta, hitLayer))
            {
                OnWaveHit(hit);
                return;
            }

            transform.position += transform.forward * moveDelta;
        }

        private void OnWaveHit(RaycastHit hit)
        {
            _hasHit = true;

            if (hitVfx != null)
                createChannel?.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(
                    hitVfx, hit.point, Quaternion.LookRotation(hit.normal)));

            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(new DamageData(
                    _damage, hit.point, hit.normal, _owner, false, _hitType));
            }

            ReturnToPool();
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
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, hitRadius);
            Gizmos.DrawRay(transform.position, transform.forward * speed);
        }
#endif
    }
}
