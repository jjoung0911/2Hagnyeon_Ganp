using UnityEngine;

namespace Agents.Modules
{
    public class Sensor : MonoBehaviour, IModule, ISensor
    {
        // 탐지 범위 제한 없이 타겟을 찾을 때 사용하는 반경 (씬 전체를 포괄하는 충분히 큰 값)
        private const float UnlimitedSearchRadius = 10000f;

        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask targetLayer;
		[SerializeField] private int maxDetectCount;

        private Collider[] _colliderResults;
        public Collider[] ColliderResults => _colliderResults;
        
        private ModuleOwner _owner;
        
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner as Agent;
            _colliderResults = new Collider[maxDetectCount];
        }

        public bool IsTargetInViewAngle(Transform targetTransform, float viewAngle)
        {
            Vector3 direction = targetTransform.position - transform.position;
            direction.y = 0;
            float angle = Vector3.Angle(transform.forward, direction);
            return angle <= viewAngle / 2;
        }

        public bool IsTargetInSight(Transform targetTrm)
        {
            Vector3 targetPosition = targetTrm.position;
            targetPosition.y = transform.position.y;

            Vector3 direction = targetPosition - transform.position;
            float distance = direction.magnitude;

            if (Physics.Raycast(transform.position, direction.normalized, out RaycastHit hit,
                    distance, groundLayer))
            {
                return false;
            }

            return true;
        }

        public bool IsTargetInViewRadius(Transform targetTrm, float viewRadius)
            => (targetTrm.position - transform.position).sqrMagnitude <= viewRadius * viewRadius;

        public int FindTargetInRadius(float viewRadius)
            => Physics.OverlapSphereNonAlloc(transform.position, viewRadius, _colliderResults, targetLayer);

        // 범위 내에서 가장 가까운 타겟의 콜라이더를 반환한다. 없으면 null.
        public Collider FindClosestTargetInRadius(float viewRadius)
        {
            int count = FindTargetInRadius(viewRadius);
            if (count <= 0) return null;

            float minDist = float.MaxValue;
            Collider closest = null;
            for (int i = 0; i < count; i++)
            {
                Collider col = _colliderResults[i];
                if (col == null) continue;

                float dist = (col.transform.position - transform.position).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = col;
                }
            }

            return closest;
        }

        // 탐지 범위(시야각/거리) 검사 없이 targetLayer 내 가장 가까운 타겟을 찾는다.
        public Collider FindClosestTarget() => FindClosestTargetInRadius(UnlimitedSearchRadius);
    }
}