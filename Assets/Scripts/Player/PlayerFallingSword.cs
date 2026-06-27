using Agents;
using Agents.CombatSystem;
using csiimnida.CSILib.SoundManager.RunTime;
using Enemy.Boss;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using System.Managers;
using UnityEngine;

namespace Player
{
    // 낙하검(Sword Rain)이 소환하는 검.
    // 한 지점에서 소환되어 지정된 목표 지점(주변 적)을 향해 빠르게 날아가다
    // 지면/적과 충돌하면 범위 피해를 입히고 풀로 반환된다.
    // 목표가 없으면 그 자리에서 수직으로 낙하한다.
    // 진화("천검우")에서는 착지 지점에 PlayerDamageField(지속 피해 장판)를 남긴다.
    // 충돌 감지: FixedUpdate에서 impactSize 기반 OverlapBoxNonAlloc — 별도 Collider 컴포넌트 불필요
    public class PlayerFallingSword : AbstractMonoPoolable
    {
        [SerializeField] private float flySpeed = 30f;
        [SerializeField] private float spinSpeed = 360f;        // 낙하 중 날 회전 속도 (deg/s)
        [SerializeField] private float turnSpeed = 3f;          // 목표 방향으로 선회하는 속도
        [SerializeField] private Vector3 impactSize;
        [SerializeField] private LayerMask hitLayer;
        [SerializeField] private LayerMask groundLayer;

        [Header("이펙트")]
        [SerializeField] private PoolItemSO impactVfx;
        [SerializeField] private EventChannelSO createChannel;
        [SerializeField] private EventChannelSO bossChannel;

        [Header("사운드")]
        [SerializeField] private SoundSo fallSfx;
        [SerializeField] private SoundSo impactSfx;

        [Header("타격감")]
        [SerializeField] private float shakeForce = 0.5f;
        [SerializeField] private float hitStopDuration = 0.06f;
        [SerializeField] private float hitStopTimeScale = 0.05f;

        [Header("진화 — 잔류 장판")]
        [SerializeField] private PoolManagerSO poolManagerAsset;
        [SerializeField] private PoolItemSO groundFieldItem;

        private readonly Collider[] _hitBuffer = new Collider[12];

        private float _damage;
        private Agent _owner;
        private HitType _hitType;
        private float _radiusMultiplier;
        // 0 = 잔류 장판 없음
        private float _groundFieldDuration;
        private float _groundFieldDps;
        private float _runtimeFlySpeed;
        private bool _applyHitStop;

        private Vector3 _direction;
        private Vector3 _targetPosition;
        private bool _hasTarget;
        private bool _hasImpacted;
        private float _spinAngle;

        private float _hoverTimer;
        private bool _isHovering;

        public override void ResetItem()
        {
            base.ResetItem();
            transform.localScale = Vector3.one;
            _hasImpacted = false;
            _spinAngle = 0f;
            _hoverTimer = 0f;
            _isHovering = false;
        }

        // targetPosition이 있으면 해당 지점을 향해 직선으로 날아가고, 없으면 제자리에서 수직 낙하한다.
        public void Launch(Agent owner, Vector3? targetPosition, float damage, HitType hitType, float radiusMultiplier,
            float speedMultiplier = 1f, float groundFieldDuration = 0f, float groundFieldDps = 0f,
            float hoverDuration = 0f, bool applyHitStop = true)
        {
            _owner = owner;
            _damage = damage;
            _hitType = hitType;
            _radiusMultiplier = radiusMultiplier;
            _groundFieldDuration = groundFieldDuration;
            _groundFieldDps = groundFieldDps;
            _runtimeFlySpeed = flySpeed * speedMultiplier;
            _applyHitStop = applyHitStop;

            _hasTarget = targetPosition.HasValue;
            if (_hasTarget)
                _targetPosition = targetPosition.Value;

            // 시작은 항상 수직 낙하 — 검 끝(로컬 Y+)이 아래를 향하도록
            _direction = Vector3.down;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, _direction);

            if (hoverDuration > 0f)
            {
                _hoverTimer = hoverDuration;
                _isHovering = true;
            }
        }

        private void FixedUpdate()
        {
            if (_hasImpacted) return;

            if (_isHovering)
            {
                _hoverTimer -= Time.fixedDeltaTime;
                if (_hoverTimer <= 0f)
                {
                    _isHovering = false;
                    if (fallSfx != null)
                        SoundManager.Instance.PlaySound(fallSfx, transform.position);
                }
                return;
            }

            // 목표 방향으로 점진 선회 (수직 낙하 → 곡선 궤적)
            if (_hasTarget)
            {
                Vector3 toTarget = (_targetPosition - transform.position);
                if (toTarget.sqrMagnitude > 0.0001f)
                    _direction = Vector3.Slerp(_direction, toTarget.normalized, turnSpeed * Time.fixedDeltaTime);
            }

            // 검 끝(로컬 Y+)이 이동 방향을 향하도록 + 블레이드 축(로컬 Y) 기준 스핀
            // FromToRotation은 up↔down 반평행 케이스도 안전하게 처리됨
            transform.rotation = Quaternion.FromToRotation(Vector3.up, _direction)
                                * Quaternion.AngleAxis(_spinAngle, Vector3.up);
            _spinAngle += spinSpeed * Time.fixedDeltaTime;

            float moveDelta = _runtimeFlySpeed * Time.fixedDeltaTime;
            Vector3 nextPos = transform.position + _direction * moveDelta;

            // 다음 위치에서 impactSize 박스로 지면·적 감지
            if (Physics.OverlapBoxNonAlloc(nextPos, impactSize, _hitBuffer, transform.rotation, groundLayer | hitLayer) > 0)
            {
                Impact(nextPos);
                return;
            }

            // 목표 지점 도달 체크
            if (_hasTarget && Vector3.Distance(transform.position, _targetPosition) <= moveDelta)
            {
                Impact(_targetPosition);
                return;
            }

            transform.position = nextPos;
        }

        private void Impact(Vector3 point)
        {
            if (_hasImpacted) return;
            _hasImpacted = true;

            int count = Physics.OverlapBoxNonAlloc(point, impactSize * _radiusMultiplier, _hitBuffer, Quaternion.identity, hitLayer);
            for (int i = 0; i < count; i++)
            {
                if (!_hitBuffer[i].TryGetComponent<IDamageable>(out var damageable)) continue;
                Vector3 hitPoint = Physics.ClosestPoint(point, _hitBuffer[i], _hitBuffer[i].transform.position, _hitBuffer[i].transform.rotation);
                damageable.TakeDamage(new DamageData(_damage, hitPoint, Vector3.up, _owner, false, _hitType));
            }

            if (impactSfx != null)
                SoundManager.Instance.PlaySound(impactSfx, point);

            bossChannel?.RaiseEvent(BossEvents.CameraShakeRequest.Init(shakeForce));

            if (_applyHitStop)
                TimeManager.Instance?.HitStop(hitStopDuration, hitStopTimeScale);

            if (impactVfx != null)
                createChannel?.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(impactVfx, point, Quaternion.identity));

            if (_groundFieldDuration > 0f && groundFieldItem != null && poolManagerAsset != null)
            {
                PlayerDamageField field = poolManagerAsset.Pop<PlayerDamageField>(groundFieldItem);
                field.transform.SetPositionAndRotation(point, Quaternion.identity);
                field.Activate(_owner, _groundFieldDps, _groundFieldDuration, _radiusMultiplier, _hitType);
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
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.5f);
            Gizmos.DrawWireCube(transform.position, impactSize * Mathf.Max(_radiusMultiplier, 1f) * 2);
            Gizmos.DrawRay(transform.position, _direction * 2f);
        }
#endif
    }
}
