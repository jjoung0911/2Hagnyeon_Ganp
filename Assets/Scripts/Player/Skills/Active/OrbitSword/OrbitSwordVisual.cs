using System.Collections.Generic;
using Agents.CombatSystem;
using Agents.Modules;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using UnityEngine;

namespace Player.Skills.Active
{
    // 플레이어 주위를 떠다니는 검 비주얼.
    // 부유 중에는 OrbitSwordSkill이 위치를 직접 갱신하고,
    // 발사된 뒤에는 스스로 플레이어 주변 적을 탐색·호밍하며 관통 타격한다.
    // 충돌해도 소멸하지 않고, 적이 사라지면 OrbitSwordSkill이 다시 부유 상태로 되돌린다.
    public class OrbitSwordVisual : AbstractMonoPoolable
    {
        private const int MaxDetectBuffer = 8;
        private const int MaxCastBuffer = 8;
        private const float DefaultReHitInterval = 0.3f;

        [SerializeField] private Vector3 hitBoxHalfExtents = new(0.15f, 0.6f, 0.15f);
        [SerializeField] private HitType hitType = HitType.Light;
        [SerializeField] private PoolItemSO hitPoolItem;
        [SerializeField] private EventChannelSO createEvent;
        [SerializeField] private Vector3 offset;
        // 타격 후 그 적에게서 이만큼 벌어진 뒤 다음 적을 탐색한다 — 적 안에서 방향이 떨리는 현상 방지
        [SerializeField] private float retreatDistance = 2f;

        private readonly Collider[] _detectBuffer = new Collider[MaxDetectBuffer];
        private readonly RaycastHit[] _castBuffer = new RaycastHit[MaxCastBuffer];
        // 검마다 적별 마지막 타격 시각 — 재타격 쿨다운으로 중복 데미지 방지
        private readonly Dictionary<Collider, float> _hitCooldowns = new();

        // 발사된 검의 행동 단계 — 적에게 호밍(Seeking)하거나 타격 후 후퇴(Retreating)한다
        private enum AttackPhase { Seeking, Retreating }

        private AttackPhase _phase;
        private Vector3 _retreatFromPoint; // 후퇴 시 기준이 되는 마지막 타격 지점
        private bool _isLaunched;
        private float _speed;
        private float _damage;
        private float _detectRadius;
        private float _reHitInterval;
        private float _maxDistance;
        private LayerMask _targetLayer;
        private ModuleOwner _owner;

        // 적을 향해 발사 — 이후 매 프레임 스스로 탐색/호밍/관통 타격
        public void Launch(ModuleOwner owner, float speed, float damage, float detectRadius,
            float reHitInterval, float maxDistance, LayerMask targetLayer)
        {
            _owner = owner;
            _speed = speed;
            _damage = damage;
            _detectRadius = detectRadius;
            _reHitInterval = reHitInterval > 0f ? reHitInterval : DefaultReHitInterval;
            _maxDistance = maxDistance > 0f ? maxDistance : detectRadius;
            _targetLayer = targetLayer;
            _hitCooldowns.Clear();
            _phase = AttackPhase.Seeking;
            _isLaunched = true;
        }

        // 공격 중단 — 적이 사라져 부유 상태로 복귀할 때 OrbitSwordSkill이 호출.
        // 검은 씬에 남고 이후 위치는 다시 스킬이 SmoothDamp로 갱신한다.
        public void StopAttack()
        {
            _isLaunched = false;
            _phase = AttackPhase.Seeking;
            _hitCooldowns.Clear();
        }

        public override void ResetItem()
        {
            base.ResetItem();
            _isLaunched = false;
            _phase = AttackPhase.Seeking;
            _hitCooldowns.Clear();
        }

        private void Update()
        {
            if (!_isLaunched) return;

            Vector3 playerPos = _owner.transform.position;
            float moveDelta = _speed * Time.deltaTime;

            if (_phase == AttackPhase.Retreating)
            {
                UpdateRetreat(playerPos, moveDelta);
                return;
            }

            UpdateSeeking(playerPos, moveDelta);
        }

        // 쿨다운이 풀린 가장 가까운 적으로 호밍·관통 타격. 타격에 성공하면 후퇴 단계로 전환한다.
        private void UpdateSeeking(Vector3 playerPos, float moveDelta)
        {
            Collider target = FindReadyTarget(playerPos);
            if (target == null) return; // 칠 수 있는 적이 없으면 정지 — 적이 모두 사라지면 스킬이 부유로 전환

            // 발이 아닌 몸 중앙(bounds.center)을 조준
            Vector3 direction = target.bounds.center - transform.position;
            if (direction.sqrMagnitude < 0.0001f) direction = transform.up;
            Vector3 dir = direction.normalized;

            // 검 모델의 칼날 방향이 로컬 +Y축이므로 +Y축을 진행 방향에 맞춘다
            Quaternion orientation = Quaternion.FromToRotation(Vector3.up, dir);

            bool hit = DamageAlongPath(dir, orientation, moveDelta);
            MoveWithLeash(playerPos, dir, moveDelta, orientation);

            // 타격 직후 적 안에서 방향이 떨리지 않도록, 그 적에게서 벌어지는 후퇴 단계로 전환
            if (hit)
            {
                _retreatFromPoint = target.bounds.center;
                _phase = AttackPhase.Retreating;
            }
        }

        // 마지막 타격 지점에서 retreatDistance만큼 멀어질 때까지 후퇴. 충분히 벌어지면 다시 탐색 단계로.
        private void UpdateRetreat(Vector3 playerPos, float moveDelta)
        {
            Vector3 away = transform.position - _retreatFromPoint;
            if (away.sqrMagnitude < 0.0001f) away = transform.up;
            float dist = away.magnitude;

            if (dist >= retreatDistance)
            {
                _phase = AttackPhase.Seeking;
                return;
            }

            Vector3 dir = away / dist;
            Quaternion orientation = Quaternion.FromToRotation(Vector3.up, dir);
            Vector3 before = transform.position;
            MoveWithLeash(playerPos, dir, moveDelta, orientation);

            // 리시(leash)에 막혀 더 벌어지지 못하면 후퇴를 끝내고 탐색으로 전환
            if ((transform.position - before).sqrMagnitude < (moveDelta * moveDelta) * 0.01f)
                _phase = AttackPhase.Seeking;
        }

        // 쿨다운이 풀린 가장 가까운 적을 선택. 칠 수 있는 적이 없으면 null.
        private Collider FindReadyTarget(Vector3 playerPos)
        {
            int count = Physics.OverlapSphereNonAlloc(playerPos, _detectRadius, _detectBuffer, _targetLayer);

            Collider nearest = null;
            float minSq = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider col = _detectBuffer[i];
                if (!IsOffCooldown(col)) continue;

                float sqDist = (col.bounds.center - transform.position).sqrMagnitude;
                if (sqDist < minSq)
                {
                    minSq = sqDist;
                    nearest = col;
                }
            }

            return nearest;
        }

        // moveDelta 경로상의 모든 적을 관통 타격 — 쿨다운이 풀린 적에게만 데미지. 하나라도 맞히면 true.
        private bool DamageAlongPath(Vector3 dir, Quaternion orientation, float moveDelta)
        {
            int hitCount = Physics.BoxCastNonAlloc(
                transform.position, hitBoxHalfExtents, dir, _castBuffer, orientation, moveDelta, _targetLayer);

            bool hitAny = false;
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _castBuffer[i].collider;
                if (col == null || !IsOffCooldown(col)) continue;

                _hitCooldowns[col] = Time.time;
                hitAny = true;

                if (col.TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(new DamageData(_damage, _castBuffer[i].point, _castBuffer[i].normal, _owner, false, hitType));

                createEvent.RaiseEvent(CreateEvents.ShowPoolingVfx
                    .InitData(hitPoolItem, _castBuffer[i].point, Quaternion.LookRotation(_castBuffer[i].normal)));
            }

            return hitAny;
        }

        // 이동 + 플레이어로부터 maxDistance를 넘지 않도록 위치를 리시(leash)로 제한
        private void MoveWithLeash(Vector3 playerPos, Vector3 dir, float moveDelta, Quaternion orientation)
        {
            Vector3 newPos = transform.position + dir * moveDelta;

            Vector3 fromPlayer = newPos - playerPos;
            if (fromPlayer.sqrMagnitude > _maxDistance * _maxDistance)
                newPos = playerPos + fromPlayer.normalized * _maxDistance;

            transform.position = newPos;
            transform.rotation = orientation;
        }

        private bool IsOffCooldown(Collider col)
            => !_hitCooldowns.TryGetValue(col, out float last) || Time.time - last >= _reHitInterval;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.6f, 0.4f, 0.2f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position + offset, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, hitBoxHalfExtents * 2f);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
