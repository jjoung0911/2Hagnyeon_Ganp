using System;

namespace Agents.Modules
{
    public interface IHitHandler
    {
        void HandleHit();
        bool IsStaggered { get; }
        event Action OnStaggerBegin;
    }
}
