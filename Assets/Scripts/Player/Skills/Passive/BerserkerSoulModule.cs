using Agents.Modules;
using System.StatSystem;
using UnityEngine;

namespace Player.Skills.Passive
{
    // 광전사의 영혼 — 현재 체력이 낮을수록 공격력 스탯이 증가한다.
    // AgentStat의 공격력 스탯에 모디파이어를 적용하므로, 공격력을 참조하는 모든 스킬과
    // StatPanel(PlayerStatRelayModule)에 자동으로 반영된다.
    public class BerserkerSoulModule : AbstractPassiveSkill
    {
        [SerializeField] private StatSO atkStatSo;
        // 레벨별 최대 보너스 배율 (체력 0%일 때 적용되는 공격력 증가율, 기준 공격력 대비)
        [SerializeField] private float[] maxBonusByLevel = { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };

        private HealthModule _health;
        private IAgentStat _statModule;
        private readonly object _modifierKey = new();

        protected override void OnInitialize()
        {
            _health = Player.GetModule<HealthModule>();
            _statModule = Player.GetModule<IAgentStat>();
            if (_health != null) _health.OnHealthChangeEvent += HandleHealthChanged;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnHealthChangeEvent -= HandleHealthChanged;
            if (_statModule != null && atkStatSo != null) _statModule.RemoveModifier(atkStatSo.Index, _modifierKey);
        }

        private void HandleHealthChanged(float current, float previous, float max) => RecalculateBonus();

        protected override void OnLevelChanged(int level) => RecalculateBonus();

        private void RecalculateBonus()
        {
            if (_statModule == null || atkStatSo == null) return;

            _statModule.RemoveModifier(atkStatSo.Index, _modifierKey);

            if (Level <= 0 || _health == null || _health.MaxHp <= 0f) return;

            var stat = _statModule.GetStat(atkStatSo.Index);
            if (stat == null) return;

            float missingRatio = 1f - _health.CurrentHp / _health.MaxHp;
            float bonus = stat.BaseValue * maxBonusByLevel[Level - 1] * missingRatio;
            if (bonus > 0f) _statModule.AddModifier(atkStatSo.Index, _modifierKey, bonus);
        }

        public override string GetEffectDetail(int level)
        {
            if (level <= 0 || level > maxBonusByLevel.Length) return null;
            return $"체력 낮을수록 공격력 증가 (최대 +{maxBonusByLevel[level - 1] * 100f:0.##}%)";
        }
    }
}
