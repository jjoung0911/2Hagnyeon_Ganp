using Agents.Modules;
using JWLib.AnimationSystem;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using UnityEngine;

namespace Enemy.Boss
{
    // HP 비율을 감시해 Phase1 -> Phase2 전환만 책임진다.
    // 전환 시 짧은 무적, 포효 애니메이션, 충격파 VFX, BossPhaseChangedEvent 발행을 수행한다.
    public class BossPhaseController : MonoBehaviour, IModule, IAfterInit
    {
        [Header("페이즈 전환 설정")]
        [SerializeField] private float phaseThreshold = 0.5f;
        [SerializeField] private float invincibleDuration = 1.5f;

        [Header("포효 애니메이션")]
        [SerializeField] private AnimParamSO roarClip;
        [SerializeField] private float roarCrossDuration = 0.1f;

        [Header("이펙트")]
        [SerializeField] private PoolItemSO phaseTransitionVfx;
        [SerializeField] private EventChannelSO createChannel;
        [SerializeField] private EventChannelSO bossEventChannel;

        public BossPhase CurrentPhase { get; private set; } = BossPhase.Phase1;

        private HealthModule _health;
        private IRenderer _renderer;

        public void Initialize(ModuleOwner owner)
        {
            _health = owner.GetModule<HealthModule>();
            _renderer = owner.GetModule<IRenderer>();
        }

        public void AfterInit()
        {
            if (_health != null)
                _health.OnHealthChangeEvent += HandleHealthChanged;
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.OnHealthChangeEvent -= HandleHealthChanged;
        }

        private void HandleHealthChanged(float current, float previous, float max)
        {
            if (CurrentPhase != BossPhase.Phase1) return;
            if (max <= 0f) return;

            if (current / max <= phaseThreshold)
                EnterPhase2();
        }

        private void EnterPhase2()
        {
            CurrentPhase = BossPhase.Phase2;

            _health?.SetInvincible(invincibleDuration);

            if (roarClip != null)
                _renderer?.PlayClip(roarClip.ParamHash, roarCrossDuration);

            if (phaseTransitionVfx != null)
                createChannel?.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(
                    phaseTransitionVfx, transform.position, Quaternion.identity));

            bossEventChannel?.RaiseEvent(BossEvents.BossPhaseChanged.Init(CurrentPhase));
        }
    }
}
