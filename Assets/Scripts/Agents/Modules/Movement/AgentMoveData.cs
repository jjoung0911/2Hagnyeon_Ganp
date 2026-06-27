using System;
using System.StatSystem;
using UnityEngine;

namespace Agents.Modules.Movement
{
    public class AgentMoveData : MonoBehaviour, IModule, IAfterInit, IMoveData
    {
        private float _gravity = -40;
        public Vector3 MoveVelocity => _finalVelocity;
        public Vector3 NormalizedMoveDir => _finalVelocity.normalized;
        public Vector3 TargetMoveDir => _targetMoveDir;
        public float VerticalVelocity => _verticalVelocity;
        public bool CanManualMove { get; set; } = true;
        public bool IsRootMotionActive { get; set; }
        public bool IsGrounded => _isGround;
        public float SpeedMultiplier { get; set; } = 1f;
        
        public event Action<bool> OnGroundChanged;
        public event Action<bool> OnRunStateChanged;

        [SerializeField] private StatSO walkSpeedStatSo;
        [SerializeField] private float accelerationSpeed = 8f;
        [SerializeField] private float terminalVelocity = 25f;
        [SerializeField] private float groundCheckDistance = 0.15f;
        [SerializeField] private LayerMask groundMask = -1;

        private Vector3 _moveVelocity; // 이동 속도 (보간된 방향)
        private Vector3 _targetMoveDir; // 목표 방향 (입력값)
        private Vector3 _forcedVelocity; // 외부 힘
        private Vector3 _finalVelocity; // 최종 속도
        private float _verticalVelocity; // 수직 속도

        private bool _isGround;
        protected float _walkSpeed; // 이동 속력

        protected AgentStat _stat;
        protected Agent _owner;
        protected CharacterController _controller;

        public virtual void Initialize(ModuleOwner owner)
        {
            _owner = owner as Agent;
            _stat = _owner.GetModule<AgentStat>();
            _controller = _owner.GetComponent<CharacterController>();
        }
        public virtual void AfterInit()
        {
            _stat.SubscribeStat(walkSpeedStatSo.Index, HandleWalkSpeedChange, 1);
            _walkSpeed = _stat.GetStat(walkSpeedStatSo.Index).Value;
        }

        private void Update()
        {
            _moveVelocity = Vector3.Lerp(_moveVelocity, _targetMoveDir, accelerationSpeed * Time.deltaTime);
            UpdateGravity();
            _finalVelocity = CalculateFinalVelocity();
            CheckGround();
        }

        public void SetMoveDir(Vector3 dir) => _targetMoveDir = dir.normalized;
        public void AddForce(Vector3 force) => _forcedVelocity += force;
        public void SetForcedVelocity(Vector3 force) => _forcedVelocity = force;
        public void SetVerticalVelocity(float verticalValue) => _verticalVelocity = verticalValue;
        public void StopImmediately(bool stopMove, bool stopForce, bool stopVertical)
        {
            if (stopMove) { _moveVelocity = Vector3.zero; _targetMoveDir = Vector3.zero; }
            if (stopForce) _forcedVelocity = Vector3.zero;
            if (stopVertical) _verticalVelocity = 0;
        }
        public void SetGravity(float gravity) => _gravity = gravity;
        public void Warp(Vector3 position)
        {
            _controller.enabled = false;
            _owner.transform.position = position;
            _controller.enabled = true;
        }
        
        private void CheckGround()
        {
            bool isGround = _controller.isGrounded || IsNearGround();
            if (isGround != _isGround)
            {
                OnGroundChanged?.Invoke(isGround);
                _isGround = isGround;
            }
        }

        // CharacterController.isGrounded가 엣지에서 false를 반환할 때의 보조 감지
        private bool IsNearGround()
        {
            float radius = _controller.radius * 0.9f;
            Vector3 origin = _owner.transform.position + Vector3.up * (_controller.radius + 0.02f);
            if (!Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit,
                groundCheckDistance + 0.02f, groundMask, QueryTriggerInteraction.Ignore))
                return false;

            // CharacterController나 Rigidbody가 있는 동적 오브젝트(적, 파편 등)는 지면으로 인식하지 않음
            if (hit.collider.TryGetComponent<CharacterController>(out _)) return false;
            if (hit.collider.attachedRigidbody != null) return false;
            return true;
        }

        public virtual float GetMoveSpeed() => _walkSpeed; // 이동 속도를 가져옴,
                                                           // 함수로 하는 이유는 다른 속도를 가진 스크립트는 재정의해서 사용
        
        private Vector3 CalculateFinalVelocity()
        {
            float realSpeed = CanManualMove ? GetMoveSpeed() * SpeedMultiplier : 0f;
            Vector3 horizontalMovement = _moveVelocity * realSpeed;
            Vector3 vertical  = Vector3.up * _verticalVelocity;

            return horizontalMovement + vertical + _forcedVelocity;
        }
        private void UpdateGravity()
        {
            if (_isGround && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity = Mathf.Max(_verticalVelocity + _gravity * Time.deltaTime, -terminalVelocity);
        }
        private void HandleWalkSpeedChange(StatSO stat, float current, float previous) => _walkSpeed = current;

        protected virtual void OnDestroy()
        {
            _stat.UnSubscribeStat(walkSpeedStatSo.Index, HandleWalkSpeedChange);
        }

    }
}