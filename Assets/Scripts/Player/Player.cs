using __.GameModules.PlayerData;
using Agents;
using Agents.FSM;
using Agents.Modules;
using UnityEngine;

namespace Player
{
    public class Player : Agent
    {
        [field: SerializeField] public InputReader Input { get; private set; }

    }
}
