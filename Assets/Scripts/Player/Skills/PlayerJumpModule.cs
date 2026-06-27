using System;
using Agents.CombatSystem;
using Agents.Modules;
using csiimnida.CSILib.SoundManager.RunTime;
using JWLib.AnimationSystem;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using UnityEngine;

namespace Player.Skills
{
    public class PlayerJumpModule : AbstractPlayerSkill
    {
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private int lowerBodyLayer = 1;

        [Header("점프 애니메이션")]
        [SerializeField] private AnimParamSO combatJumpStartParam;
        [SerializeField] private AnimParamSO combatJumpLoopParam;
        [SerializeField] private AnimParamSO combatJumpLandParam;

        [Header("Anticipation — null이면 즉시 점프")]
        [SerializeField] private AnimParamSO anticipationParam;

        [Header("착지 연출")]
        [SerializeField] private Transform footTrm;
        [SerializeField] private PoolItemSO jumpVfx;
        
        
        [Header("착지 연출")]
        [SerializeField] private float landingShakeForce = 0.35f;
        // 착지 연출(카메라 흔들림, VFX)이 발생할 최소 낙하 높이
        [SerializeField] private float minFallHeightForEffect = 2f;
        [SerializeField] private PoolItemSO landingVfx;
        [SerializeField] private EventChannelSO createChannel;

        [Header("사운드")]
        [Tooltip("점프 시 재생할 SFX")]
        [SerializeField] private SoundSo jumpSfx;
        [Tooltip("착지 시 재생할 SFX")]
        [SerializeField] private SoundSo landSfx;

        private PlayerCameraModule _cameraModule;
        private Action _onAnticipationEnd;
        // 공중에 뜬 동안 도달한 최고 높이 — 착지 시 낙하 높이 계산에 사용
        private float _airbornePeakY;

        public bool IsAirborne { get; private set; }

        public override void Initialize(ISkillModule module)
        {
            base.Initialize(module);
            _cameraModule = _player.GetModule<PlayerCameraModule>();
            _moveData.OnGroundChanged += HandleGroundChanged;
        }

        private void OnDestroy()
        {
            _moveData.OnGroundChanged -= HandleGroundChanged;
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return _moveData.IsGrounded && !IsUsing;
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);

            if (anticipationParam != null)
            {
                _renderer?.PlayClip(anticipationParam.ParamHash, 0.05f, 0f, lowerBodyLayer);
                _onAnticipationEnd = () =>
                {
                    _trigger.OnAnimationEnd -= _onAnticipationEnd;
                    _onAnticipationEnd = null;
                    TryJump();
                };
                _trigger.OnAnimationEnd += _onAnticipationEnd;
            }
            else
            {
                TryJump();
            }
        }

        private void TryJump()
        {
            if (!_moveData.IsGrounded) { StopSkill(); return; }
            _moveData.SetVerticalVelocity(jumpForce);
            _audio?.PlaySfx(jumpSfx);

            if (combatJumpStartParam != null)
            {
                _renderer?.PlayClip(combatJumpStartParam.ParamHash, 0.1f, 0f, lowerBodyLayer);
                createChannel.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(jumpVfx, footTrm.position, Quaternion.identity));
            }
        }

        private void Update()
        {
            if (IsAirborne)
                _airbornePeakY = Mathf.Max(_airbornePeakY, _player.transform.position.y);
        }

        private void HandleGroundChanged(bool isGrounded)
        {
            if (!isGrounded)
            {
                IsAirborne = true;
                _airbornePeakY = _player.transform.position.y;

                // 대쉬 등 다른 스킬이 lowerBodyLayer를 점유 중이면 jump loop로 그 애니를 덮어쓰지 않는다.
                // (덮어쓰면 해당 스킬의 종료 애니메이션 이벤트가 소실되어 스킬이 영구 고착됨)
                if (!IsUsing && _skillModule.IsSkillActive)
                    return;

                if (combatJumpLoopParam != null)
                    _renderer?.PlayClip(combatJumpLoopParam.ParamHash, 0.15f, 0f, lowerBodyLayer);
            }
            else if (IsAirborne)
            {
                IsAirborne = false;

                float fallHeight = _airbornePeakY - _player.transform.position.y;
                if (fallHeight >= minFallHeightForEffect)
                {
                    _audio?.PlaySfx(landSfx);
                    _cameraModule?.ShakeCamera(landingShakeForce, Vector3.down * 2f);
                    createChannel.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(
                        landingVfx, transform.position, Quaternion.identity));
                }

                // 대쉬 등 다른 스킬이 lowerBodyLayer를 점유 중이면 그 스킬의 HandleSkillEnd가 idle 복구를 담당
                // → 착지 연출/복구로 간섭하지 않고 종료
                if (!IsUsing && _skillModule.IsSkillActive)
                    return;

                // 착지 연출 (연출용) — 곧바로 idle로 정착 블렌드되며, 이동/재점프 입력 시 즉시 덮어쓰기 가능
                if (combatJumpLandParam != null)
                    _renderer?.PlayClip(combatJumpLandParam.ParamHash, 0.05f, 0f, lowerBodyLayer);

                // 애니메이션 이벤트(OnLandEnd)에 의존하지 않고 지면 접촉 즉시 복구한다.
                // → Land() 이벤트가 클립에 없거나 CrossFade로 누락돼도 IsUsing/하반신 레이어가 고착되지 않음
                if (IsUsing)
                    StopSkill();              // 정상 흐름: 점프가 CurrentSkill → HandleSkillEnd → idle
                else if (!_skillModule.IsSkillActive)
                    _skillModule.PlayIdle();  // 점프가 외부 중단됐고 다른 스킬도 없음 → 직접 idle 복귀
            }
        }

        public override void StopSkill()
        {
            if (_onAnticipationEnd != null)
            {
                _trigger.OnAnimationEnd -= _onAnticipationEnd;
                _onAnticipationEnd = null;
            }

            base.StopSkill();
        }
    }
}
