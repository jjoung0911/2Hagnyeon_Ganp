using System;
using Agents.Modules;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ProcessHitAction", story: "[Enemy] process hit from [TargetGameObject]", category: "Action/Combat", id: "c8d9e0f1a2b3c4d5e6f7a8b9c0d1e2f3")]
    public partial class ProcessHitAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;
        [SerializeReference] public BlackboardVariable<float> RotateSpeed;

        private IHitHandler _hitHandler;
        private NavAgentRenderer _renderer;
        private IAnimationTrigger _trigger;
        private bool _animationEnded;
        private bool _originalUpdateRotationByAnimation;

        protected override Status OnStart()
        {
            if (_hitHandler == null && Enemy?.Value != null)
            {
                _hitHandler = Enemy.Value.GetModule<IHitHandler>();
                _renderer = Enemy.Value.GetModule<NavAgentRenderer>();
                _trigger = Enemy.Value.GetModule<IAnimationTrigger>();
            }

            if (_hitHandler == null || !_hitHandler.IsStaggered)
                return Status.Success;

            _animationEnded = false;
            if (_trigger != null)
                _trigger.OnAnimationEnd += HandleAnimationEnd;

            if (_renderer != null)
            {
                _originalUpdateRotationByAnimation = _renderer.IsUpdateRotationByAnimation;
                _renderer.IsUpdateRotationByAnimation = false;
            }
            RotateTowardTarget();

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            // 애니메이션 이벤트가 발화됐거나, 경직 코루틴이 먼저 끝난 경우 모두 완료 처리
            if (_animationEnded || (_hitHandler != null && !_hitHandler.IsStaggered))
                return Status.Success;
            return Status.Running;
        }

        protected override void OnEnd()
        {
            if (_trigger != null)
                _trigger.OnAnimationEnd -= HandleAnimationEnd;
            if (_renderer != null)
                _renderer.IsUpdateRotationByAnimation = _originalUpdateRotationByAnimation;
        }

        private void HandleAnimationEnd() => _animationEnded = true;

        private void RotateTowardTarget()
        {
            var enemy = Enemy?.Value;
            var target = TargetGameObject?.Value;
            if (enemy == null || target == null) return;

            Vector3 dir = target.transform.position - enemy.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;

            float speed = RotateSpeed?.Value ?? 360f;
            enemy.transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
