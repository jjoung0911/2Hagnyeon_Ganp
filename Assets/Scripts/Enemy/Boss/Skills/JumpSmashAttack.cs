using Agents.CombatSystem;
using Agents.Modules;
using Enemy.Skills;
using JWLib.AnimationSystem;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using UnityEngine;

namespace Enemy.Boss.Skills
{
    // 패턴 4: 도약 강하 AoE.
    // 점프 애니메이션 동안 타겟 위치로 이동한 뒤, 착지(OnAttackStart) 시점에
    // 원형 충격 VFX/데칼 + 카메라 쉐이크 + 범위 판정(IOneShotCaster, Heavy)을 수행한다.
    public class JumpSmashAttack : AbstractEnemySkill, ISkillCondition
    {
        [Header("애니메이션")]
        [SerializeField] private AnimParamSO jumpClip;
        [SerializeField] private float crossDuration = 0.1f;

        [Header("사거리")]
        [SerializeField] private float minRange = 4f;
        [SerializeField] private float maxRange = 12f;

        [Header("착지 연출")]
        [SerializeField] private AttackTelegraphView telegraphPrefab;
        [SerializeField] private float telegraphRadius = 3f;
        [SerializeField] private float telegraphDuration = 0.4f;
        [SerializeField] private PoolItemSO landingVfx;
        [SerializeField] private EventChannelSO createChannel;
        [SerializeField] private EventChannelSO bossEventChannel;
        [SerializeField] private float landingShakeForce = 0.8f;

        private BossPatternController _patternController;
        private INavMovement _nav;
        private IAnimationTrigger _trigger;
        private IOneShotCaster _caster;

        public override void Initialize(ISkillModule module)
        {
            base.Initialize(module);
            _patternController = _enemy?.GetModule<BossPatternController>();
            _nav = _enemy?.GetModule<INavMovement>();
            _trigger = _enemy?.GetModule<IAnimationTrigger>();
            _caster = GetComponentInChildren<IOneShotCaster>();

            if (_caster == null)
                Debug.LogWarning($"[JumpSmashAttack] IOneShotCaster 자식 컴포넌트를 찾지 못했습니다 — 착지 피해 판정이 발생하지 않습니다: {name}");
        }

        public bool CheckCondition(GameObject target)
        {
            if (target == null) return false;
            float dist = Vector3.Distance(_enemy.transform.position, target.transform.position);
            return dist >= minRange && dist <= maxRange
                   && (_patternController == null || _patternController.CanUsePattern(BossPatternType.JumpSmash));
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return !IsUsing && NormalizedCooldown >= 1f && CheckCondition(target);
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _patternController?.RecordPatternUsed(BossPatternType.JumpSmash);
            _nav?.SetDestination(target.transform.position);

            if (_trigger != null)
            {
                _trigger.OnAttackStart += HandleAttackStart;
                _trigger.OnAnimationEnd += HandleAnimationEnd;
            }

            if (jumpClip != null)
                _renderer?.PlayClip(jumpClip.ParamHash, crossDuration);
        }

        public override void StopSkill()
        {
            if (_trigger != null)
            {
                _trigger.OnAttackStart -= HandleAttackStart;
                _trigger.OnAnimationEnd -= HandleAnimationEnd;
            }

            base.StopSkill();
        }

        private void HandleAttackStart()
        {
            if (telegraphPrefab != null)
            {
                var telegraph = Instantiate(telegraphPrefab, _enemy.transform.position, Quaternion.identity);
                telegraph.SetAngle(360f);
                telegraph.Show(telegraphRadius, telegraphDuration);
            }

            if (landingVfx != null)
                createChannel?.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(
                    landingVfx, _enemy.transform.position, Quaternion.identity));

            bossEventChannel?.RaiseEvent(BossEvents.CameraShakeRequest.Init(landingShakeForce));

            if (_caster == null) return;
            _caster.CurrentHitType = HitType.Heavy;
            _caster.SetDamageOverride(SkillData.damage);
            _caster.CastOnce();
        }

        private void HandleAnimationEnd() => StopSkill();
    }
}
