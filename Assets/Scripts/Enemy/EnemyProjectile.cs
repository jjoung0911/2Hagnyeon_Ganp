using Agents;
using Agents.CombatSystem;
using Agents.Modules;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using UnityEngine;

namespace Enemy
{
    // 적이 발사하는 투사체.
    // 전방으로 이동하다 hitLayer에 닿으면 데미지 + 히트 VFX 후 풀로 반환된다.
    // IParryableAttack을 구현해 플레이어 패링 시 반사된다.
    public class EnemyProjectile : AbstractMonoPoolable, IParryableAttack
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float lifetime = 6f;
        [SerializeField] private float hitRadius = 0.15f;
        [SerializeField] private LayerMask hitLayer;

        [Header("반사")]
        // 패링 후 반사됐을 때 타격 가능한 레이어 (보통 Enemy 레이어)
        [SerializeField] private LayerMask reflectLayer;

        [Header("이펙트")]
        [SerializeField] private PoolItemSO hitVfx;
        [SerializeField] private EventChannelSO createChannel;

        [Header("풀링")]
        [SerializeField] private PoolManagerSO poolManagerAsset;

        private LayerMask _originalHitLayer;
        private float _damage;
        private Agent _owner;
        private ModuleOwner _attackerForParry; // 반사 후에도 attacker 참조 유지
        private HitType _hitType;
        private float _spawnTime;
        private bool _hasHit;

        // IParryableAttack
        public ModuleOwner Attacker => _attackerForParry;

        private void Awake()
        {
            _originalHitLayer = hitLayer;
        }

        private void OnEnable()
        {
            ParryableRegistry.Register(this);
        }

        private void OnDisable()
        {
            ParryableRegistry.Unregister(this);
        }

        // 풀에서 꺼낼 때 패링으로 반사됐던 상태(hitLayer 등)를 원래대로 되돌린다
        public override void ResetItem()
        {
            base.ResetItem();
            hitLayer = _originalHitLayer;
            _hasHit = false;
        }

        public void Launch(Agent owner, float damage, HitType hitType = HitType.Light)
        {
            _owner             = owner;
            _attackerForParry  = owner;
            _damage            = damage;
            _hitType           = hitType;
            _spawnTime         = Time.time;
            _hasHit            = false;
        }

        // IParryableAttack: 패링 성공 시 투사체를 반사시킨다
        public void OnParried(Vector3 hitPoint)
        {
            _damage = 0;
            // 뒤집어서 발사한 쪽을 향해 날아가도록
            transform.forward = -transform.forward;
            hitLayer   = reflectLayer;
            _owner     = null;  // 반사된 투사체는 중립 데미지 소스
            _spawnTime = Time.time; // 수명 초기화
            _hasHit    = false;
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
                OnProjectileHit(hit);
                return;
            }

            transform.position += transform.forward * moveDelta;
        }

        private void OnProjectileHit(RaycastHit hit)
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
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, hitRadius);
            Gizmos.DrawRay(transform.position, transform.forward * speed);
        }
#endif
    }
}
