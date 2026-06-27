namespace UI.Interfaces
{
    public interface ICooldownDisplay
    {
        void UpdateCooldown(int skillIndex, float normalizedCooldown, bool isReady);
    }
}
