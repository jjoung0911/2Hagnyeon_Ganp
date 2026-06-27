using Agents.Modules;
using JWLib.EventChannelSystem;
using UnityEngine;

namespace Player
{
    public static class CombatEvents { }

    public class ParrySuccessEvent : GameEvent
    {
        public ModuleOwner Attacker;
        public ModuleOwner Defender;
        public Vector3 HitPoint;

        public ParrySuccessEvent Init(ModuleOwner attacker, ModuleOwner defender, Vector3 hitPoint)
        {
            Attacker = attacker;
            Defender = defender;
            HitPoint = hitPoint;
            return this;
        }
    }

    // 플레이어가 적에게 공격을 적중시킬 때마다 발행.
    // 데이터 없음 — 싱글턴 인스턴스 하나로 충분하다.
    public class PlayerHitEvent : GameEvent { }

    // 플레이어가 적에게 피격당할 때마다 발행.
    public class PlayerDamagedEvent : GameEvent { }

    // 플레이어가 사망한 순간 한 번 발행. 게임오버 흐름·입력 잠금·연출의 시작점.
    // 데이터 없음 — 처치 수는 GameOverController가 DifficultyManager에서 직접 읽는다.
    public class PlayerDiedEvent : GameEvent { }

    // 모든 전투 이벤트 인스턴스를 사전 생성해두는 캐시.
    public static class CombatEventCache
    {
        public static readonly ParrySuccessEvent ParrySuccess  = new();
        public static readonly PlayerHitEvent    PlayerHit     = new();
        public static readonly PlayerDamagedEvent PlayerDamaged = new();
        public static readonly PlayerDiedEvent    PlayerDied    = new();
    }
}
