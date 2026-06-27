using JWLib.EventChannelSystem;
using UnityEngine;

namespace JWLib.ObjectPool.Runtime.Events
{
    public static class CreateEvents
    {
        public static readonly ShowPoolingVfx ShowPoolingVfx = new();
        public static readonly ShowPoolingVfxNoRot showPoolingVfxNoRot = new();
    }

    public class ShowPoolingVfx : GameEvent
    {
        public PoolItemSO ItemData { get; private set; }
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }

        public ShowPoolingVfx InitData(PoolItemSO itemData, Vector3 position, Quaternion rotation)
        {
            ItemData = itemData;
            Position = position;
            Rotation = rotation;
            return this;
        }

    } 
    public class ShowPoolingVfxNoRot : GameEvent
    {
        public PoolItemSO ItemData { get; private set; }
        public Vector3 Position { get; private set; }
        
        public ShowPoolingVfxNoRot InitData(PoolItemSO itemData, Vector3 position)
        {
            ItemData = itemData;
            Position = position;
            return this;
        }

    }
}