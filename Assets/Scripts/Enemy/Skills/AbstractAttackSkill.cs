using Agents.CombatSystem;
using Agents.Modules;
using JWLib.AnimationSystem;
using UnityEngine;

namespace Enemy.Skills
{
    public abstract class AbstractAttackSkill : AbstractEnemySkill
    {
        [SerializeField] private AnimParamSO attackClip;
        [SerializeField] private float crossDuration = 0.1f;

        private IAnimationTrigger _trigger;
        private IOneShotCaster _caster;

        public override void Initialize(ISkillModule module)
        {
            base.Initialize(module);
            _trigger = _enemy?.GetModule<IAnimationTrigger>();
            _caster = GetComponentInChildren<IOneShotCaster>();
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);

            if (_trigger != null)
            {
                _trigger.OnAttackStart += HandleAttack;
                _trigger.OnAnimationEnd += HandleAnimationEnd;
            }

            if (attackClip != null)
                _renderer?.PlayClip(attackClip.ParamHash, crossDuration);
        }

        public override void StopSkill()
        {
            if (_trigger != null)
            {
                _trigger.OnAttackStart -= HandleAttack;
                _trigger.OnAnimationEnd -= HandleAnimationEnd;
            }

            base.StopSkill();
        }

        private void HandleAttack()
        {
            if (_caster == null) return;
            _caster.CurrentHitType = GetHitType();
            _caster.SetDamageOverride(SkillData.damage);
            _caster.CastOnce();
        }

        // 서브클래스에서 타수별 HitType 반환. 기본값 Light
        protected virtual HitType GetHitType() => HitType.Light;

        private void HandleAnimationEnd() => StopSkill();
    }
}
