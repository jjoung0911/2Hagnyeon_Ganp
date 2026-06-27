using System.Collections;
using Agents;
using Agents.CombatSystem;
using Agents.Modules;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using UnityEngine;

namespace Enemy.Boss
{
    // Blade Rain 패턴의 검 1개 — 경고 표시 -> 대기 -> 낙하 -> 폭발 판정 -> 소멸을 책임진다
    public class BladeRainProjectile : MonoBehaviour
    {
        [Header("경고")]
        [SerializeField] private AttackTelegraphView telegraph;
        [SerializeField] private float warningDuration = 0.8f;

        [Header("낙하")]
        [SerializeField] private float fallHeight = 10f;
        [SerializeField] private float fallSpeed = 20f;

        [Header("폭발 판정")]
        [SerializeField] private float explosionRadius = 2f;
        [SerializeField] private float damage = 15f;
        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private HitType hitType = HitType.Medium;

        [Header("이펙트")]
        [SerializeField] private PoolItemSO impactVfx;
        [SerializeField] private EventChannelSO createChannel;

        [Header("디졸브 (소환 연출)")]
        [SerializeField] private Renderer[] dissolveRenderers;
        [SerializeField] private string dissolveProperty = "Dissolve_Level";
        [SerializeField] private float dissolveStartValue = -7f;
        [SerializeField] private float dissolveEndValue = 3.3f;
        [SerializeField] private float dissolveDuration = 0.4f;

        private readonly Collider[] _results = new Collider[16];
        private MaterialPropertyBlock _propertyBlock;
        private Agent _owner;
        private Vector3 _groundPosition;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        // groundPosition: 검이 떨어질 바닥 위치
        public void Begin(Agent owner, Vector3 groundPosition)
        {
            _owner = owner;
            _groundPosition = groundPosition;

            // 검을 낙하 시작 높이에 미리 배치 — 바닥에 있다가 하늘로 솟는 부자연스러운 움직임 방지
            transform.position = groundPosition + Vector3.up * fallHeight;
            if (telegraph != null)
            {
                telegraph.SetAngle(0f);
                telegraph.Show(explosionRadius, warningDuration);
            }

            SetDissolveValue(dissolveStartValue);
            StartCoroutine(DissolveInRoutine());
            StartCoroutine(FallRoutine());
        }

        // 소환 시 디졸브 값을 점차 올려 검이 서서히 나타나는 연출
        private IEnumerator DissolveInRoutine()
        {
            float elapsed = 0f;
            while (elapsed < dissolveDuration)
            {
                elapsed += Time.deltaTime;
                SetDissolveValue(Mathf.Lerp(dissolveStartValue, dissolveEndValue, elapsed / dissolveDuration));
                yield return null;
            }

            SetDissolveValue(dissolveEndValue);
        }

        private void SetDissolveValue(float value)
        {
            if (dissolveRenderers == null) return;

            foreach (var r in dissolveRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(dissolveProperty, value);
                r.SetPropertyBlock(_propertyBlock);
            }
        }

        private IEnumerator FallRoutine()
        {
            yield return new WaitForSeconds(warningDuration);

            while (transform.position.y > _groundPosition.y)
            {
                transform.position += Vector3.down * (fallSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = _groundPosition;
            Explode();
        }

        private void Explode()
        {
            if (impactVfx != null)
                createChannel?.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(impactVfx, transform.position, Quaternion.identity));

            int count = Physics.OverlapSphereNonAlloc(transform.position, explosionRadius, _results, targetLayer);
            for (int i = 0; i < count; i++)
            {
                if (_results[i] == null) continue;
                if (!_results[i].TryGetComponent<IDamageable>(out var damageable)) continue;

                Vector3 hitPoint = _results[i].ClosestPoint(transform.position);
                Vector3 hitNormal = (_results[i].transform.position - transform.position).normalized;

                damageable.TakeDamage(new DamageData(damage, hitPoint, hitNormal, _owner, false, hitType));
            }

            Destroy(gameObject);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
#endif
    }
}
