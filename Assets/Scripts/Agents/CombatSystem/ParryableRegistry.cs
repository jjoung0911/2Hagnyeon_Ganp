using System.Collections.Generic;
using UnityEngine;

namespace Agents.CombatSystem
{
    // 현재 활성 상태인 IParryableAttack 오브젝트를 관리하는 정적 레지스트리.
    // 투사체/공격은 생성 시 Register, 소멸 시 Unregister를 호출한다.
    // ParryModule이 물리 쿼리 없이 근처 패링 대상을 탐색할 때 사용한다.
    public static class ParryableRegistry
    {
        private static readonly List<IParryableAttack> _active = new();

        public static void Register(IParryableAttack attack) => _active.Add(attack);

        public static void Unregister(IParryableAttack attack) => _active.Remove(attack);

        // pos에서 radius 이내에서 가장 가까운 IParryableAttack을 반환.
        // 없으면 null. hitPoint에 해당 오브젝트의 위치가 담긴다.
        public static IParryableAttack FindNearest(Vector3 pos, float radius, out Vector3 hitPoint)
        {
            float sqrRadius = radius * radius;
            IParryableAttack nearest = null;
            float nearestSqr = float.MaxValue;
            hitPoint = pos;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                // MonoBehaviour가 Destroy됐을 때 null 체크 (Unity 가짜 null 처리)
                if (_active[i] is not MonoBehaviour mb || mb == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                // 풀로 반환되어 비활성화된 투사체는 패링 대상에서 제외
                if (!mb.gameObject.activeInHierarchy)
                    continue;

                float sqr = (mb.transform.position - pos).sqrMagnitude;
                if (sqr <= sqrRadius && sqr < nearestSqr)
                {
                    nearest = _active[i];
                    nearestSqr = sqr;
                    hitPoint = mb.transform.position;
                }
            }

            return nearest;
        }
    }
}
