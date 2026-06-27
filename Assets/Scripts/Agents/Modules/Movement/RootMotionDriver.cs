using Agents.Modules;
using UnityEngine;

namespace Agents.Modules.Movement
{
    public class RootMotionDriver : MonoBehaviour, IModule, IRootMotionDriver
    {
        private Animator _animator;
        private CharacterController _controller;
        private IMoveData _moveData;

        private Vector3 _worldDir;
        private float _scale = 1f;

        public void Initialize(ModuleOwner owner)
        {
            _animator = GetComponent<Animator>();
            _controller = owner.GetComponent<CharacterController>();
            _moveData = owner.GetModule<AgentMoveData>();
        }

        public void Begin(Vector3 worldDir, float scale = 1f)
        {
            _worldDir = worldDir.normalized;
            _scale = scale;
            _moveData.IsRootMotionActive = true;
            _animator.applyRootMotion = true;
        }

        public void End()
        {
            _moveData.IsRootMotionActive = false;
            _animator.applyRootMotion = false;
        }

        private void OnAnimatorMove()
        {
            if (!_moveData.IsRootMotionActive) return;

            float horizontalMag = new Vector3(
                _animator.deltaPosition.x, 0f, _animator.deltaPosition.z).magnitude;

            Vector3 move = _worldDir * (horizontalMag * _scale)
                           + Vector3.up * (_moveData.VerticalVelocity * Time.deltaTime);

            _controller.Move(move);
        }
    }
}