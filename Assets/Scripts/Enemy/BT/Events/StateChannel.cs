using _00.Scripts.Enemy.BT;
using System;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;

#if UNITY_EDITOR
[CreateAssetMenu(menuName = "Behavior/Event Channels/StateChannel")]
#endif
[Serializable, GeneratePropertyBag]
[EventChannelDescription(name: "StateChannel", message: "Change [State]", category: "Events", id: "7ea7418e63f194e0659e1071c03f53f1")]
public sealed partial class StateChannel : EventChannel<EnemyState> { }

