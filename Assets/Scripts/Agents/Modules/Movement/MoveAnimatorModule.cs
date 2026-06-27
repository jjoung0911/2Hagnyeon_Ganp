using Agents.Modules;
using JWLib.AnimationSystem;
using UnityEngine;

namespace Agents.Modules.Movement
{
    public class MoveAnimatorModule : MonoBehaviour, IModule, IAfterInit
    {
        [SerializeField] private AnimParamSO inputXParam;
        [SerializeField] private AnimParamSO inputYParam;
        [SerializeField] private AnimParamSO isMovingParam;
        [SerializeField] private float inputDampSpeed = 8f;

        private IRenderer _renderer;
        private IMoveData _moveData;
        private AgentMover _mover;
        private Transform _ownerTransform;

        private Vector2 _smoothedInputDir;
        private Vector2 _lastMoveDir;
        private bool _isMoving;

        public void Initialize(ModuleOwner owner)
        {
            _renderer = owner.GetModule<IRenderer>();
            _moveData = owner.GetModule<AgentMoveData>();
            _mover = owner.GetModule<AgentMover>();
            _ownerTransform = owner.transform;
        }

        public void AfterInit()
        {
            _mover.OnMovingStateChanged += HandleMovingStateChanged;
        }

        private void OnDestroy()
        {
            _mover.OnMovingStateChanged -= HandleMovingStateChanged;
        }

        private void HandleMovingStateChanged(bool isMoving)
        {
            _isMoving = isMoving;

            if (isMoving)
                _smoothedInputDir = ToLocalInputDir(_moveData.TargetMoveDir);
        }

        private Vector2 ToLocalInputDir(Vector3 worldDir)
        {
            Vector3 local = _ownerTransform.InverseTransformDirection(new Vector3(worldDir.x, 0f, worldDir.z).normalized);
            return new Vector2(local.x, local.z);
        }

        private void Update()
        {
            Vector2 targetDir = _isMoving
                ? ToLocalInputDir(_moveData.TargetMoveDir)
                : _lastMoveDir;

            if (_isMoving)
                _lastMoveDir = targetDir;

            _smoothedInputDir = Vector2.Lerp(_smoothedInputDir, targetDir, inputDampSpeed * Time.deltaTime);

            if (inputXParam != null && inputYParam != null)
                _renderer?.SetVector2(inputXParam, inputYParam, _smoothedInputDir);
        }
    }
}