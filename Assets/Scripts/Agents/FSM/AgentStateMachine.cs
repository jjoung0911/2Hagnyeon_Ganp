using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Agents.FSM
{
    public class AgentStateMachine
    {
        private Dictionary<int, AgentState> _states = new();
        public AgentState CurrentState { get; private set; }
        public int CurrentStateIndex { get; private set; } = -1;

        public AgentStateMachine(Agent owner, IReadOnlyList<StateSO> states)
        {
            foreach (var so in states)
            {
                Type t = Type.GetType(so.className)
                      ?? Type.GetType(so.className + ", Assembly-CSharp");
                Debug.Assert(t != null, $"State type not found: {so.className}");

                int paramHash = so.param != null ? so.param.ParamHash : 0;
                AgentState state = Activator.CreateInstance(t, owner, paramHash) as AgentState;
                _states[so.stateIndex] = state;
            }
        }

        public void ChangeState(int nextIndex, float transitionDuration = 0.1f, int layer = -1)
        {
            CurrentState?.Exit();

            if (!_states.TryGetValue(nextIndex, out AgentState nextState))
            {
                Debug.LogAssertion($"[FSM] State index {nextIndex} not found.");
                return;
            }

            CurrentStateIndex = nextIndex;
            CurrentState = nextState;
            CurrentState.Enter(transitionDuration, layer);
        }

        public void UpdateState()
        {
            CurrentState?.Update();
        }
    }
}
