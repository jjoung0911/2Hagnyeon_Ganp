using Agents.CombatSystem;
using Agents.Modules;
using Agents.Modules.Movement;
using JWLib.AnimationSystem;
using Player.Skills;
using UnityEngine;

namespace Player
{
    public class PlayerSlideModule : AbstractPlayerSkill
    {
        [SerializeField] private AnimParamSO slideParam;

        private IAnimationTrigger _trigger;

        public override void Initialize(ISkillModule module)
        {
            base.Initialize(module);
            _trigger = _player.GetModule<IAnimationTrigger>();
        }

        public override bool CanUseSkill(GameObject target = null)
            => NormalizedCooldown >= 1f && !IsUsing;

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            Vector3 dir = _moveData.TargetMoveDir.magnitude > 0.1f
                ? _moveData.TargetMoveDir.normalized
                : _player.transform.forward;

            _moveData.CanManualMove = false;
            _rootMotion.Begin(dir);
            _renderer.PlayClip(slideParam.ParamHash, 0.05f, 0.05f, 1);
            _trigger.OnAnimationEnd += HandleFinished;
        }

        private void HandleFinished() => StopSkill();

        public override void StopSkill()
        {
            _moveData.CanManualMove = true;
            _trigger.OnAnimationEnd -= HandleFinished;
            _rootMotion.End();
            base.StopSkill();
        }
    }
}