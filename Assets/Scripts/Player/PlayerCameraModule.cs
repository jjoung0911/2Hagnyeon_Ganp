using Agents.Modules;
using Agents.Modules.Movement;
using Enemy.Boss;
using JWLib.EventChannelSystem;
using Unity.Cinemachine;
using UnityEngine;

namespace Player
{
    public class PlayerCameraModule : MonoBehaviour, IModule, IAfterInit
    {
        [Header("락온 설정")]
        [SerializeField] private float lockOnSpeed = 8f;
        [SerializeField] private Transform defaultLookAt;

        [Header("쉐이크 설정")]
        [SerializeField] private float traumaDecayRate = 1.8f;
        [SerializeField] private float maxShakeAmplitude = 3.5f;
        [SerializeField] private float maxShakeFrequency = 1.2f;

        [Header("걷기 흔들림(헤드 바브) 설정")]
        [SerializeField] private float bobAmplitude = 0.04f;
        [SerializeField] private float bobFrequency = 6f;
        [SerializeField] private float maxBobSpeed = 6f;
        [SerializeField] private float bobSmoothSpeed = 8f;

        [Header("카메라 참조")]
        [SerializeField] private CinemachineCamera vcam;

        [Header("보스 이벤트")]
        [SerializeField] private EventChannelSO bossEventChannel;

        private CinemachineImpulseSource _impulseSource;
        private CinemachineBasicMultiChannelPerlin _noise;
        private PlayerCombatModule _combatModule;
        private IMoveData _moveData;
        private Player _player;

        private float _trauma;
        private Vector3 _basePos;
        private float _bobTimer;

        public void Initialize(ModuleOwner owner)
        {
            _player = owner as Player;
            _impulseSource = GetComponent<CinemachineImpulseSource>();
            _noise = vcam.GetComponent<CinemachineBasicMultiChannelPerlin>();
            _basePos = transform.localPosition;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void AfterInit()
        {
            _combatModule = _player.GetModule<PlayerCombatModule>();
            _moveData = _player.GetModule<IMoveData>();
            bossEventChannel?.AddListener<CameraShakeRequestEvent>(HandleCameraShakeRequest);
        }

        private void OnDestroy()
        {
            bossEventChannel?.RemoveListener<CameraShakeRequestEvent>(HandleCameraShakeRequest);
        }

        private void HandleCameraShakeRequest(CameraShakeRequestEvent evt)
        {
            ShakeCamera(evt.Force, evt.Velocity);
        }

        private void LateUpdate()
        {
            UpdateLockOn();
            UpdateTrauma();
            UpdateWalkBob();
        }

        private void UpdateLockOn()
        {
            Transform target = _combatModule?.LockOnTarget;
            vcam.LookAt = target != null ? target : defaultLookAt;
        }

        private void UpdateTrauma()
        {
            if (_noise == null) return;

            float shake = _trauma * _trauma;
            _noise.AmplitudeGain = shake * maxShakeAmplitude;
            _noise.FrequencyGain = shake * maxShakeFrequency;
            _trauma = Mathf.Max(0f, _trauma - traumaDecayRate * Time.deltaTime);
        }

        // 이동 속도에 비례해 카메라를 상하/좌우로 흔들어 걷는 느낌을 표현
        private void UpdateWalkBob()
        {
            if (_moveData == null) return;

            Vector3 horizontalVel = new Vector3(_moveData.MoveVelocity.x, 0f, _moveData.MoveVelocity.z);
            float speedRatio = Mathf.Clamp01(horizontalVel.magnitude / maxBobSpeed);

            if (_moveData.IsGrounded && speedRatio > 0.01f)
                _bobTimer += Time.deltaTime * bobFrequency;
            else
                _bobTimer = 0f;

            float verticalOffset = Mathf.Abs(Mathf.Sin(_bobTimer)) * bobAmplitude * speedRatio;
            float horizontalOffset = Mathf.Sin(_bobTimer * 0.5f) * bobAmplitude * 0.5f * speedRatio;
            Vector3 targetPos = _basePos + new Vector3(horizontalOffset, verticalOffset, 0f);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, bobSmoothSpeed * Time.deltaTime);
        }

        /// <param name="force">충격 세기 (0~1 권장, 누적됨)</param>
        /// <param name="velocity">방향성 Impulse 벡터 — zero이면 Impulse 생략</param>
        public void ShakeCamera(float force = 1f, Vector3 velocity = default)
        {
            // Cinemachine Impulse는 감쇠 전 이전 임펄스 위에 새 임펄스가 그대로 중첩된다.
            // 콤보 후반처럼 짧은 간격으로 연타가 들어오면 _trauma가 아직 decay되지 않은 채 쌓여
            // 임펄스가 누적 합산되어 체감 쉐이크가 과도하게 강해진다.
            // 이미 쌓인 트라우마만큼 새 임펄스 강도를 줄여 누적 폭주를 방지한다.
            float impulseForce = force * (1f - _trauma * 0.5f);

            _trauma = Mathf.Clamp01(_trauma + force * 0.4f);

            if (_impulseSource != null && velocity != Vector3.zero)
            {
                _impulseSource.DefaultVelocity = velocity;
                _impulseSource.GenerateImpulseWithForce(impulseForce);
            }
        }
    }
}
