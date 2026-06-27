namespace JWLib.EventChannelSystem
{
    public abstract class GameEvent
    {
        // 같은 이벤트 타입이라도 슬롯/대상별로 별도 캐시가 필요한 경우(예: 스킬 슬롯별 쿨다운) 오버라이드.
        // null이면 타입 하나당 마지막 값 하나만 캐시(기존 동작과 동일).
        public virtual object CacheKey => null;
    }
}