using System;
using UnityEngine;

namespace Agents.Modules.Movement
{
    public class AgentMover : MonoBehaviour, IModule, IAgentMover
    {
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float moveThreshold = 0.1f;

        private CharacterController _controller;
        private IMoveData _moveData;
        private Agent _owner;

        public event Action<Vector2> OnVelocityChanged;
        public event Action<bool> OnMovingStateChanged;

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner as Agent;
            _controller = GetComponentInParent<CharacterController>();
            _moveData = _owner.GetModule<AgentMoveData>();
        }

        private bool _isMoving;

        private void FixedUpdate()
        {
            if (_moveData.IsRootMotionActive) return;
            RotateAgent();
            MoveCharacter();
        }

        private void RotateAgent()
        {
            Vector3 dir = new Vector3(_moveData.NormalizedMoveDir.x, 0, _moveData.NormalizedMoveDir.z);
            if (dir.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                _owner.transform.rotation =
                    Quaternion.Slerp(_owner.transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }

        private void MoveCharacter()
        {
            _controller.Move(_moveData.MoveVelocity * Time.deltaTime);

            Vector3 vel = _controller.velocity;
            Vector2 horizontalVel = new Vector2(vel.x, vel.z);
            OnVelocityChanged?.Invoke(horizontalVel);

            bool moving = _moveData.IsGrounded && _moveData.TargetMoveDir.magnitude > moveThreshold;
            if (moving != _isMoving)
            {
                _isMoving = moving;
                OnMovingStateChanged?.Invoke(moving);
            }
        }
    }
}