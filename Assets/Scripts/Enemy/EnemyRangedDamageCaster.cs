using Agents.CombatSystem;
using Agents.Modules;
using UnityEngine;

namespace Enemy
{
    // 전방으로 SphereCast를 시전해 원거리 데미지를 적용하는 캐스터
    public class EnemyRangedDamageCaster : AbstractDamageCaster, IOneShotCaster
    {
        [SerializeField] private float attackRange = 10f;
        [SerializeField] private float attackRadius = 0.3f;
        [SerializeField] private float attackDamage = 8f;
        [SerializeField] private LayerMask targetLayer;

        public void CastOnce()
        {
            Vector3 origin = transform.position;
            Vector3 direction = transform.forward;

            if (!Physics.SphereCast(origin, attackRadius, direction, out RaycastHit hit, attackRange, targetLayer))
            {
                ResetDamageOverride();
                return;
            }

            if (!hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                ResetDamageOverride();
                return;
            }

            Vector3 hitPoint = Physics.ClosestPoint(
                origin + direction * hit.distance,
                hit.collider,
                hit.collider.transform.position,
                hit.collider.transform.rotation);

            ApplyDamage(hit.collider, damageable, new DamageData(
                GetEffectiveDamage(attackDamage),
                hitPoint,
                hit.normal,
                _owner,
                false,
                CurrentHitType));

            ResetDamageOverride();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, attackRadius);
            Gizmos.DrawRay(transform.position, transform.forward * attackRange);
        }
#endif
    }
}
