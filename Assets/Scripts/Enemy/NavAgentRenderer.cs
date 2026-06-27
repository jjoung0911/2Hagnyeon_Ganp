using Agents.Modules;
using JWLib.AnimationSystem;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy
{
    // Animator와 같은 GameObject에 부착해야 OnAnimatorMove가 호출됨
    public class NavAgentRenderer : AgentRenderer, IAfterInit
    {
        [SerializeField] private AnimParamSO speedParam;
        [SerializeField] private AnimParamSO walkParam;

        [Header("Navigation Agent Control")]
        // true: NavAgent가 위치를 직접 제어 (In Place 애니메이션)
        // false: 루트 모션이 위치를 제어 (Root Motion 애니메이션)
        [SerializeField] private bool useNavAgentPosition = true;
        [SerializeField] private bool useNavAgentRotation;
        [Tooltip("true: 루트 모션 Y 적용 / false: NavAgent Y로 고정 (기본)")]
        [SerializeField] private bool applyRootMotionY = false;

        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 360f;

        [Header("Model Forward Offset")]
        [Tooltip("모델의 forward가 +Z가 아닐 때 Y 보정값. 뒤를 보면 180, 오른쪽을 보면 -90")]
        [SerializeField] private float modelForwardOffsetY = 0f;

        private INavMovement _navMovement;
        private NavMeshAgent _navAgent;
        private Vector2 _smoothDeltaPosition;

        // true이면 애니메이션이 회전을 제어, false이면 NavMeshAgent 또는 ForceRotation이 제어
        public bool IsUpdateRotationByAnimation
        {
            get => !useNavAgentRotation;
            set
            {
                useNavAgentRotation = !value;
                if (_navAgent != null)
                    _navAgent.updateRotation = useNavAgentRotation;
            }
        }

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _navMovement = owner.GetModule<INavMovement>();
            Debug.Assert(_navMovement != null, "NavMovement 모듈이 존재하지 않습니다.");
        }

        public void AfterInit()
        {
            _navAgent = _navMovement.NavMeshAgent;
            _navAgent.updatePosition = useNavAgentPosition;
            _navAgent.updateRotation = useNavAgentRotation;
        }

        private void OnAnimatorMove()
        {
            if (_navAgent == null) return;

            if (!useNavAgentPosition)
            {
                // 루트 모션 모드: 애니메이션 루트 포지션을 NavMesh 위에 적용
                Vector3 rootPosition = Animator.rootPosition;
                if (!applyRootMotionY)
                    rootPosition.y = _navAgent.nextPosition.y;

                if (NavMesh.SamplePosition(rootPosition, out NavMeshHit hit, 0.4f, NavMesh.AllAreas))
                {
                    _owner.transform.position = rootPosition;
                    _navAgent.nextPosition = hit.position;
                }
            }

            if (IsUpdateRotationByAnimation)
                _owner.transform.rotation = Animator.rootRotation * Quaternion.Euler(0f, -modelForwardOffsetY, 0f);
        }

        private void Update()
        {
            SynchronizeSpeedParam();
            RotationControl();
        }

        private void SynchronizeSpeedParam()
        {
            if (_navMovement == null || speedParam == null) return;
            if (!_navAgent.isActiveAndEnabled || !_navAgent.isOnNavMesh) return;

            float speed;
            if (useNavAgentPosition)
            {
                // NavAgent가 위치를 제어하므로 NavAgent velocity를 직접 사용
                speed = _navAgent.speed > 0f
                    ? _navAgent.velocity.magnitude / _navAgent.speed
                    : 0f;

                if (_navAgent.remainingDistance <= _navAgent.stoppingDistance)
                    speed = Mathf.Lerp(0f, speed,
                        _navAgent.stoppingDistance > 0f
                            ? _navAgent.remainingDistance / _navAgent.stoppingDistance
                            : 0f);
            }
            else
            {
                // 루트 모션 모드: NavAgent nextPosition과 실제 위치 델타로 계산
                Vector3 worldDelta = _navAgent.nextPosition - _owner.transform.position;
                worldDelta.y = 0f;

                float dx = Vector3.Dot(_owner.transform.right, worldDelta);
                float dy = Vector3.Dot(_owner.transform.forward, worldDelta);
                float smooth = Mathf.Min(1f, Time.deltaTime / 0.1f);

                _smoothDeltaPosition = Vector2.Lerp(_smoothDeltaPosition, new Vector2(dx, dy), smooth);
                Vector2 velocity = _smoothDeltaPosition / Time.deltaTime;

                if (_navAgent.remainingDistance <= _navAgent.stoppingDistance)
                    velocity = Vector2.Lerp(Vector2.zero, velocity,
                        _navAgent.stoppingDistance > 0f
                            ? _navAgent.remainingDistance / _navAgent.stoppingDistance
                            : 0f);

                speed = velocity.magnitude;

                // 모델이 NavAgent 위치와 벌어지면 보간으로 복귀
                if (worldDelta.magnitude > _navAgent.radius * 0.5f)
                {
                    _owner.transform.position = Vector3.Lerp(
                        Animator.rootPosition, _navAgent.nextPosition, smooth);
                }
            }

            Animator.SetFloat(speedParam.ParamHash, speed);
        }

        private void RotationControl()
        {
            if (_navAgent == null || useNavAgentRotation) return;
            if (_navMovement.IsArrived) return;

            Vector3 desiredDirection = _navAgent.steeringTarget - _owner.transform.position;
            if (desiredDirection.sqrMagnitude < 0.01f) return;

            desiredDirection.y = 0f;
            Quaternion target = Quaternion.LookRotation(desiredDirection, Vector3.up)
                * Quaternion.Euler(0f, modelForwardOffsetY, 0f);
            _owner.transform.rotation = Quaternion.RotateTowards(
                _owner.transform.rotation, target, rotationSpeed * Time.deltaTime);
        }
    }
}
