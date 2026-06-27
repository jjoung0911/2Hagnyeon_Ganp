using System;
using Enemy;
using Unity.Behavior;
using Unity.Properties;
using Action = Unity.Behavior.Action;

namespace _00.Scripts.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "StopAction", story: "Stop [Enemy]", category: "Action/Movement", id: "c2d3e4f5a6b7c8d9e0f1a2b3c4d5e6f7")]
    public partial class StopAction : Action
    {
        [UnityEngine.SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;

        protected override Status OnStart()
        {
            Enemy?.Value?.NavMovement?.StopImmediately();
            return Status.Success;
        }

        protected override Status OnUpdate() => Status.Success;
        protected override void OnEnd() { }
    }
}
