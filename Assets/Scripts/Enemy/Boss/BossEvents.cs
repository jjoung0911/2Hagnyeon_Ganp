using JWLib.EventChannelSystem;
using UnityEngine;

namespace Enemy.Boss
{
    // 보스 페이즈 전환 알림 — 카메라 연출 등 외부 구독자에게 전달
    public class BossPhaseChangedEvent : GameEvent
    {
        public BossPhase NewPhase { get; private set; }

        public BossPhaseChangedEvent Init(BossPhase newPhase)
        {
            NewPhase = newPhase;
            return this;
        }
    }

    // 카메라 흔들림 요청 — PlayerCameraModule이 구독해 ShakeCamera를 호출한다
    public class CameraShakeRequestEvent : GameEvent
    {
        public float Force { get; private set; }
        public Vector3 Velocity { get; private set; }

        public CameraShakeRequestEvent Init(float force, Vector3 velocity = default)
        {
            Force = force;
            Velocity = velocity;
            return this;
        }
    }

    // 보스 사망 연출(슬로우모션) 종료 후 발행 — 보스 격파 UI 트리거
    public class BossDefeatedEvent : GameEvent
    {
    }

    // 보스 등장 연출 시작 — 플레이어 입력 잠금, 보스 HP바 등장 등 외부 구독자에게 전달
    public class BossIntroStartedEvent : GameEvent
    {
    }

    // 보스 등장 연출 종료 — 플레이어 입력 복원, 보스 BT 활성화 등 외부 구독자에게 전달
    public class BossIntroEndedEvent : GameEvent
    {
    }

    // 보스 이벤트 인스턴스 캐시 — RaiseEvent마다 new하지 않고 Init()으로 재사용
    public static class BossEvents
    {
        public static readonly BossPhaseChangedEvent BossPhaseChanged = new();
        public static readonly CameraShakeRequestEvent CameraShakeRequest = new();
        public static readonly BossDefeatedEvent BossDefeated = new();
        public static readonly BossIntroStartedEvent BossIntroStarted = new();
        public static readonly BossIntroEndedEvent BossIntroEnded = new();
    }
}
