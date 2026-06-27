using Agents.Modules;
using JWLib.EventChannelSystem;
using Player.LevelSystem;
using UnityEngine;

namespace Player.Skills.Passive
{
    // 피의 축제 — 적 처치 시 체력을 회복한다.
    public class BloodFeastModule : AbstractPassiveSkill
    {
        [SerializeField] private EventChannelSO eventChannel;
        // 레벨별 처치 시 회복량
        [SerializeField] private float[] healByLevel = { 3f, 6f, 9f, 12f, 15f };

        private HealthModule _health;

        protected override void OnInitialize()
        {
            _health = Player.GetModule<HealthModule>();
            eventChannel.AddListener<EnemyKilledEvent>(HandleEnemyKilled);
        }

        private void OnDestroy()
        {
            eventChannel.RemoveListener<EnemyKilledEvent>(HandleEnemyKilled);
        }

        private void HandleEnemyKilled(EnemyKilledEvent evt)
        {
            if (Level <= 0 || _health == null) return;
            _health.CurrentHp += healByLevel[Level - 1];
        }

        protected override void OnLevelChanged(int level) { }

        public override string GetEffectDetail(int level)
        {
            if (level <= 0 || level > healByLevel.Length) return null;
            return $"적 처치 시 체력 +{healByLevel[level - 1]:0.##}";
        }
    }
}
