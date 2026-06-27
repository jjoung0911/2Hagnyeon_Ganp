using System;
using Enemy;
using JWLib.AnimationSystem;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace _00.Scripts.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PlayClipAction", story: "[Enemy] play [Clip] at [Layer] and [Position]", category: "Action/Animation", id: "b1c2d3e4f5a6b7c8d9e0f1a2b3c4d5e6")]
    public partial class PlayClipAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<AnimParamSO> Clip;
        [SerializeReference] public BlackboardVariable<int> Layer;
        [SerializeReference] public BlackboardVariable<int> Position;
        [SerializeReference] public BlackboardVariable<float> CrossDuration;

        protected override Status OnStart()
        {
            var enemy = Enemy?.Value;
            var clip = Clip?.Value;
            if (enemy?.Renderer == null || clip == null) return Status.Failure;

            float normalizedTime = (Position?.Value ?? 0) / 100f;
            int layer = Layer?.Value ?? -1;
            float crossDur = CrossDuration?.Value ?? 0.1f;

            enemy.Renderer.PlayClip(clip.ParamHash, crossDur, normalizedTime, layer);
            return Status.Success;
        }

        protected override Status OnUpdate() => Status.Success;
        protected override void OnEnd() { }
    }
}
