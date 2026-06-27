using UnityEngine;
using UnityEngine.VFX;

namespace System.EffectSystem
{
    public class PlayGraphVfx : MonoBehaviour, IPlayableVFX
    {
        [field: SerializeField] public AssetNameSO VFXAsset { get; }
        [field: SerializeField] public float VfxDuration { get; private set; }
        [SerializeField] private VisualEffect[] effects;
        public void Play()
        {
            foreach (VisualEffect effect in effects)
            {
                effect.Play();
            }
        }

        public void Play(Vector3 pos, Quaternion rotation)
        {
            // 월드 좌표 지정 재생 — 플레이어 등 부모를 따라가지 않도록 분리 후 배치
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(pos, rotation);
            Play();
        }

        public void Play(Vector3 pos)
        {
            transform.position = pos;
            Play();
        }

        public void Stop()
        {
            foreach (VisualEffect effect in effects)
            {
                effect.Stop();
            }
        }
    }
}