using UnityEngine;

namespace Agents.FSM
{
    [CreateAssetMenu(fileName = "StateListSO", menuName = "SO/FSM/StateListSO", order = 0)]
    public class StateListSO : ScriptableObject
    {
        public string enumName;
        public StateSO[] states;
    }
}