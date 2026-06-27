using System;
using Enemy;
using Unity.Behavior;
using UnityEngine;

namespace Enemy.BT.Conditions
{
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "IsTargetTooFar", story: "[Enemy] check [TargetGameObject] too far", category: "Conditions", id: "c1d2e3f4a5b6c7d8e9f0a1b2c3d4e5f6")]
    public partial class IsTargetTooFarCondition : Condition
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
            return targetDistance > Enemy.Value.PreferredMaxRange;
        }
    }
}
