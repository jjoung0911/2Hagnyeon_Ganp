using System;
using Unity.Behavior;
using UnityEngine;

namespace Enemy.BT.Conditions
{
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "TargetInStopDistance", story: "[Enemy] check [TargetGameObject] in stopDistance", category: "Conditions", id: "431e2d45cc826144b0667943ed06ed7f")]
    public partial class TargetInStopDistanceCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        public override bool IsTrue()
        {
            if (Enemy.Value == null || TargetGameObject.Value == null)
            {
                Debug.LogError("condition에 Enemy 또는 TargetGameObject가 할당되지 않았습니다.");
                return false;
            }
            
            float stopDistance = Enemy.Value.StopDistance;
            float targetDistance = Vector3.Distance(Enemy.Value.transform.position, TargetGameObject.Value.transform.position);
            
            return targetDistance <= stopDistance;
        }
    }
}
