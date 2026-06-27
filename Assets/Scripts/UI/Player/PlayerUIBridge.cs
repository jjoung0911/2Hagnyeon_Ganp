using JWLib.EventChannelSystem;
using Player.LevelSystem;
using UI.Events;
using UI.ViewModels;
using UnityEngine;

namespace UI.Player
{
    // HUD View들의 OnEnable보다 ViewModel Clone(Awake)이 먼저 실행되도록 우선순위를 둔다
    // (DependencyInjector와 동일한 DefaultExecutionOrder(-10) 컨벤션).
    // Player를 직렬화로 참조하지 않고, PlayerUIRelayModule/PlayerLevelModule이 발행하는
    // playerChannel 이벤트만 구독해 ViewModel을 채운다.
    [DefaultExecutionOrder(-10)]
    public class PlayerUIBridge : MonoBehaviour
    {
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private StatViewModel viewModelTemplate;

        // 다른 컴포넌트의 Start()보다 먼저 ViewModel이 준비되도록 Awake에서 Clone한다
        // (AgentStat.Initialize()가 statOverrides를 Clone해 보관하는 타이밍과 동일한 패턴).
        public StatViewModel ViewModel { get; private set; }

        private void Awake()
        {
            ViewModel = (StatViewModel)viewModelTemplate.Clone();

            playerChannel.AddListener<PlayerHpChangedEvent>(HandleHpChanged);
            playerChannel.AddListener<SkillCooldownChangedEvent>(HandleSkillCooldownChanged);
            playerChannel.AddListener<ChargeProgressEvent>(HandleChargeProgressChanged);
            playerChannel.AddListener<ExpChangedEvent>(HandleExpChanged);
        }

        private void OnDestroy()
        {
            playerChannel.RemoveListener<PlayerHpChangedEvent>(HandleHpChanged);
            playerChannel.RemoveListener<SkillCooldownChangedEvent>(HandleSkillCooldownChanged);
            playerChannel.RemoveListener<ChargeProgressEvent>(HandleChargeProgressChanged);
            playerChannel.RemoveListener<ExpChangedEvent>(HandleExpChanged);
        }

        private void HandleHpChanged(PlayerHpChangedEvent evt)
        {
            ViewModel.MaxHp = evt.Max;
            ViewModel.CurrentHp = evt.Current;
        }

        private void HandleSkillCooldownChanged(SkillCooldownChangedEvent evt)
        {
            var slots = ViewModel.SkillCooldowns;
            if (evt.SlotIndex < 0 || evt.SlotIndex >= slots.Count) return;

            var slot = slots[evt.SlotIndex];
            slot.SkillIndex         = evt.SkillIndex;
            slot.SkillName          = evt.SkillName;
            slot.Icon               = evt.Icon;
            slot.NormalizedCooldown = evt.NormalizedCooldown;
            slot.RemainingSeconds   = evt.RemainingSeconds;
            slot.IsReady            = evt.IsReady;
        }

        private void HandleChargeProgressChanged(ChargeProgressEvent evt)
        {
            ViewModel.IsCharging     = evt.IsCharging;
            ViewModel.ChargeProgress = evt.Progress;
        }

        private void HandleExpChanged(ExpChangedEvent evt)
        {
            ViewModel.Level       = evt.Level;
            ViewModel.CurrentExp  = evt.CurrentExp;
            ViewModel.RequiredExp = evt.RequiredExp;
        }
    }
}
