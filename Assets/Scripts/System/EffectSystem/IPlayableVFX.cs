
using UnityEngine;

namespace System.EffectSystem
{
    public interface IPlayableVFX
    {
        public AssetNameSO VFXAsset { get; }
        float VfxDuration { get; }
        public void Play();
        public void Play(Vector3 pos, Quaternion rotation);
        public void Play(Vector3 pos);
        public void Stop();
    }
}