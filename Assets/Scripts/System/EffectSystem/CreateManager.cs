using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using UnityEngine;

namespace System.EffectSystem
{
    public class CreateManager : MonoBehaviour
    {
        [SerializeField] private EventChannelSO createChannel;
        [SerializeField] private PoolManagerSO poolManagerAsset;

        private void Awake()
        {
            createChannel.AddListener<ShowPoolingVfx>(HandleShowPoolingVfx);
            createChannel.AddListener<ShowPoolingVfxNoRot>(HandleShowPoolingVfxNoPos);
        }

        private void OnDestroy()
        {
            
            createChannel.RemoveListener<ShowPoolingVfx>(HandleShowPoolingVfx);
            createChannel.RemoveListener<ShowPoolingVfxNoRot>(HandleShowPoolingVfxNoPos);
        }

        private void HandleShowPoolingVfxNoPos(ShowPoolingVfxNoRot evt)
        {
            PoolableVFX vfx = poolManagerAsset.Pop<PoolableVFX>(evt.ItemData);
            if (vfx == null) return; // 풀에 등록되지 않은 ItemData 방어
            vfx.OnVfxEnd += HandleVfxEnd;
            vfx.PlayVfx();
        }

        private void HandleShowPoolingVfx(ShowPoolingVfx evt)
        {
            PoolableVFX vfx = poolManagerAsset.Pop<PoolableVFX>(evt.ItemData);
            if (vfx == null) return; // 풀에 등록되지 않은 ItemData 방어
            vfx.OnVfxEnd += HandleVfxEnd;
            vfx.PlayVfx(evt.Position, evt.Rotation);
        }

        private void HandleVfxEnd(PoolableVFX targetVfx)
        {
            targetVfx.OnVfxEnd -= HandleVfxEnd;
            poolManagerAsset.Push(targetVfx);
        }
    }
}