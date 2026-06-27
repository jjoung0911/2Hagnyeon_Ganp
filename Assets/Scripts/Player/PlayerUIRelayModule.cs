using System;
using System.Collections.Generic;
using System.Linq;
using Agents.Modules;
using JWLib.EventChannelSystem;
using Player.Skills;
using UI.Events;
using UnityEngine;

namespace Player
{
    // HealthModule/UltGaugeModule/PlayerSkillModule의 값을 playerChannel/uiChannel 이벤트로 중계한다.
    // UI는 Player를 직렬화로 참조하지 않고 이 이벤트들만 구독해 값을 받는다.
    public class PlayerUIRelayModule : MonoBehaviour, IModule, IAfterInit
    {
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private EventChannelSO uiChannel;

        private HealthModule _health;
        private UltGaugeModule _ultGauge;
        private AbstractPlayerSkill[] _trackedSkills;
        private float[] _prevCooldowns;
        private bool[] _prevUnlocked;
        private PlayerChargeAttackSkill _chargeSkill;
        private bool _prevIsCharging;
        private float _prevChargeProgress;

        private PlayerDashSkill _dashSkill;
        private int _prevDashCharges = -1;
        private float _prevDashNormalizedCooldown = -1f;

        public void Initialize(ModuleOwner owner)
        {
            _health   = owner.GetModule<HealthModule>();
            _ultGauge = owner.GetModule<UltGaugeModule>();

            RegisterSkillTracking(owner.GetModule<PlayerSkillModule>());
        }

        public void AfterInit()
        {
            if (_health != null)
            {
                _health.OnHealthChangeEvent += HandleHpChanged;
                RaiseHp(_health.CurrentHp, _health.MaxHp);
            }

            if (_ultGauge != null)
            {
                _ultGauge.OnGaugeChanged += HandleUltGaugeChanged;
                _ultGauge.OnUltReady     += HandleUltReady;
                RaiseUlt(_ultGauge.CurrentGauge, _ultGauge.MaxGauge);
            }

            RaiseInitialSkillCooldowns();
            PollDashCharges();
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.OnHealthChangeEvent -= HandleHpChanged;

            if (_ultGauge != null)
            {
                _ultGauge.OnGaugeChanged -= HandleUltGaugeChanged;
                _ultGauge.OnUltReady     -= HandleUltReady;
            }
        }

        private void Update()
        {
            PollSkillCooldowns();
            PollChargeGauge();
            PollDashCharges();
        }

        private void RegisterSkillTracking(PlayerSkillModule skillModule)
        {
            if (skillModule == null)
            {
                _trackedSkills = Array.Empty<AbstractPlayerSkill>();
                _prevCooldowns = Array.Empty<float>();
                _prevUnlocked = Array.Empty<bool>();
                return;
            }

            _dashSkill = skillModule.GetSkill<PlayerDashSkill>();
            _chargeSkill = skillModule.GetSkill<PlayerChargeAttackSkill>();

            _trackedSkills = skillModule.AllSkills
                .Where(s => s.ShowInHUD)
                .OrderBy(s => s.SkillData.hudOrder)
                .ToArray();
            _prevCooldowns = new float[_trackedSkills.Length];
            _prevUnlocked  = new bool[_trackedSkills.Length];
        }

        private void RaiseInitialSkillCooldowns()
        {
            for (int i = 0; i < _trackedSkills.Length; i++)
            {
                var skill = _trackedSkills[i];
                float normalized = skill.NormalizedCooldown;
                _prevCooldowns[i] = normalized;
                _prevUnlocked[i]  = skill.IsUnlocked;
                RaiseSkillCooldown(i, skill, normalized);
            }
        }

        private void PollSkillCooldowns()
        {
            for (int i = 0; i < _trackedSkills.Length; i++)
            {
                var skill = _trackedSkills[i];
                float current   = skill.NormalizedCooldown;
                bool  unlocked  = skill.IsUnlocked;
                bool changed = !Mathf.Approximately(current, _prevCooldowns[i])
                               || unlocked != _prevUnlocked[i];
                if (!changed) continue;
                _prevCooldowns[i] = current;
                _prevUnlocked[i]  = unlocked;
                RaiseSkillCooldown(i, skill, current);
            }
        }

        private void RaiseSkillCooldown(int slotIndex, AbstractPlayerSkill skill, float normalized)
        {
            float remaining = skill.SkillData.cooldown * (1f - normalized);
            // 슬롯마다 별도 인스턴스를 새로 만든다. UIEventCache의 공유 인스턴스를 재사용하면
            // EventChannelSO의 슬롯별 리플레이 캐시가 모두 같은 객체를 참조하게 되어
            // 마지막으로 갱신된 슬롯 값으로 덮어써지는 문제가 있다.
            playerChannel.RaiseEvent(new SkillCooldownChangedEvent().Init(
                slotIndex, skill.SkillData.skillIndex, skill.SkillData.skillName,
                skill.SkillData.icon, normalized, remaining, normalized >= 1f, skill.IsUnlocked));
        }

        private void PollDashCharges()
        {
            if (_dashSkill == null) return;

            int currentCharges = _dashSkill.CurrentCharges;
            float normalized = _dashSkill.NormalizedCooldown;
            if (currentCharges == _prevDashCharges && Mathf.Approximately(normalized, _prevDashNormalizedCooldown)) return;
            _prevDashCharges = currentCharges;
            _prevDashNormalizedCooldown = normalized;

            playerChannel.RaiseEvent(UIEventCache.DashChargeChanged.Init(currentCharges, _dashSkill.MaxCharges, normalized));
        }

        private void PollChargeGauge()
        {
            if (_chargeSkill == null) return;

            bool isActive = _chargeSkill.IsCharging || _chargeSkill.IsChargeFull;
            float progress = _chargeSkill.ChargeProgress;

            if (isActive == _prevIsCharging && Mathf.Approximately(progress, _prevChargeProgress)) return;
            _prevIsCharging     = isActive;
            _prevChargeProgress = progress;

            playerChannel.RaiseEvent(UIEventCache.ChargeProgress.Init(progress, isActive, progress >= 1f));
        }

        private void HandleHpChanged(float current, float previous, float max) => RaiseHp(current, max);

        private void RaiseHp(float current, float max)
            => playerChannel.RaiseEvent(UIEventCache.PlayerHpChanged.Init(current, max));

        private void HandleUltGaugeChanged(float current, float max) => RaiseUlt(current, max);

        private void RaiseUlt(float current, float max)
            => playerChannel.RaiseEvent(UIEventCache.UltGaugeChanged.Init(current, max, current >= max));

        private void HandleUltReady()
            => uiChannel.RaiseEvent(UIEventCache.CombatFeedback.Init("ULT READY", FeedbackType.UltReady));
    }
}
