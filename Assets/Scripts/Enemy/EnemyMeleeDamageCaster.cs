using Agents.CombatSystem;
using Agents.Modules;
using UnityEngine;

namespace Enemy
{
    public class EnemyMeleeDamageCaster : AbstractDamageCaster, IOneShotCaster
    {
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private LayerMask targetLayer;

        private readonly Collider[] _results = new Collider[8];

        public void CastOnce()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, attackRange, _results, targetLayer);
            for (int i = 0; i < count; i++)
            {
                if (_results[i] == null) continue;
                if (!_results[i].TryGetComponent<IDamageable>(out var damageable)) continue;

                Vector3 hitPoint = _results[i].ClosestPoint(transform.position);
                Vector3 hitNormal = (hitPoint - transform.position).normalized;

                ApplyDamage(_results[i], damageable, new DamageData(
                    GetEffectiveDamage(attackDamage),
                    hitPoint,
                    hitNormal,
                    _owner,
                    false,
                    CurrentHitType
                ));
            }
            ResetDamageOverride();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
#endif
    }
}
