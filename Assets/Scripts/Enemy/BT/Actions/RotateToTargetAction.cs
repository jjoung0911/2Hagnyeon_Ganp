using System;
using Enemy;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace _00.Scripts.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "RotateToTargetAction", story: "[Enemy] rotate to [TargetGameObject]", category: "Action/Combat", id: "f5a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0")]
    public partial class RotateToTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;
        [SerializeReference] public BlackboardVariable<float> RotateSpeed;
        [SerializeReference] public BlackboardVariable<float> RotateDuration;

        private Transform _enemyTransform;
        private Transform _targetTransform;
        private float _elapsed;

        protected override Status OnStart()
        {
            var enemy = Enemy?.Value;
            var target = TargetGameObject?.Value;
            if (enemy == null || target == null) return Status.Failure;

            _enemyTransform = enemy.transform;
            _targetTransform = target.transform;
            _elapsed = 0f;
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            _elapsed += Time.deltaTime;

            Vector3 dir = _targetTransform.position - _enemyTransform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                float speed = RotateSpeed?.Value ?? 360f;
                _enemyTransform.rotation = Quaternion.RotateTowards(
                    _enemyTransform.rotation, Quaternion.LookRotation(dir), speed * Time.deltaTime);
            }

            float duration = RotateDuration?.Value ?? 0.5f;
            return _elapsed >= duration ? Status.Success : Status.Running;
        }

        protected override void OnEnd() { }
    }
}
