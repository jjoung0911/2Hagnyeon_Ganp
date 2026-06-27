using Agents.Modules;

namespace Agents.FSM
{
    public class AgentState
    {
        protected Agent Owner;
        protected int AnimHash;
        protected bool IsTriggerCall;
        protected AgentStat _stat;

        protected IRenderer _renderer;

        public AgentState(Agent owner, int paramHash)
        {
            Owner = owner;
            AnimHash = paramHash;
            _renderer = Owner.GetModule<IRenderer>();
            _stat = Owner.GetModule<AgentStat>();
        }

        public virtual void Enter(float transitionDuration, int layer = -1)
        {
            IsTriggerCall = false;
            if (AnimHash != 0)
                _renderer.PlayClip(AnimHash, 0.1f, transitionDuration, layer);
        }
        
        public virtual void Update(){}
        
        public virtual void Exit(){}

        protected void AnimationEndTrigger() => IsTriggerCall = true;
    }
}