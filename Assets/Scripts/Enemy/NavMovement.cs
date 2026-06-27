using Agents.Modules;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy
{
    public class NavMovement : MonoBehaviour, IModule, INavMovement
    {
        public NavMeshAgent NavMeshAgent { get; private set; }

        public Vector3 Velocity
        {
            get => NavMeshAgent != null ? NavMeshAgent.velocity : Vector3.zero;
            set
            {
                if (NavMeshAgent != null)
                    NavMeshAgent.velocity = value;
            }
        }

        public float Speed
        {
            get => NavMeshAgent != null ? NavMeshAgent.speed : 0f;
            set
            {
                if (NavMeshAgent != null)
                    NavMeshAgent.speed = value;
            }
        }

        public bool IsStopped
        {
            get => NavMeshAgent != null && NavMeshAgent.isStopped;
            set
            {
                if (NavMeshAgent != null)
                    NavMeshAgent.isStopped = value;
            }
        }

        public bool IsArrived
        {
            get
            {
                if (NavMeshAgent == null || !NavMeshAgent.isActiveAndEnabled) return false;
                if (NavMeshAgent.pathPending) return false;
                // 경로가 없으면 도착이 아닌 미설정 상태 — false 반환
                if (!NavMeshAgent.hasPath) return false;

                return NavMeshAgent.remainingDistance <= NavMeshAgent.stoppingDistance * 0.5f
                    && NavMeshAgent.velocity.sqrMagnitude <= 0.04f;
            }
        }

        public void SetDestination(Vector3 destination)
        {
            if (NavMeshAgent != null && NavMeshAgent.isActiveAndEnabled && NavMeshAgent.isOnNavMesh)
                NavMeshAgent.SetDestination(destination);
        }

        public void StopImmediately()
        {
            if (NavMeshAgent != null && NavMeshAgent.isActiveAndEnabled && NavMeshAgent.isOnNavMesh)
                NavMeshAgent.ResetPath();
        }

        // 풀에서 재사용/스폰 시 사용 — transform.position을 직접 바꾸면
        // NavMeshAgent가 다음 프레임에 이전 내부 위치로 되돌려 순간이동처럼 보이는 문제를 방지
        public void Warp(Vector3 position)
        {
            if (NavMeshAgent != null && NavMeshAgent.isActiveAndEnabled)
                NavMeshAgent.Warp(position);
            else
                transform.position = position;
        }

        public void Initialize(ModuleOwner owner)
        {
            NavMeshAgent = owner.GetComponent<NavMeshAgent>();
            Debug.Assert(NavMeshAgent != null, "NavMeshAgent == null");
        }
    }
}
