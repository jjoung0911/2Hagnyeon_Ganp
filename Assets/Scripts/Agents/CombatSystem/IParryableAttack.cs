using Agents.Modules;
using UnityEngine;

namespace Agents.CombatSystem
{
    // 공격 또는 투사체가 구현하는 인터페이스.
    // 이 인터페이스를 구현한 오브젝트는 패링 판정 대상이 된다.
    public interface IParryableAttack
    {
        // 이 공격을 시작한 에이전트 (VFX 위치·카운터 이벤트 용도)
        ModuleOwner Attacker { get; }

        // 패링 성공 시 호출 — 반사·소멸 등 공격 측 처리를 여기서 수행
        // hitPoint: 패링 판정이 발생한 월드 위치
        void OnParried(Vector3 hitPoint);
    }
}
