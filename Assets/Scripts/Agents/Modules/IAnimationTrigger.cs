using System;

namespace Agents.Modules
{
    public interface IAnimationTrigger
    {
        public event Action OnAnimationEnd;
        public event Action OnAttack;       // 1회성 타격 순간
        public event Action OnAttackEnd;
        public event Action OnAttackStart;
        public event Action OnFootStep;
    }
}