using Agents.CombatSystem;
using UnityEngine;

namespace Agents.Modules
{
    public class RayDamageCaster : AbstractDamageCaster, IOneShotCaster
    {
        public enum CastType { Ray, Sphere, Box }

        [SerializeField] private CastType castType = CastType.Sphere;
        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private Vector3 boxSize = Vector3.one;
        [SerializeField, Range(0.5f, 3f)] private float casterRadius = 1f;
        [SerializeField, Range(0f, 1f)] private float casterInterpolation = 0.5f;
        [SerializeField, Range(0f, 3f)] private float castingRange = 1f;
        [SerializeField] private bool isDebugMode;


        public Vector3 LastHitPosition { get; private set; }
        public Vector3 LastHitNormal { get; private set; }

        private Vector3? _directionOverride;
        private readonly RaycastHit[] _hitBuffer = new RaycastHit[10];

        // 업그레이드로 공격 범위(반경/사거리)를 배율로 조정 — 1 = 변화 없음
        private float _rangeMultiplier = 1f;

        public void SetDirectionOverride(Vector3 direction) => _directionOverride = direction.normalized;
        public void SetRangeMultiplier(float multiplier) => _rangeMultiplier = Mathf.Max(0.1f, multiplier);

        public void CastOnce()
        {
            Vector3 dir = _directionOverride ?? transform.forward;
            _directionOverride = null;
            CastDamageAll(transform.position, dir, GetEffectiveDamage(0f));
            ResetDamageOverride();
        }

        // 단일 히트 — 외부에서 직접 호출 시 사용 (기존 호환성 유지)
        public bool CastDamage(Vector3 position, Vector3 direction, float damageAmount, float damageMultiplier = 1f)
        {
            Vector3 start = position - direction * (casterInterpolation * 2f);

            if (!TryCast(start, direction, out RaycastHit hit)) return false;
            if (!hit.collider.TryGetComponent<IDamageable>(out var damageable)) return false;

            Vector3 sphereCenter = start + direction * hit.distance;
            LastHitPosition = Physics.ClosestPoint(
                sphereCenter, hit.collider,
                hit.collider.transform.position, hit.collider.transform.rotation);
            LastHitNormal = hit.normal;

            ApplyDamage(hit.collider, damageable, new DamageData(
                damageAmount * damageMultiplier,
                LastHitPosition,
                LastHitNormal,
                _owner,
                false,
                CurrentHitType));

            return true;
        }

        // 범위 내 모든 적 히트
        private void CastDamageAll(Vector3 position, Vector3 direction, float damageAmount)
        {
            Vector3 start = position - direction * (casterInterpolation * 2f);
            int hitCount = TryCastAll(start, direction);
            Debug.Log(hitCount);
            for (int i = 0; i < hitCount; i++)
            {
                var hit = _hitBuffer[i];
                if (!hit.collider.TryGetComponent<IDamageable>(out var damageable)) continue;

                Vector3 sphereCenter = start + direction * hit.distance;
                LastHitPosition = Physics.ClosestPoint(
                    sphereCenter, hit.collider,
                    hit.collider.transform.position, hit.collider.transform.rotation);
                LastHitNormal = hit.normal;

                    Debug.Log(hit.transform.gameObject.name);
                ApplyDamage(hit.collider, damageable, new DamageData(
                    damageAmount,
                    LastHitPosition,
                    LastHitNormal,
                    _owner,
                    false,
                    CurrentHitType));
            }
        }

        private bool TryCast(Vector3 start, Vector3 direction, out RaycastHit hit)
        {
            float radius = casterRadius * _rangeMultiplier;
            float range = castingRange * _rangeMultiplier;
            return castType switch
            {
                CastType.Ray => Physics.Raycast(start, direction, out hit, range, targetLayer),
                CastType.Sphere => Physics.SphereCast(start, radius, direction, out hit, range, targetLayer),
                CastType.Box => Physics.BoxCast(start, boxSize * 0.5f * _rangeMultiplier, direction, out hit, transform.rotation, range, targetLayer),
                _ => throw new System.ArgumentOutOfRangeException()
            };
        }

        private int TryCastAll(Vector3 start, Vector3 direction)
        {
            float radius = casterRadius * _rangeMultiplier;
            float range = castingRange * _rangeMultiplier;
            return castType switch
            {
                CastType.Ray => Physics.RaycastNonAlloc(start, direction, _hitBuffer, range, targetLayer),
                CastType.Sphere => Physics.SphereCastNonAlloc(start, radius, direction, _hitBuffer, range, targetLayer),
                CastType.Box => Physics.BoxCastNonAlloc(start, boxSize * 0.5f * _rangeMultiplier, direction, _hitBuffer, transform.rotation, range, targetLayer),
                _ => throw new System.ArgumentOutOfRangeException()
            };
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!isDebugMode) return;

            Vector3 start = transform.position - transform.forward * (casterInterpolation * 2f);
            Vector3 end = start + transform.forward * castingRange;

            switch (castType)
            {
                case CastType.Ray:
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(start, transform.forward * castingRange);
                    break;
                case CastType.Sphere:
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(start, casterRadius);
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(end, casterRadius);
                    break;
                case CastType.Box:
                    Matrix4x4 prevMatrix = Gizmos.matrix;
                    Gizmos.color = Color.green;
                    Gizmos.matrix = Matrix4x4.TRS(start, transform.rotation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, boxSize);
                    Gizmos.color = Color.red;
                    Gizmos.matrix = Matrix4x4.TRS(end, transform.rotation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, boxSize);
                    Gizmos.matrix = prevMatrix;
                    break;
            }
        }
#endif
    }
}
