using Agents.CombatSystem;
using Agents.Modules;
using UnityEngine;

namespace Enemy
{
    // 자신 주변 OverlapSphere로 범위 내 모든 대상에게 데미지를 적용하는 AoE 캐스터
    public class EnemySlamDamageCaster : AbstractDamageCaster, IOneShotCaster
    {
        [SerializeField] private float slamRadius = 3f;
        [SerializeField] private float attackDamage = 25f;
        [SerializeField] private LayerMask targetLayer;

        private readonly Collider[] _results = new Collider[16];

        public void CastOnce()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, slamRadius, _results, targetLayer);

            for (int i = 0; i < count; i++)
            {
                if (_results[i] == null) continue;
                if (!_results[i].TryGetComponent<IDamageable>(out var damageable)) continue;

                Vector3 hitPoint = _results[i].ClosestPoint(transform.position);
                Vector3 hitNormal = (_results[i].transform.position - transform.position).normalized;

                ApplyDamage(_results[i], damageable, new DamageData(
                    GetEffectiveDamage(attackDamage),
                    hitPoint,
                    hitNormal,
                    _owner,
                    false,
                    CurrentHitType));
            }

            ResetDamageOverride();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, slamRadius);
        }
#endif
    }
}
