using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;

namespace Agents.FSM
{
    public class AgentLayeredStateMachine
    {
        private Dictionary<int, AgentStateMachine> _layeredMachines = new();

        public AgentLayeredStateMachine(ParentStateSO[] roots, Agent owner)
        {
            foreach (ParentStateSO rootState in roots)
            {
                var subStateMachine = new AgentStateMachine(owner, rootState.SubStates);
                _layeredMachines[rootState.Layer] = subStateMachine;
            }
        }

        public void ChangeState(int layer, int nextIndex, float transitionDuration = 0.1f)
        {
            if (_layeredMachines.TryGetValue(layer, out var machine))
                machine.ChangeState(nextIndex, transitionDuration, layer);
        }

        public void UpdateState(int layer)
        {
            if (_layeredMachines.TryGetValue(layer, out var machine))
                machine.UpdateState();
        }

        public int GetCurrentStateIndex(int layer)
        {
            if (_layeredMachines.TryGetValue(layer, out var machine))
                return machine.CurrentStateIndex;
            return -1;
        }
    }
}