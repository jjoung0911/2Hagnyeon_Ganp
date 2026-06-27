using JWLib.EventChannelSystem;
using UnityEngine;

namespace Player.LevelSystem
{
    public static class LevelEvents
    {
        public static readonly EnemyKilledEvent EnemyKilledEvent = new();
        public static readonly ExpChangedEvent ExpChangedEvent = new();
        public static readonly ExperienceOrbDropEvent ExperienceOrbDropEvent = new();
    }

    // 적 처치 시 발행 — ExpRewardModule이 발행, PlayerLevelModule이 구독해 경험치를 누적한다
    public class EnemyKilledEvent : GameEvent
    {
        public int ExpReward;

        public EnemyKilledEvent Init(int expReward)
        {
            ExpReward = expReward;
            return this;
        }
    }

    // 경험치/레벨 변동 시 발행 — View(경험치 바, 레벨 텍스트 등)가 구독
    public class ExpChangedEvent : GameEvent
    {
        public int CurrentExp;
        public int RequiredExp;
        public int Level;

        public ExpChangedEvent Init(int currentExp, int requiredExp, int level)
        {
            CurrentExp = currentExp;
            RequiredExp = requiredExp;
            Level = level;
            return this;
        }
    }

    // 적 사망 시 발행 — ExpRewardModule이 발행, ExperienceOrbManager가 구독해 경험치 오브를 드롭한다
    public class ExperienceOrbDropEvent : GameEvent
    {
        public Vector3 Position;
        public int ExpAmount;

        public ExperienceOrbDropEvent Init(Vector3 position, int expAmount)
        {
            Position = position;
            ExpAmount = expAmount;
            return this;
        }
    }
}
