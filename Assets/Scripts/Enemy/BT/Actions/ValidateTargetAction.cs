using System;
using Agents;
using Enemy;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace _00.Scripts.Enemy.BT.Actions
{
    // COMBAT 루프 진입 시 타겟이 아직 유효한지(살아있고 범위 내에 있는지) 검사한다.
    // Failure 반환 시 BT가 타겟을 초기화하고 IDLE로 복귀하도록 유도한다.
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ValidateTargetAction", story: "[Enemy] validate [TargetGameObject] within [MaxRange]",
        category: "Action/Combat", id: "b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7")]
    public partial class ValidateTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        // 이 거리 이상이면 타겟을 잃은 것으로 판단 (0 = 무제한)
        [SerializeReference] public BlackboardVariable<float> MaxRange;

        protected override Status OnStart()
        {
            var target = TargetGameObject?.Value;
            var enemy = Enemy?.Value;

            if (target == null || enemy == null)
            {
                ClearTarget();
                return Status.Failure;
            }

            // Agent가 이미 사망했으면 타겟 해제
            var agent = target.GetComponent<Agent>();
            if (agent != null && agent.IsDead)
            {
                ClearTarget();
                return Status.Failure;
            }

            float maxRange = MaxRange?.Value ?? 0f;
            if (maxRange > 0f)
            {
                float dist = Vector3.Distance(enemy.transform.position, target.transform.position);
                if (dist > maxRange)
                {
                    ClearTarget();
                    return Status.Failure;
                }
            }

            return Status.Success;
        }

        protected override Status OnUpdate() => Status.Success;
        protected override void OnEnd() { }

        private void ClearTarget()
        {
            if (TargetGameObject != null)
                TargetGameObject.Value = null;
        }
    }
}
