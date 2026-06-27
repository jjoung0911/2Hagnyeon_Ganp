namespace Player.Skills.Passive
{
    public interface IPassiveSkillModule
    {
        AbstractPassiveSkill GetSkill(PassiveSkillEnum index);
        AbstractPassiveSkill GetSkill(int index);
    }
}