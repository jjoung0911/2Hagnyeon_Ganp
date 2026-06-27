using Agents.Modules;
using System.StatSystem;
using UnityEngine;

namespace Player.Skills.Active

{
    // 발동형(Triggered) 스킬 — 쿨다운이 0 이하가 되면 자동으로 Activate() 1회 실행.
    public abstract class AbstractTriggeredSkill<TLevel> : AbstractActiveSkill where TLevel : struct
    {
        [SerializeField] private TriggeredSkillDataSO<TLevel> data;
        // 천상의 가호(Angel's Grace) 패시브 — 쿨타임 감소율 스탯
        [SerializeField] private StatSO cooldownReductionStatSo;

        protected TLevel CurrentLevelData => data.GetLevel(Level);
        protected float CooldownMultiplier { get; set; } = 1f;

        private float _cooldownTimer;
        private IAgentStat _stat;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _stat = Player.GetModule<IAgentStat>();
            if (_stat != null && cooldownReductionStatSo != null)
                CooldownMultiplier = 1f - _stat.SubscribeStat(cooldownReductionStatSo.Index, HandleCooldownReductionChanged, 0f);
        }

        private void HandleCooldownReductionChanged(StatSO stat, float current, float previous)
            => CooldownMultiplier = 1f - current;

        protected virtual void OnDestroy()
        {
            if (_stat != null && cooldownReductionStatSo != null)
                _stat.UnSubscribeStat(cooldownReductionStatSo.Index, HandleCooldownReductionChanged);
        }

        public sealed override void Tick(float dt)
        {
            _cooldownTimer -= dt;
            if (_cooldownTimer > 0f) return;

            Activate();
            Audio?.PlaySfx(activateSfx);
            _cooldownTimer = data.GetCooldown(Level) * CooldownMultiplier;
            playerChannel?.RaiseEvent(ActiveSkillEvents.ActiveSkillActivatedEvent.Init(SkillIndex));
        }

        // 쿨다운 만료 시 1회 실행할 동작 — 적 탐색, 투사체 발사 등
        protected abstract void Activate();

        protected sealed override void OnLevelChanged(int level)
        {
            if (level <= 0)
            {
                _cooldownTimer = 0f;
                return;
            }

            // 남은 쿨다운을 새 레벨의 쿨다운 비율에 맞춰 재조정
            float prevCooldown = data.GetCooldown(Mathf.Max(1, level - 1));
            if (_cooldownTimer > 0f && prevCooldown > 0f)
            {
                float ratio = _cooldownTimer / prevCooldown;
                _cooldownTimer = data.GetCooldown(level) * ratio;
            }

            OnLevelDataChanged(data.GetLevel(level));
        }

        // 레벨 변경 시 수치 캐시/풀 인스턴스 등을 갱신
        protected abstract void OnLevelDataChanged(TLevel levelData);
    }
}
