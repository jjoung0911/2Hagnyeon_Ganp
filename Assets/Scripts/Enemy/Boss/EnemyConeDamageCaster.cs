using Agents.CombatSystem;
using Agents.Modules;
using UnityEngine;

namespace Enemy.Boss
{
    // 전방 부채꼴(반경 + 각도) 범위에 1회 데미지를 적용하는 캐스터
    // Basic Slash / Combo Slash 패턴이 사용한다
    public class EnemyConeDamageCaster : AbstractDamageCaster, IOneShotCaster
    {
        [SerializeField] private float radius = 3f;
        [SerializeField] private float angle = 120f;
        [SerializeField] private float attackDamage = 20f;
        [SerializeField] private LayerMask targetLayer;

        private readonly Collider[] _results = new Collider[16];

        public void CastOnce()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _results, targetLayer);

            for (int i = 0; i < count; i++)
            {
                if (_results[i] == null) continue;

                Vector3 toTarget = _results[i].transform.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude < 0.0001f) continue;

                if (Vector3.Angle(transform.forward, toTarget) > angle * 0.5f) continue;

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
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
            Vector3 forward = transform.forward;
            Quaternion leftRot = Quaternion.AngleAxis(-angle * 0.5f, Vector3.up);
            Quaternion rightRot = Quaternion.AngleAxis(angle * 0.5f, Vector3.up);
            Gizmos.DrawRay(transform.position, leftRot * forward * radius);
            Gizmos.DrawRay(transform.position, rightRot * forward * radius);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif
    }
}
