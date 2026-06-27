using System.Collections.Generic;
using Agents.CombatSystem;
using UnityEngine;

namespace Agents.Modules
{
    public class OverlapDamageCaster : AbstractDamageCaster, IOneShotCaster, IWindowCaster
    {
        public enum CastShape { Sphere, Box }

        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private LayerMask targetLayer;

        [Header("Shape")]
        [SerializeField] private CastShape shape = CastShape.Sphere;
        // true이면 구/박스 중심을 이 컴포넌트의 transform 대신 owner(캐릭터 루트) 위치로 사용
        [SerializeField] private bool useOwnerPosition = false;

        [Header("Sphere")]
        [SerializeField] private float radius = 1.5f;

        [Header("Box")]
        [SerializeField] private Vector3 boxSize = Vector3.one;
        [SerializeField] private Vector3 boxOffset = Vector3.zero;

        // 0 = 윈도우 내 타깃당 1회만 피해. >0 = 해당 초 간격으로 재적중 허용 (스핀 스킬 등)
        [Header("Window")]
        [SerializeField] private float hitInterval = 0f;

        private readonly Collider[] _results = new Collider[16];
        private readonly HashSet<IDamageable> _hitTargets = new();         // hitInterval=0 전용
        private readonly Dictionary<IDamageable, float> _intervalMap = new(); // hitInterval>0 전용
        private bool _isActive;

        // 업그레이드로 범위(반경/박스 크기)를 배율로 조정 — 1 = 변화 없음
        private float _rangeMultiplier = 1f;
        public void SetRangeMultiplier(float multiplier) => _rangeMultiplier = Mathf.Max(0.1f, multiplier);

        public void CastOnce()
        {
            _hitTargets.Clear();
            ProcessHits(PerformOverlap());
            _hitTargets.Clear();
            ResetDamageOverride();
        }

        public void StartCasting()
        {
            _hitTargets.Clear();
            _intervalMap.Clear();
            _isActive = true;
        }

        public void StopCasting()
        {
            _isActive = false;
            _hitTargets.Clear();
            _intervalMap.Clear();
            ResetDamageOverride();
        }

        private void FixedUpdate()
        {
            if (!_isActive) return;
            ProcessHits(PerformOverlap());
        }

        private void ProcessHits(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_results[i] == null) continue;
                if (!_results[i].TryGetComponent<IDamageable>(out var damageable)) continue;
                if (!TryRegisterHit(damageable)) continue;

                Vector3 hitPoint = Physics.ClosestPoint(
                    transform.position, _results[i],
                    _results[i].transform.position, _results[i].transform.rotation);
                Vector3 hitNormal = (transform.position - _results[i].bounds.center).normalized;

                ApplyDamage(_results[i], damageable, new DamageData(
                    GetEffectiveDamage(attackDamage), hitPoint, hitNormal, _owner, false, CurrentHitType));
            }
        }

        private bool TryRegisterHit(IDamageable target)
        {
            if (hitInterval <= 0f)
                return _hitTargets.Add(target);

            float now = Time.unscaledTime;
            if (_intervalMap.TryGetValue(target, out float last) && now - last < hitInterval)
                return false;
            _intervalMap[target] = now;
            return true;
        }

        private int PerformOverlap()
        {
            Vector3 origin = useOwnerPosition && _owner != null
                ? _owner.transform.position
                : transform.position;

            if (shape == CastShape.Sphere)
                return Physics.OverlapSphereNonAlloc(origin, radius * _rangeMultiplier, _results, targetLayer);

            Vector3 center = origin + transform.TransformDirection(boxOffset);
            return Physics.OverlapBoxNonAlloc(center, boxSize * 0.5f * _rangeMultiplier, _results, transform.rotation, targetLayer);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 origin = (useOwnerPosition && _owner != null)
                ? _owner.transform.position
                : transform.position;

            Gizmos.color = _isActive ? Color.red : new Color(1f, 0.5f, 0f, 0.4f);
            if (shape == CastShape.Sphere)
            {
                Gizmos.DrawWireSphere(origin, radius * _rangeMultiplier);
            }
            else
            {
                Matrix4x4 prev = Gizmos.matrix;
                Vector3 center = origin + transform.TransformDirection(boxOffset);
                Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, boxSize * _rangeMultiplier);
                Gizmos.matrix = prev;
            }
        }
#endif
    }
}
