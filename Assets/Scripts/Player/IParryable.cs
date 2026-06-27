using Agents.Modules;
using UnityEngine;

namespace Player
{
    public interface IParryable
    {
        // 패링 입력 시점에 호출 — 근접 공격 + 투사체 통합 판정
        bool TryParry(out ModuleOwner attacker, out Vector3 hitPoint);

        // 패링 윈도우 중 Update마다 호출 — 투사체만 지속 감지
        // 근접 공격(InvokeAttackEnd)은 입력 시점 1회만 처리하므로 여기서는 제외
        bool TryParryProjectile(out ModuleOwner attacker, out Vector3 hitPoint);
    }
}
