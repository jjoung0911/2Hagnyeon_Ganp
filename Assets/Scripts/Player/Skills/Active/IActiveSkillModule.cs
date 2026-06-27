namespace Player.Skills.Active
{
    public interface IActiveSkillModule
    {
        AbstractActiveSkill GetSkill(ActiveSkillEnum index);
        AbstractActiveSkill GetSkill(int index);
    }
}