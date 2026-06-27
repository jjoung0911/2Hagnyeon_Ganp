using System;
using Enemy;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

namespace _00.Scripts.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "RetreatFromTargetAction", story: "[Enemy] retreat from [TargetGameObject]", category: "Action/Movement", id: "f1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6")]
    public partial class RetreatFromTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<SkeletonArcher> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        private INavMovement _navMovement;

        protected override Status OnStart()
        {
            var enemy = Enemy?.Value;
            if (enemy == null || TargetGameObject?.Value == null || enemy.NavMovement == null)
                return Status.Failure;

            _navMovement = enemy.NavMovement;

            Vector3 away = enemy.transform.position - TargetGameObject.Value.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
                away = -enemy.transform.forward;
            away.Normalize();

            Vector3 dest = enemy.transform.position + away * enemy.RetreatStep;

            if (NavMesh.SamplePosition(dest, out NavMeshHit hit, enemy.RetreatStep, NavMesh.AllAreas))
                _navMovement.SetDestination(hit.position);
            else
                _navMovement.SetDestination(enemy.transform.position);

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (_navMovement == null) return Status.Failure;
            return _navMovement.IsArrived ? Status.Success : Status.Running;
        }

        protected override void OnEnd()
        {
            _navMovement?.StopImmediately();
        }
    }
}
