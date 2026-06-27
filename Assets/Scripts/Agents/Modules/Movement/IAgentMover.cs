using System;
using UnityEngine;

namespace Agents.Modules.Movement
{
    public interface IAgentMover
    {
        event Action<Vector2> OnVelocityChanged;
    }
}