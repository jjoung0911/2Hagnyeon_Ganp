using System.Collections.Generic;
using System.EffectSystem;
using System.Linq;
using UnityEngine;

namespace Agents.Modules
{
    public class VFXModule : MonoBehaviour, IModule
    {
        private Agent _owner;
        private Dictionary<int, IPlayableVFX> _playableDict = new Dictionary<int, IPlayableVFX>();
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner as Agent;
            _playableDict = GetComponentsInChildren<IPlayableVFX>()
                .ToDictionary(x => x.VFXAsset.AssetHash, x => x);
        }

        public void PlayVFX(int hash, Vector3 position, Quaternion rotation)
        {
            if (_playableDict.TryGetValue(hash, out IPlayableVFX vfx))
            {
                vfx.Play(position, rotation);
            }
            else
            {
                Debug.LogWarning($"VFX with hash {hash} not found in {name}");
            }
        }
        
        public void PlayVFX(int hash)
        {
            if (_playableDict.TryGetValue(hash, out IPlayableVFX vfx))
            {
                vfx.Play();
            }
            else
            {
                Debug.LogWarning($"VFX with hash {hash} not found in {name}");
            }
        }
        public void StopVFX(int hash)
        {
            if (_playableDict.TryGetValue(hash, out IPlayableVFX vfx))
            {
                vfx.Stop();
            }
            else
            {
                Debug.LogWarning($"VFX with hash {hash} not found in {name}");
            }
        }

        // 등록된 VFX를 구체 타입으로 가져온다 — SetRadius 등 IPlayableVFX에 없는 타입 고유 제어가 필요할 때 사용
        public bool TryGetVFX<T>(int hash, out T vfx) where T : IPlayableVFX
        {
            if (_playableDict.TryGetValue(hash, out IPlayableVFX playable) && playable is T typed)
            {
                vfx = typed;
                return true;
            }

            vfx = default;
            return false;
        }
    }
}