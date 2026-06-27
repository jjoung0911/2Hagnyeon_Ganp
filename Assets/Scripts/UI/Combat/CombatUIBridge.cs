using JWLib.EventChannelSystem;
using Player;
using Player.Skills;
using UI.Events;
using UI.Player;
using UnityEngine;

namespace UI.Combat
{
    public class CombatUIBridge : MonoBehaviour
    {
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private EventChannelSO uiChannel;
        [SerializeField] private PlayerUIBridge playerBridge;
        [SerializeField] private float comboResetWindow = 2f;

        private int _comboCount;
        private float _lastHitTime;
        private bool _comboActive;

        private void Awake()
        {
            playerChannel.AddListener<PlayerHitEvent>(HandlePlayerHit);
            playerChannel.AddListener<ParrySuccessEvent>(HandleParrySuccess);
            playerChannel.AddListener<ChargeAttackEvent>(HandleChargeReady);
        }

        private void OnDestroy()
        {
            playerChannel.RemoveListener<PlayerHitEvent>(HandlePlayerHit);
            playerChannel.RemoveListener<ParrySuccessEvent>(HandleParrySuccess);
            playerChannel.RemoveListener<ChargeAttackEvent>(HandleChargeReady);
        }

        private void Update()
        {
            if (!_comboActive) return;

            float elapsed = Time.unscaledTime - _lastHitTime;
            if (elapsed > comboResetWindow)
            {
                ResetCombo();
                return;
            }

            playerBridge.ViewModel.ComboTimeRatio = 1f - elapsed / comboResetWindow;
        }

        private void HandlePlayerHit(PlayerHitEvent evt)
        {
            _comboCount++;
            _lastHitTime = Time.unscaledTime;
            _comboActive = true;
            playerBridge.ViewModel.ComboCount = _comboCount;
            uiChannel.RaiseEvent(UIEventCache.ComboChanged.Init(_comboCount));
        }

        private void ResetCombo()
        {
            _comboCount  = 0;
            _comboActive = false;
            playerBridge.ViewModel.ComboCount    = 0;
            playerBridge.ViewModel.ComboTimeRatio = 1f;
            uiChannel.RaiseEvent(UIEventCache.ComboChanged.Init(0));
        }

        private void HandleParrySuccess(ParrySuccessEvent evt)
        {
            uiChannel.RaiseEvent(UIEventCache.CombatFeedback.Init("PARRY", FeedbackType.Parry));
        }

        private void HandleChargeReady(ChargeAttackEvent evt)
        {
            uiChannel.RaiseEvent(UIEventCache.CombatFeedback.Init("CHARGED", FeedbackType.Charged));
        }

        public void NotifySlice()
        {
            uiChannel.RaiseEvent(UIEventCache.CombatFeedback.Init("SLICE", FeedbackType.Slice));
        }

        public void NotifyCounter()
        {
            uiChannel.RaiseEvent(UIEventCache.CombatFeedback.Init("COUNTER", FeedbackType.Counter));
        }
    }
}
