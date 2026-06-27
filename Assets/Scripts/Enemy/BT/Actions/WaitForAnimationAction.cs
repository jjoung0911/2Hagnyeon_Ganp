using System;
using Agents.Modules;
using Enemy;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace _00.Scripts.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "WaitForAnimation", story: "[Enemy] wait for animation", category: "Action/Animation", id: "b7c8d9e0f1a2b3c4d5e6f7a8b9c0d1e2")]
    public partial class WaitForAnimationAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;

        private IAnimationTrigger _agentTrigger;
        private bool _isAnimationEnd;

        protected override Status OnStart()
        {
            if (Enemy.Value == null || Enemy.Value.Trigger == null)
                return Status.Failure;

            _isAnimationEnd = false;
            _agentTrigger = Enemy.Value.Trigger;
            _agentTrigger.OnAnimationEnd += HandleAnimationEnd;

            return Status.Running;
        }

        private void HandleAnimationEnd() => _isAnimationEnd = true;

        protected override Status OnUpdate()
        {
            return _isAnimationEnd ? Status.Success : Status.Running;
        }

        protected override void OnEnd()
        {
            if(_agentTrigger != null)
                _agentTrigger.OnAnimationEnd -= HandleAnimationEnd;
        }
    }
}
