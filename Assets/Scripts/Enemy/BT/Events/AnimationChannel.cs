using System;
using JWLib.AnimationSystem;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;


namespace _00.Scripts.Enemy.BT.Events
{
    [CreateAssetMenu(menuName = "Behavior/Event Channels/AnimationChannel")]
    [Serializable, GeneratePropertyBag]
    [EventChannelDescription(name: "AnimationChannel", message: "Play [Clip]", category: "Action/Animation", id: "d9d4232f31c3dd83d8de79beab70e926")]
    public sealed partial class AnimationChannel : EventChannel<AnimParamSO> { }
}

