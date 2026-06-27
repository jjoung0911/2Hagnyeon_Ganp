using Agents.CombatSystem;
using Agents.Modules;
using JWLib.AnimationSystem;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy.Skills
{
    // 플레이어 뒤로 순간이동 후 공격하는 도적 스킬.
    // ISkillCondition: 지정 사거리 안에 있을 때 사용.
    //
    // 페이즈 1 — vanishClip 재생 + 소멸 VFX
    //   → AnimationEnd → 순간이동 + 출현 VFX
    // 페이즈 2 — attackClip 재생
    //   → AttackStart → 데미지 판정
    //   → AnimationEnd → 종료
    public class EnemyTeleportAttackSkill : AbstractEnemySkill, ISkillCondition
    {
        [Header("애니메이션")]
        [SerializeField] private AnimParamSO vanishClip;
        [SerializeField] private AnimParamSO attackClip;
        [SerializeField] private float crossDuration = 0.1f;

        [Header("순간이동")]
        [SerializeField] private float teleportOffset = 1.5f;  // 플레이어 뒤 거리
        [SerializeField] private float navSampleRadius = 3f;   // NavMesh 위치 탐색 반경

        [Header("사거리")]
        [SerializeField] private float minRange = 0f;
        [SerializeField] private float maxRange = 12f;

        [Header("타격")]
        [SerializeField] private HitType hitType = HitType.Heavy;

        [Header("이펙트")]
        [SerializeField] private PoolItemSO vanishVfx;   // 사라지는 위치 VFX
        [SerializeField] private PoolItemSO appearVfx;   // 나타나는 위치 VFX
        [SerializeField] private EventChannelSO createChannel;

        private IAnimationTrigger _trigger;
        private IOneShotCaster _caster;
        private GameObject _target;

        public override void Initialize(ISkillModule module)
        {
            base.Initialize(module);
            _trigger = _enemy?.GetModule<IAnimationTrigger>();
            _caster  = GetComponentInChildren<IOneShotCaster>();
        }

        public bool CheckCondition(GameObject target)
        {
            if (target == null) return false;
            float dist = Vector3.Distance(_enemy.transform.position, target.transform.position);
            return dist >= minRange && dist <= maxRange;
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return !IsUsing && NormalizedCooldown >= 1f && CheckCondition(target);
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _target = target;

            // 사라지는 위치 VFX
            if (vanishVfx != null)
                createChannel?.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(
                    vanishVfx, _enemy.transform.position, _enemy.transform.rotation));

            _trigger.OnAnimationEnd += HandleVanishEnd;

            if (vanishClip != null)
                _renderer?.PlayClip(vanishClip.ParamHash, crossDuration);
            else
                HandleVanishEnd();  // vanish 애니메이션 없으면 즉시 이동
        }

        public override void StopSkill()
        {
            if (_trigger != null)
            {
                _trigger.OnAnimationEnd -= HandleVanishEnd;
                _trigger.OnAttackStart  -= HandleAttack;
                _trigger.OnAnimationEnd -= HandleAnimationEnd;
            }

            base.StopSkill();
        }

        private void HandleVanishEnd()
        {
            _trigger.OnAnimationEnd -= HandleVanishEnd;

            TeleportBehindTarget();

            _trigger.OnAttackStart  += HandleAttack;
            _trigger.OnAnimationEnd += HandleAnimationEnd;

            if (attackClip != null)
                _renderer?.PlayClip(attackClip.ParamHash, crossDuration);
        }

        private void TeleportBehindTarget()
        {
            if (_target == null) return;

            // 플레이어 뒤 위치 계산 후 NavMesh 위로 스냅
            Vector3 candidate = _target.transform.position - _target.transform.forward * teleportOffset;
            Vector3 destination = candidate;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit navHit, navSampleRadius, NavMesh.AllAreas))
                destination = navHit.position;

            // 출현 VFX (이동 후 위치)
            if (appearVfx != null)
                createChannel?.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(
                    appearVfx, destination, _enemy.transform.rotation));

            // NavMeshAgent.Warp으로 이동 (경로 초기화 포함)
            NavMeshAgent agent = _enemy.NavMovement?.NavMeshAgent;
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.Warp(destination);
            else
                _enemy.transform.position = destination;

            // 플레이어 방향을 바라봄 (Y축만)
            Vector3 look = _target.transform.position - _enemy.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
                _enemy.transform.rotation = Quaternion.LookRotation(look);
        }

        private void HandleAttack()
        {
            _trigger.OnAttackStart -= HandleAttack;

            if (_caster == null) return;
            _caster.CurrentHitType = hitType;
            _caster.SetDamageOverride(SkillData.damage);
            _caster.CastOnce();
        }

        private void HandleAnimationEnd()
        {
            _trigger.OnAnimationEnd -= HandleAnimationEnd;
            StopSkill();
        }
    }
}
