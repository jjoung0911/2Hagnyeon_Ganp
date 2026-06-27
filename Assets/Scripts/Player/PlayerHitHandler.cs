using Agents.Modules;
using Agents.Modules.Movement;
using JWLib.AnimationSystem;
using JWLib.EventChannelSystem;
using UnityEngine;

namespace Player
{
    public class PlayerHitHandler : AbstractHitHandler
    {
        [SerializeField] private float hitShakeForce = 0.5f;
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private AnimParamSO hitDirXParam;
        [SerializeField] private AnimParamSO hitDirYParam;
        
        private PlayerCameraModule _cameraModule;
        private IRootMotionDriver _rootMotionDriver;
        private IAnimationTrigger _trigger;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _rootMotionDriver = owner.GetModule<IRootMotionDriver>();
            _trigger = owner.GetModule<IAnimationTrigger>();
            _cameraModule = owner.GetModule<PlayerCameraModule>();
        }

        private bool _isDead;

        protected override void HandleDeath()
        {
            // 사망 후 추가 피격으로 OnDeath가 재발화돼도 연출/이벤트가 중복되지 않도록 가드
            if (_isDead) return;
            _isDead = true;

            // 사망 애니메이션(deathStateParam) + 사망 SFX 재생
            base.HandleDeath();

            // 게임오버 흐름 시작점 — GameOverController/PlayerDeathModule/CameraHandler가 구독
            playerChannel?.RaiseEvent(CombatEventCache.PlayerDied);
        }

        public override void HandleHit()
        {
            Vector3 hitDir = CurrentHitNormal;
            _cameraModule?.ShakeCamera(hitShakeForce, hitDir);
            playerChannel?.RaiseEvent(CombatEventCache.PlayerDamaged);
            _renderer.SetVector2(hitDirXParam, hitDirYParam, hitDir);
            PlayHitReaction(CurrentHitType, 0.05f, 0f, 1);

            hitDir.y = 0;

            // 중복 피격으로 재진입할 경우를 대비해 이전 구독을 먼저 해제
            _trigger.OnAnimationEnd -= HandleHitRecoveryEnd;
            _trigger.OnAnimationEnd += HandleHitRecoveryEnd;
            _rootMotionDriver.Begin(hitDir);
        }

        private void HandleHitRecoveryEnd()
        {
            _trigger.OnAnimationEnd -= HandleHitRecoveryEnd;
            _rootMotionDriver.End();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_trigger != null)
                _trigger.OnAnimationEnd -= HandleHitRecoveryEnd;
        }
    }
}
