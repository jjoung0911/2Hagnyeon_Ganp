using System;
using Enemy;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace _00.Scripts.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ChaseToTargetAction", story: "[Enemy] chase to [TargetGameObject]", category: "Action/Movement", id: "e4f5a6b7c8d9e0f1a2b3c4d5e6f7a8b9")]
    public partial class ChaseToTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        // 이 거리 이상 멀어지면 타겟을 잃은 것으로 판단하고 Failure 반환 (0 = 무제한)
        [SerializeField] private float loseTargetDistance = 30f;
        // NavMesh 경로 실패가 이 시간 이상 지속되면 Failure 반환
        [SerializeField] private float pathFailTimeout = 2f;

        private Vector3 _destination;
        private INavMovement _navMovement;
        private float _pathFailTimer;

        protected override Status OnStart()
        {
            if(Enemy.Value == null || TargetGameObject.Value == null || Enemy.Value.NavMovement == null)
                return Status.Failure;

            _destination = TargetGameObject.Value.transform.position;
            _navMovement = Enemy.Value.NavMovement;
            _navMovement.SetDestination(_destination);
            _pathFailTimer = 0f;

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (TargetGameObject.Value == null)
                return Status.Failure;

            Vector3 enemyPos = Enemy.Value.transform.position;
            Vector3 targetPos = TargetGameObject.Value.transform.position;

            // 타겟이 너무 멀어졌으면 추적 포기
            if (loseTargetDistance > 0f && Vector3.Distance(enemyPos, targetPos) > loseTargetDistance)
                return Status.Failure;

            // NavMesh 경로 상태 감시 — 경로 실패가 일정 시간 지속되면 포기
            var agent = _navMovement is NavMovement nav ? nav.NavMeshAgent : null;
            if (agent != null && !agent.pathPending &&
                agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
            {
                _pathFailTimer += Time.deltaTime;
                if (_pathFailTimer >= pathFailTimeout)
                    return Status.Failure;
            }
            else
            {
                _pathFailTimer = 0f;
            }

            Vector3 newDestination = targetPos;
            if (Vector3.Distance(_destination, newDestination) > 0.5f)
            {
                _destination = newDestination;
                _navMovement.SetDestination(_destination);
            }

            if(_navMovement.IsArrived)
                return Status.Success;

            return Status.Running;
        }
    }
}
