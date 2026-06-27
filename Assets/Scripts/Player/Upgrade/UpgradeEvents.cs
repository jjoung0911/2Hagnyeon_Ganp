using Agents.Modules;
using JWLib.EventChannelSystem;
using System.UpgradeSystem;

namespace Player.Upgrade
{
    public static class UpgradeEvents
    {
        public static readonly LevelUpEvent LevelUpEvent = new();
        public static readonly UpgradeChoicesReadyEvent UpgradeChoicesReadyEvent = new();
        public static readonly UpgradeAppliedEvent UpgradeAppliedEvent = new();
        public static readonly UpgradeSelectedEvent UpgradeSelectedEvent = new();
    }
    
    // 외부 레벨/경험치 시스템에서 레벨업 시 발행 — PlayerUpgradeModule이 구독해 선택지를 생성한다
    public class LevelUpEvent : GameEvent
    {
        public int NewLevel;

        public LevelUpEvent Init(int newLevel)
        {
            NewLevel = newLevel;
            return this;
        }
    }

    // 업그레이드 선택지가 준비됐을 때 발행 — View가 구독해 선택창을 표시
    public class UpgradeChoicesReadyEvent : GameEvent
    {
        public UpgradeDataSO[] Choices;

        public UpgradeChoicesReadyEvent Init(UpgradeDataSO[] choices)
        {
            Choices = choices;
            return this;
        }
    }
    
    public class UpgradeSelectedEvent : GameEvent
    {
        public UpgradeDataSO Selected;

        public UpgradeSelectedEvent Init(UpgradeDataSO selected)
        {
            Selected = selected;
            return this;
        }
    }

    // 업그레이드 선택 및 적용 완료 시 발행 — View가 선택창을 닫고 결과를 표시
    public class UpgradeAppliedEvent : GameEvent
    {
        public UpgradeDataSO Selected;
        public ModuleOwner Owner;

        public UpgradeAppliedEvent Init(UpgradeDataSO selected, ModuleOwner owner)
        {
            Selected = selected;
            Owner = owner;
            return this;
        }
    }
}
