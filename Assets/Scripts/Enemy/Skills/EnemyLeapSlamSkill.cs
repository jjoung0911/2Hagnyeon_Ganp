using Agents.CombatSystem;
using Agents.Modules;
using JWLib.AnimationSystem;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using UnityEngine;

namespace Enemy.Skills
{
    // 슬램 스킬 — 애니메이션 재생 후 AttackStart(착지) 순간 CastOnce() 1회 판정
    // EnemySlamDamageCaster와 같은 GameObject에 부착
    public class EnemyLeapSlamSkill : AbstractEnemySkill, ISkillCondition
    {
        [SerializeField] private AnimParamSO slamClip;
        [SerializeField] private float crossDuration = 0.1f;
        [SerializeField] private PoolItemSO landingVFX;
        [SerializeField] private EventChannelSO createChannel;
        [SerializeField] private float minRange = 3f;
        [SerializeField] private float maxRange = 6f;

        private INavMovement _nav;
        private IAnimationTrigger _trigger;
        private IOneShotCaster _caster;

        public override void Initialize(ISkillModule module)
        {
            base.Initialize(module);
            _nav = _enemy?.GetModule<INavMovement>();
            _trigger = _enemy?.GetModule<IAnimationTrigger>();
            _caster = GetComponentInChildren<IOneShotCaster>();
        }
        
        public bool CheckCondition(GameObject target)
        {
            if (target == null) return false;
            float distance = Vector3.Distance(_enemy.transform.position, target.transform.position);
            return distance >= minRange && distance <= maxRange;
        }
        
        public override bool CanUseSkill(GameObject target = null)
        {
            return !IsUsing && NormalizedCooldown >= 1f && CheckCondition(target);
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _nav.SetDestination(target.transform.position);
            if (_trigger != null)
            {
                _trigger.OnAttackStart += HandleAttackStart;
                _trigger.OnAnimationEnd += HandleAnimationEnd;
            }

            if (slamClip != null)
                _renderer?.PlayClip(slamClip.ParamHash, crossDuration);
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
            createChannel?.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(landingVFX, transform.position, Quaternion.identity));
            if (_caster == null) return;
            _caster.CurrentHitType = HitType.Heavy;
            _caster.CastOnce();
        }

        private void HandleAnimationEnd() => StopSkill();
    }
}
