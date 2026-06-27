using System;
using System.Collections;
using Agents.CombatSystem;
using Agents.Modules;
using Agents.Modules.Movement;
using JWLib.AnimationSystem;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using System.Managers;
using UnityEngine;

namespace Player.Skills
{
    // 키를 누르면 최대 3단계까지 충전, 단계마다 대미지 강화 + 애니메이션·이펙트 변화.
    // 충전 중 키를 떼면 현재 단계로 1회 강타. 단계 미달 시 콤보 스킬로 위임.
    public class PlayerChargeAttackSkill : AbstractPlayerSkill
    {
        private const int MaxTier = 3;

        [SerializeField] private int attackSkillIndex;
        [SerializeField] private float chargeThreshold = 0.15f;    // 이 시간 이상 눌러야 차징 시작
        [SerializeField] private float tierInterval = 0.8f;
        [SerializeField] private float damagePerTier = 20f;

        [Header("애니메이션")]
        [SerializeField] private AnimParamSO chargeStartParam;     // 충전 시작 1회
        [SerializeField] private AnimParamSO chargeLoopParam;      // 충전 중 반복 재생
        [SerializeField] private AnimParamSO[] chargeTierParams;   // 단계 강화 1회 (MaxTier 개)
        [SerializeField] private AnimParamSO chargeEndParam;       // 강타 공격

        [Header("이펙트")]
        [SerializeField] private AssetNameSO chargeLoopVfx;
        [SerializeField] private AssetNameSO[] tierUpgradeVfx;     // MaxTier 개
        [SerializeField] private EventChannelSO createChannel;
        [SerializeField] private PoolItemSO hitVfx;

        [Header("타격")]
        [SerializeField] private RayDamageCaster caster;
        [SerializeField] private HitType hitType = HitType.Heavy;

        [Header("히트 피드백")]
        [SerializeField] private float shakeForce = 0.4f;
        [SerializeField] private Vector3 shakeDirection = Vector3.up;
        [SerializeField] private float hitStopTimeScale = 0.001f;
        [SerializeField] private float hitStopDuration = 0.12f;

        private VFXModule _vfxModule;
        private PlayerCameraModule _cameraModule;
        private Coroutine _chargeCoroutine;
        private Coroutine _thresholdCoroutine;
        private int _currentTier;
        private float _pendingDamage;

        // UI 바인딩용
        public bool IsCharging => _chargeCoroutine != null && _currentTier < MaxTier;
        public bool IsChargeFull => _chargeCoroutine != null && _currentTier >= MaxTier;
        public float ChargeProgress { get; private set; }

        public override bool IsSystemTriggered => false;

        public override void Initialize(ISkillModule module)
        {
            base.Initialize(module);
            _vfxModule = _player.GetModule<VFXModule>();
            _cameraModule = _player.GetModule<PlayerCameraModule>();
            _skillModule.RegisterGatedIndex(attackSkillIndex);
            caster?.InitCaster(module.Owner);
        }

        // InputReader(SO) 캐시 — 파괴 시점에 _player는 이미 파괴돼 접근 불가하므로 SO 참조로 해제한다.
        // (파괴된 UnityEngine.Object는 != null이 false라 _player 가드로는 해제가 누락된다)
        private __.GameModules.PlayerData.InputReader _input;

        private void Start()
        {
            _input = _player.Input;
            _input.OnSkillKeyStateChanged += HandleSkillKeyState;
        }

        private void OnDestroy()
        {
            if (_input != null)
                _input.OnSkillKeyStateChanged -= HandleSkillKeyState;
        }

        private void HandleSkillKeyState(int index, bool pressed)
        {
            if (index != attackSkillIndex) return;

            if (pressed) StartThreshold();
            else ReleaseCharge();
        }

        // ─── 충전 시작 ───────────────────────────────────────────
        private void StartThreshold()
        {
            if (IsUsing) return;
            if (_thresholdCoroutine != null) StopCoroutine(_thresholdCoroutine);
            _thresholdCoroutine = StartCoroutine(ThresholdCoroutine());
        }

        private IEnumerator ThresholdCoroutine()
        {
            float elapsed = 0f;
            while (elapsed < chargeThreshold)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            _thresholdCoroutine = null;
            BeginCharge();
        }

        private void BeginCharge()
        {
            if (IsUsing) return;

            _currentTier = 0;
            ChargeProgress = 0f;
            LockMovement();

            if (chargeStartParam != null)
            {
                _renderer.PlayClip(chargeStartParam.ParamHash, 0.1f, 0f);
                _trigger.OnAnimationEnd += HandleStartEnd;
            }
            else
            {
                PlayLoop();
            }

            if (chargeLoopVfx != null)
                _vfxModule?.PlayVFX(chargeLoopVfx.AssetHash);

            if (_chargeCoroutine != null) StopCoroutine(_chargeCoroutine);
            _chargeCoroutine = StartCoroutine(ChargeCoroutine());
        }

        // Start 애니메이션 종료 → Loop 전환
        private void HandleStartEnd()
        {
            _trigger.OnAnimationEnd -= HandleStartEnd;
            PlayLoop();
        }

        private void PlayLoop()
        {
            if (chargeLoopParam != null)
                _renderer.PlayClip(chargeLoopParam.ParamHash, 0.15f, 0f);
        }

        // ─── 충전 Loop ───────────────────────────────────────────
        private IEnumerator ChargeCoroutine()
        {
            while (_currentTier < MaxTier)
            {
                float elapsed = 0f;
                while (elapsed < tierInterval)
                {
                    elapsed += Time.unscaledDeltaTime;
                    ChargeProgress = ((float)_currentTier + Mathf.Clamp01(elapsed / tierInterval)) / MaxTier;
                    yield return null;
                }

                _currentTier++;
                OnTierUpgrade(_currentTier);
            }

            ChargeProgress = 1f;
            while (true) yield return null;
        }

        private void OnTierUpgrade(int tier)
        {
            int idx = tier - 1;

            // Start 애니메이션이 아직 재생 중일 수 있으므로 구독 해제
            _trigger.OnAnimationEnd -= HandleStartEnd;
            _trigger.OnAnimationEnd -= HandleEnhanceEnd;

            if (tierUpgradeVfx != null && idx < tierUpgradeVfx.Length && tierUpgradeVfx[idx] != null)
                _vfxModule?.PlayVFX(tierUpgradeVfx[idx].AssetHash, transform.position, Quaternion.identity);

            // Enhance 애니메이션 재생 → 종료 시 Loop로 복귀
            if (chargeTierParams != null && idx < chargeTierParams.Length && chargeTierParams[idx] != null)
            {
                _renderer.PlayClip(chargeTierParams[idx].ParamHash, 0.1f, 0f);
                _trigger.OnAnimationEnd += HandleEnhanceEnd;
            }
            else
            {
                PlayLoop();
            }
        }

        // Enhance 애니메이션 종료 → Loop 복귀
        private void HandleEnhanceEnd()
        {
            _trigger.OnAnimationEnd -= HandleEnhanceEnd;
            if (_chargeCoroutine != null)
                PlayLoop();
        }

        // ─── 충전 종료 ───────────────────────────────────────────
        private void ReleaseCharge()
        {
            // 이 스킬이 시작한 세션(threshold 대기 or 충전 중)이 전혀 없으면 무시.
            // 다른 스킬 실행 중 탭했을 때 이전 충전의 잔여 상태(_currentTier)로 오진입하는 것을 방지.
            if (_thresholdCoroutine == null && _chargeCoroutine == null) return;

            // threshold 대기 중 뗌 → 차징 미시작, 콤보로 바로 위임
            if (_thresholdCoroutine != null)
            {
                StopCoroutine(_thresholdCoroutine);
                _thresholdCoroutine = null;
                _skillModule.RequestSkillUse(attackSkillIndex);
                return;
            }

            bool hasTier = _currentTier > 0;
            StopCharge();

            if (hasTier)
                ExecuteAttack();
            else
            {
                UnlockMovement();
                _skillModule.RequestSkillUse(attackSkillIndex);
            }
        }

        private void StopCharge()
        {
            if (_chargeCoroutine != null)
            {
                StopCoroutine(_chargeCoroutine);
                _chargeCoroutine = null;
            }

            _trigger.OnAnimationEnd -= HandleStartEnd;
            _trigger.OnAnimationEnd -= HandleEnhanceEnd;

            if (chargeLoopVfx != null)
                _vfxModule?.StopVFX(chargeLoopVfx.AssetHash);
        }

        // ─── 강타 실행 ───────────────────────────────────────────
        private void ExecuteAttack()
        {
            _pendingDamage = SkillData.damage + _currentTier * damagePerTier;
            base.UseSkill();

            Vector3 attackDir = (_moveData as PlayerMoveData)?.TargetMoveDir is { magnitude: > 0.1f } dir
                ? dir.normalized
                : _player.transform.forward;
            _player.transform.rotation = Quaternion.LookRotation(attackDir);

            if (chargeEndParam != null)
                _renderer.PlayClip(chargeEndParam.ParamHash, 0.3f, 0f);

            _trigger.OnAttack += HandleAttack;
            SubscribeAutoStopOnAnimationEnd();
        }

        private void HandleAttack()
        {
            _trigger.OnAttack -= HandleAttack;

            if (caster == null) return;
            caster.CurrentHitType = hitType;
            caster.SetDamageOverride(_pendingDamage);
            caster.OnHit += HandleHit;
            caster.CastOnce();
            caster.OnHit -= HandleHit;
        }

        private void HandleHit(DamageData data)
        {
            if (hitVfx != null)
                createChannel?.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(
                    hitVfx, data.HitPoint, Quaternion.LookRotation(data.HitNormal)));

            // 충전 단계가 높을수록 강한 타격감 — 즉각 동결 + 즉각 쉐이크
            TimeManager.Instance.HitStop(hitStopDuration * _currentTier, hitStopTimeScale);
            _cameraModule?.ShakeCamera(shakeForce * _currentTier, data.HitNormal);
        }

        public override void StopSkill()
        {
            UnlockMovement();
            _currentTier = 0;
            ChargeProgress = 0f;
            StopCharge();
            _trigger.OnAttack -= HandleAttack;
            UnsubscribeAutoStopOnAnimationEnd();
            base.StopSkill();
        }

        public override bool CanUseSkill(GameObject target = null) => false;
        public override void UseSkill(GameObject target = null) { }
    }
}
