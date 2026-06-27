namespace Player
{
    public interface IJumpModule
    {
        bool IsAirborne { get; }
        void TryJump();
    }
}