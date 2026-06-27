using System.Collections.Generic;
using UnityEngine;

namespace Agents.FSM
{
    [CreateAssetMenu(fileName = "ParentStateSO", menuName = "SO/FSM/ParentStateSO", order = 0)]
    public class ParentStateSO : ScriptableObject
    {
        [field: SerializeField] public int Layer { get; private set; }
        [field: SerializeField] public List<StateSO> SubStates { get; private set; }
        [field: SerializeField] public string rootName;
    }
}