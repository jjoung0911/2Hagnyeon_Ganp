using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;

namespace Enemy
{
    public static class EnemyEvents
    {
        public static readonly PushEvent PushEvent = new();
    }

    // 적이 죽고 일정 시간 후 발행 — 구독자가 PoolManagerSO.Push()를 호출해 풀로 반환한다
    public class PushEvent : GameEvent
    {
        public IPoolable Target;

        public PushEvent Init(IPoolable target)
        {
            Target = target;
            return this;
        }
    }
}
