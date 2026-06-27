using System.Core;
using System.Events;
using JWLib.EventChannelSystem;
using Player;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace System.Camera
{
    public class CameraHandler : MonoSingleton<CameraHandler>
    {
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private GameObject runningEffect;
        [SerializeField] private Vector3 runningEffectOffset;
        private GameObject _runningEffectInstance;

        private CinemachineCamera _cinemachineCamera;
        private Volume _volume;
        // 사망 후에는 GameOverController가 비네트를 전담하므로 여기서는 더 이상 건드리지 않는다
        private bool _vignetteYielded;

        protected override void Awake()
        {
            base.Awake();
            playerChannel.AddListener<RunningEvent>(HandleRunningEffect);
            playerChannel.AddListener<PlayerDiedEvent>(HandlePlayerDied);
            _volume = FindAnyObjectByType<Volume>();
            _cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
        }

        private void OnDestroy()
        {
            playerChannel.RemoveListener<RunningEvent>(HandleRunningEffect);
            playerChannel.RemoveListener<PlayerDiedEvent>(HandlePlayerDied);
        }

        private void HandlePlayerDied(PlayerDiedEvent _) => _vignetteYielded = true;

        private void HandleRunningEffect(RunningEvent effect)
        {
            if (effect.IsRunning)
            {
                _runningEffectInstance = Instantiate(runningEffect,
                    transform);
                _runningEffectInstance.transform.position += runningEffectOffset;
                if(!_vignetteYielded && _volume.profile.TryGet(out Vignette vignette))
                {
                    vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, 0.3f, 0.5f);
                }
            }
            else if (_runningEffectInstance != null && effect.IsRunning == false)
            {
                Destroy(_runningEffectInstance);
                if(!_vignetteYielded && _volume.profile.TryGet(out Vignette vignette))
                {
                    vignette.intensity.value = 0.1f;
                }
            }
        }

        private void Update()
        {
            // Shake();
        }

        public void Shake()
        {
        }
    }
}