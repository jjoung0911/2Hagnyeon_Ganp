using Agents.CombatSystem;
using JWLib.EventChannelSystem;

namespace Player.Skills
{
    public static class SkillEvents
    {
        public static SkillUseEvent SkillUseEvent = new();
        public static ChargeAttackEvent ChargeAttackEvent = new();
    }

    public class SkillUseEvent : GameEvent
    {
        public SkillDataSO UseSkill;
        public float CoolTime;

        public SkillUseEvent Init(SkillDataSO skillData, float coolTime)
        {
            UseSkill = skillData;
            CoolTime = coolTime;
            return this;
        }
    }

    // 차지 공격이 준비됐을 때 발행 — PlayerAttackBuffModule이 수신해 데미지 스탯에 modifier를 추가
    public class ChargeAttackEvent : GameEvent
    {
        public float DamageBonus;

        public ChargeAttackEvent Init(float bonus)
        {
            DamageBonus = bonus;
            return this;
        }
    }
}