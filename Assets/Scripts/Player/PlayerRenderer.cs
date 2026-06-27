using Agents.Modules;
using Agents.Modules.Movement;
using JWLib.AnimationSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    public class PlayerRenderer : AgentRenderer, IAfterInit
    {
        [FormerlySerializedAs("_speedParam")]
        [SerializeField] private AnimParamSO speedParam;
        [FormerlySerializedAs("_animDampTime")]
        [SerializeField] private float animDampTime = 0.1f;

        private IAgentMover _mover;
        private PlayerMoveData _moveData;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _mover = owner.GetModule<IAgentMover>();
            _moveData = owner.GetModule<PlayerMoveData>();
        }

        public void AfterInit()
        {
            _mover.OnVelocityChanged += HandleVelocityChanged;
        }

        private void OnDestroy()
        {
            if (_mover != null) _mover.OnVelocityChanged -= HandleVelocityChanged;
        }

        private void HandleVelocityChanged(Vector2 velocity)
        {
            if (speedParam != null)
            {
                float speedValue = Mathf.Clamp01(velocity.magnitude / _moveData.MaxRunSpeed);
                _animator.SetFloat(speedParam.ParamHash,speedValue , animDampTime, Time.deltaTime);
            }
        }
    }
}
