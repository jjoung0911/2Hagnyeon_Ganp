using UnityEngine;

namespace System.EffectSystem
{
    public class PlayParticleVfx : MonoBehaviour, IPlayableVFX
    {
        [field: SerializeField] public AssetNameSO VFXAsset { get; private set; }
        [field: SerializeField] public float VfxDuration { get; private set; }
        [SerializeField] private ParticleSystem[] particles;

        // radius에 맞춰 VFX 스케일을 조정 — 충돌 판정 반경과 비주얼 크기를 맞출 때 사용
        public void SetRadius(float radius)
        {
            const float baseRadius = 5f;
            float scaleRatio = radius / baseRadius;

            transform.localScale = Vector3.one * scaleRatio;
        }

        public void Play()
        {
            foreach (ParticleSystem particle in particles)
            {
                // 파괴된 자식 파티클이 있어도 나머지가 끝까지 재생되도록 건너뜀
                if (particle == null) continue;
                particle.Play();
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
            foreach (ParticleSystem particle in particles)
            {
                if (particle == null) continue;
                // 즉시 정지 + 기존 파티클 제거 — 다음 Play() 시 깔끔하게 다시 켜지도록
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}