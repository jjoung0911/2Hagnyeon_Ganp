using JWLib.EventChannelSystem;

namespace Player.Skills.Active
{
    // 액티브 스킬 관련 이벤트 — 기존 playerChannel로 발행한다.
    public static class ActiveSkillEvents
    {
        public static readonly ActiveSkillLevelChangedEvent ActiveSkillLevelChangedEvent = new();
        public static readonly ActiveSkillActivatedEvent ActiveSkillActivatedEvent = new();
        public static readonly ActiveSkillEvolvedEvent ActiveSkillEvolvedEvent = new();
    }

    // 스킬 획득(0→1) 또는 레벨업 시 발행 — UI가 구독해 아이콘/레벨 표시를 갱신
    public class ActiveSkillLevelChangedEvent : GameEvent
    {
        public ActiveSkillEnum SkillIndex { get; private set; }
        public int NewLevel { get; private set; }

        public ActiveSkillLevelChangedEvent Init(ActiveSkillEnum skillIndex, int newLevel)
        {
            SkillIndex = skillIndex;
            NewLevel = newLevel;
            return this;
        }
    }

    // 발동형 스킬이 발동될 때마다 발행 — UI 쿨다운 연출 등에 사용
    public class ActiveSkillActivatedEvent : GameEvent
    {
        public ActiveSkillEnum SkillIndex { get; private set; }

        public ActiveSkillActivatedEvent Init(ActiveSkillEnum skillIndex)
        {
            SkillIndex = skillIndex;
            return this;
        }
    }

    // 스킬이 진화(Evolve)될 때 발행 — UI 진화 연출 등에 사용
    public class ActiveSkillEvolvedEvent : GameEvent
    {
        public ActiveSkillEnum SkillIndex { get; private set; }

        public ActiveSkillEvolvedEvent Init(ActiveSkillEnum skillIndex)
        {
            SkillIndex = skillIndex;
            return this;
        }
    }
}
