using System;
using System.Collections;
using System.Events;
using System.StatSystem;
using __.GameModules.PlayerData;
using Agents.Modules;
using Agents.Modules.Movement;
using csiimnida.CSILib.SoundManager.RunTime;
using JWLib.AnimationSystem;
using JWLib.EventChannelSystem;
using UnityEngine;

namespace Player
{
    public class PlayerMoveData : AgentMoveData
    {
        [SerializeField] private StatSO runSpeedStatSo;
        [SerializeField] private StatSO dashPowerStatSo;
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private AnimationCurve runSpeedCurve;

        [SerializeField] private AnimParamSO moveSpeedParam;

        [Header("사운드")]
        [Tooltip("달리기 중 재생할 루프 발소리 SFX")]
        [SerializeField] private SoundSo footstepRunSfx;

        public float MaxRunSpeed => _runSpeedMax;
        private bool _isRunning;
        private Coroutine _runCoroutine;
        private Player _player;
        private InputReader _input;
        private Transform _camTrs;
        private AudioModule _audio;
        private float _runSpeedMax; // 스탯에서 온 목표 달리기 속도
        private float _currentRunSpeed; // 가속 애니메이션이 적용된 현재 달리기 속도
        public event Action<bool> OnRunStateChanged;

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                _isRunning = value;
                OnRunStateChanged?.Invoke(value);
                playerChannel.RaiseEvent(CameraEvents.onRunningStateChanged.Init(value));
            }
        }

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _player = owner as Player;
            _camTrs = Camera.main.transform;
            _input = _player.Input;
            _audio = _player.GetModule<AudioModule>();
            _player.Input.OnRunKeyPressed += SetRunCondition;
            
        }

        public override void AfterInit()
        {
            base.AfterInit();
            _stat.SubscribeStat(runSpeedStatSo.Index, HandleRunSpeedChange, 1);
            _runSpeedMax = _stat.GetStat(runSpeedStatSo.Index).Value;
            _currentRunSpeed = _walkSpeed;
        }

        private void FixedUpdate()
        {
            Vector2 inputDir = _input.MoveDir;
            Vector3 moveDir = GetCameraRelativeDirection(inputDir);
            SetMoveDir(moveDir);
        }

        public void SetRunCondition(bool isRun)
        {
            if (IsRunning == isRun) return;
            IsRunning = isRun;
            if (IsRunning)
            {
                StartMove();
                _audio?.PlaySfx(footstepRunSfx);
            }
            else
            {
                _audio?.StopSfx(footstepRunSfx);
            }
        }

        public override float GetMoveSpeed() => IsRunning ? _currentRunSpeed : _walkSpeed;

        private void HandleRunSpeedChange(StatSO stat, float current, float previous) => _runSpeedMax = current;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _stat.UnSubscribeStat(runSpeedStatSo.Index, HandleRunSpeedChange);

            // InputReader(SO)는 씬 리로드 후에도 살아남으므로 반드시 해제 — 묵은 델리게이트 누적 방지
            if (_input != null)
                _input.OnRunKeyPressed -= SetRunCondition;
        }

        #region Run Accelation

            public void StartMove()
            {
                if (_runCoroutine != null)
                    StopCoroutine(_runCoroutine);
                _runCoroutine = StartCoroutine(VelocityCoroutine());
            }
            private IEnumerator VelocityCoroutine()
            {
                float startSpeed = _currentRunSpeed;
                float targetSpeed = _runSpeedMax;
                float currentT = 0;
                while (currentT < 0.5f)
                {
                    currentT += Time.deltaTime;
                    float t = Mathf.Clamp01(currentT / 0.5f);
                    _currentRunSpeed = Mathf.Lerp(startSpeed, targetSpeed, runSpeedCurve.Evaluate(t));
                    yield return null;
                }
                _currentRunSpeed = targetSpeed;
                _runCoroutine = null;
            }
        #endregion

        #region MoveDir Helper
            private Vector3 GetCameraRelativeDirection(Vector2 inputDir)
            {
                Vector3 camForward = _camTrs.forward;
                Vector3 camRight = _camTrs.right;
                camForward.y = 0; camRight.y = 0;
                camForward.Normalize(); camRight.Normalize();
                Vector3 final = (camForward * inputDir.y) + (camRight * inputDir.x);
                return final;
            }
        #endregion
    }
}