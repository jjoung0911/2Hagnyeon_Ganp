using System.Collections.Generic;
using System.StatSystem;
using JWLib.EventChannelSystem;
using UnityEngine;

namespace UI.Events
{
    // 플레이어의 현재 스탯 스냅샷이 바뀔 때 발행. Stats는 플레이어의 클론 StatSO(값 실시간 반영) 목록.
    public class PlayerStatsChangedEvent : GameEvent
    {
        public IReadOnlyList<StatSO> Stats;

        public PlayerStatsChangedEvent Init(IReadOnlyList<StatSO> stats)
        {
            Stats = stats;
            return this;
        }
    }

    public class PlayerHpChangedEvent : GameEvent
    {
        public float Current;
        public float Max;

        public PlayerHpChangedEvent Init(float current, float max)
        {
            Current = current;
            Max = max;
            return this;
        }
    }

    public class UltGaugeChangedEvent : GameEvent
    {
        public float Current;
        public float Max;
        public bool IsReady;

        public UltGaugeChangedEvent Init(float current, float max, bool isReady)
        {
            Current = current;
            Max = max;
            IsReady = isReady;
            return this;
        }
    }

    public class SkillCooldownChangedEvent : GameEvent
    {
        // 슬롯별로 별도 캐시되어야 한다 — 안 그러면 늦게 구독한 리스너는 마지막 슬롯 값만 받는다.
        public override object CacheKey => SlotIndex;

        public int SlotIndex;
        public int SkillIndex;
        public string SkillName;
        public Sprite Icon;
        public float NormalizedCooldown;
        public float RemainingSeconds;
        public bool IsReady;
        public bool IsUnlocked;

        public SkillCooldownChangedEvent Init(int slotIndex, int skillIndex, string name, Sprite icon, float normalized, float remainingSeconds, bool isReady, bool isUnlocked)
        {
            SlotIndex = slotIndex;
            SkillIndex = skillIndex;
            SkillName = name;
            Icon = icon;
            NormalizedCooldown = normalized;
            RemainingSeconds = remainingSeconds;
            IsReady = isReady;
            IsUnlocked = isUnlocked;
            return this;
        }
    }

    // 대쉬 충전 수 / 다음 충전까지의 진행률이 바뀔 때 발행
    public class DashChargeChangedEvent : GameEvent
    {
        public int CurrentCharges;
        public int MaxCharges;
        public float NormalizedCooldown;

        public DashChargeChangedEvent Init(int currentCharges, int maxCharges, float normalizedCooldown)
        {
            CurrentCharges = currentCharges;
            MaxCharges = maxCharges;
            NormalizedCooldown = normalizedCooldown;
            return this;
        }
    }

    public class ChargeProgressEvent : GameEvent
    {
        public float Progress;
        public bool IsCharging;
        public bool IsFullCharge;

        public ChargeProgressEvent Init(float progress, bool isCharging, bool isFullCharge)
        {
            Progress = progress;
            IsCharging = isCharging;
            IsFullCharge = isFullCharge;
            return this;
        }
    }

    public class ComboChangedEvent : GameEvent
    {
        public int Count;

        public ComboChangedEvent Init(int count)
        {
            Count = count;
            return this;
        }
    }

    public enum FeedbackType { Default, Parry, Slice, UltReady, Counter, Charged }

    public class CombatFeedbackEvent : GameEvent
    {
        public string Text;
        public FeedbackType Type;

        public CombatFeedbackEvent Init(string text, FeedbackType type)
        {
            Text = text;
            Type = type;
            return this;
        }
    }

    public class BossHpChangedEvent : GameEvent
    {
        public string BossName;
        public float Current;
        public float Max;

        public BossHpChangedEvent Init(string bossName, float current, float max)
        {
            BossName = bossName;
            Current = current;
            Max = max;
            return this;
        }
    }

    // 적이 스폰/처치되거나 시간이 흘러 난이도 진행 상황이 바뀔 때마다 발행
    public class DifficultyProgressEvent : GameEvent
    {
        public float ElapsedTime;
        public int RemainingEnemies;
        public int TotalSpawned;
        public int KillCount;

        public DifficultyProgressEvent Init(float elapsedTime, int remainingEnemies, int totalSpawned, int killCount)
        {
            ElapsedTime = elapsedTime;
            RemainingEnemies = remainingEnemies;
            TotalSpawned = totalSpawned;
            KillCount = killCount;
            return this;
        }
    }

    // 모든 UI 이벤트 인스턴스를 사전 생성해두는 캐시.
    // RaiseEvent 호출마다 new를 하지 않고 이 인스턴스를 Init()으로 초기화해서 재사용한다.
    public static class UIEventCache
    {
        public static readonly PlayerHpChangedEvent      PlayerHpChanged      = new();
        public static readonly UltGaugeChangedEvent      UltGaugeChanged      = new();
        public static readonly DashChargeChangedEvent    DashChargeChanged    = new();
        public static readonly ChargeProgressEvent       ChargeProgress       = new();
        public static readonly ComboChangedEvent         ComboChanged         = new();
        public static readonly CombatFeedbackEvent       CombatFeedback       = new();
        public static readonly BossHpChangedEvent        BossHpChanged        = new();
        public static readonly DifficultyProgressEvent   DifficultyProgress   = new();
        public static readonly PlayerStatsChangedEvent   PlayerStatsChanged   = new();
    }
}
