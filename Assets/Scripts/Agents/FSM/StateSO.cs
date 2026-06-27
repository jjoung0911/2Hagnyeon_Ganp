using JWLib.AnimationSystem;
using UnityEngine;

namespace Agents.FSM
{
    [CreateAssetMenu(fileName = "StateSO", menuName = "SO/FSM/StateSO", order = 0)]
    public class StateSO : ScriptableObject
    {
        public string className;
        public string stateName;
        public int stateIndex;
        public AnimParamSO param;
    }
}
