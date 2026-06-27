using System.Collections.Generic;
using Agents.CombatSystem;
using Agents.Modules;
using UnityEngine;

namespace Enemy
{
    // 전방 부채꼴로 여러 SphereCast를 동시에 시전하는 원거리 캐스터
    // 같은 CastOnce() 호출에서 이미 맞은 콜라이더는 중복 데미지 방지
    public class EnemyMultiShotDamageCaster : AbstractDamageCaster, IOneShotCaster
    {
        [SerializeField] private int shotCount = 3;
        [SerializeField] private float spreadAngle = 30f;   // 발사체 간 각도 (도)
        [SerializeField] private float castRange = 10f;
        [SerializeField] private float castRadius = 0.3f;
        [SerializeField] private float attackDamage = 7f;
        [SerializeField] private LayerMask targetLayer;

        private readonly HashSet<Collider> _hitThisShot = new HashSet<Collider>();

        public void CastOnce()
        {
            _hitThisShot.Clear();

            for (int i = 0; i < shotCount; i++)
            {
                // 중앙 기준 좌우 대칭으로 각도 배분
                float angle = (i - (shotCount - 1) * 0.5f) * spreadAngle;
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
                CastShot(dir);
            }

            ResetDamageOverride();
        }

        private void CastShot(Vector3 direction)
        {
            if (!Physics.SphereCast(transform.position, castRadius, direction, out RaycastHit hit, castRange, targetLayer))
                return;
            if (_hitThisShot.Contains(hit.collider))
                return;
            if (!hit.collider.TryGetComponent<IDamageable>(out var damageable))
                return;

            _hitThisShot.Add(hit.collider);

            Vector3 hitPoint = Physics.ClosestPoint(
                transform.position + direction * hit.distance,
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
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.35f);
            for (int i = 0; i < shotCount; i++)
            {
                float angle = (i - (shotCount - 1) * 0.5f) * spreadAngle;
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
                Gizmos.DrawRay(transform.position, dir * castRange);
                Gizmos.DrawWireSphere(transform.position + dir * castRange, castRadius);
            }
        }
#endif
    }
}
