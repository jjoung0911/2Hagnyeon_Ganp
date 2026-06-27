using System;
using Enemy;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace _00.Scripts.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "FindTargetAction", story: "[Enemy] Find [TargetGameObject]", category: "Combat", id: "a4b6cf5470d89114363bf7508e379436")]
    public partial class FindTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        [SerializeField] private float searchRadius = 10f;
        // true로 설정하면 searchRadius 내 없을 때 씬 전체로 폴백 탐색
        [SerializeField] private bool fallbackToGlobal = true;

        protected override Status OnStart()
        {
            var enemy = Enemy?.Value;
            if (enemy?.Sensor == null) return Status.Failure;

            Collider closest = enemy.Sensor.FindClosestTargetInRadius(searchRadius);

            if (closest == null && fallbackToGlobal)
                closest = enemy.Sensor.FindClosestTarget();

            if (closest == null) return Status.Failure;

            if (TargetGameObject != null)
                TargetGameObject.Value = closest.gameObject;

            return Status.Success;
        }

        protected override Status OnUpdate() => Status.Success;

        protected override void OnEnd() { }
    }
}
