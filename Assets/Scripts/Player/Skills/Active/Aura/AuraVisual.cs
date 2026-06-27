using JWLib.ObjectPool.Runtime;
using UnityEngine;

namespace Player.Skills.Active
{
    // 플레이어를 따라다니는 오라 VFX(마법진 파티클 — FX_Ritual_Circle_01).
    public class AuraVisual : MonoBehaviour
    {
        private ParticleSystem[] _particles;

        private void Awake() => _particles = GetComponentsInChildren<ParticleSystem>(true);

        // radius에 맞춰 VFX 스케일을 조정
        public void SetRadius(float radius) => transform.localScale = Vector3.one * radius;

        // 버스트 시작 — 파티클 재생
        public void Play()
        {
            foreach (var particle in _particles)
                particle.Play();
        }

        // 버스트 종료 — 즉시 정지 + 기존 파티클 제거(다음 Play 시 깔끔하게 재시작)
        public void Stop()
        {
            foreach (var particle in _particles)
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
