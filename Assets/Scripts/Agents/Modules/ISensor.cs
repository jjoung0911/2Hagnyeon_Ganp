using UnityEngine;

namespace Agents.Modules
{
    public interface ISensor
    {
        Collider[] ColliderResults { get; }
        bool IsTargetInViewAngle(Transform targetTransform, float viewAngle);
        bool IsTargetInSight(Transform targetTrm);
        bool IsTargetInViewRadius(Transform targetTrm, float viewRadius);
        int FindTargetInRadius(float viewRadius);
        Collider FindClosestTargetInRadius(float viewRadius);
        Collider FindClosestTarget();
    }
}