using System;
using Enemy;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Random = UnityEngine.Random;

namespace _00.Scripts.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "RandomMove", story: "[Enemy] move in [Radius]", category: "Action/Movement", id: "d3e4f5a6b7c8d9e0f1a2b3c4d5e6f7a8")]
    public partial class RandomMoveAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<float> Radius;

        private INavMovement _navMovement;

        protected override Status OnStart()
        {
            var enemy = Enemy?.Value;
            if (enemy?.NavMovement == null) return Status.Failure;

            _navMovement = enemy.NavMovement;

            float radius = Radius?.Value ?? 5f;
            Vector3 randomOffset = Random.insideUnitSphere * radius;
            randomOffset.y = 0f;
            Vector3 dest = enemy.transform.position + randomOffset;

            if (NavMesh.SamplePosition(dest, out NavMeshHit hit, radius, NavMesh.AllAreas))
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
