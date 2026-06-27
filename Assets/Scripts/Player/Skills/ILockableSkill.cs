namespace Player.Skills
{
    // 잠금 해제가 필요한 스킬이 구현하는 인터페이스.
    // SkillUnlockUpgradeSO가 이 인터페이스를 통해 해금을 적용한다.
    // 구현 시 CanUseSkill() 내부에서 IsUnlocked를 확인해야 잠금이 실제로 동작한다.
    public interface ILockableSkill
    {
        bool IsUnlocked { get; }
        void Unlock();
    }
}
