using System;
using Enemy;
using Unity.Behavior;
using UnityEngine;

namespace Enemy.BT.Conditions
{
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "IsTargetTooClose", story: "[Enemy] check [TargetGameObject] too close", category: "Conditions", id: "b1c2d3e4f5a6b7c8d9e0f1a2b3c4d5e6")]
    public partial class IsTargetTooCloseCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<SkeletonArcher> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        public override bool IsTrue()
        {
            if (Enemy.Value == null || TargetGameObject.Value == null)
            {
                Debug.LogError("condition에 Enemy 또는 TargetGameObject가 할당되지 않았습니다.");
                return false;
            }

            float targetDistance = Vector3.Distance(Enemy.Value.transform.position, TargetGameObject.Value.transform.position);
            return targetDistance < Enemy.Value.PreferredMinRange;
        }
    }
}
