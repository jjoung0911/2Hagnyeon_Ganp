using System.Collections;
using Agents.Modules;
using JWLib.EventChannelSystem;
using Player.Skills;
using System.StatSystem;
using UnityEngine;

namespace Player
{
    // 공격 버프(차지 등) 상태를 IAgentStat을 통해 관리하는 모듈.
    // 버프 적용 → 데미지 스탯에 AddModifier, 공격 종료 → RemoveModifier.
    public class PlayerAttackBuffModule : MonoBehaviour, IModule
    {
        [SerializeField] private StatSO damageStat;
        [SerializeField] private EventChannelSO playerChannel;

        private IAgentStat _agentStat;
        private static readonly object DashBonusKey = new object();
        private Coroutine _dashBonusCoroutine;

        public float CurrentDamage => _agentStat?.GetStat(damageStat.Index)?.Value ?? damageStat.BaseValue;

        public void Initialize(ModuleOwner owner)
        {
            _agentStat = owner.GetModule<IAgentStat>();
            playerChannel?.AddListener<ChargeAttackEvent>(OnChargeAttackEvent);
        }

        private void OnDestroy()
        {
            playerChannel?.RemoveListener<ChargeAttackEvent>(OnChargeAttackEvent);
        }

        private void OnChargeAttackEvent(ChargeAttackEvent e)
        {
            _agentStat?.AddModifier(damageStat.Index, this, e.DamageBonus);
        }

        public void ResetBuff()
        {
            _agentStat?.RemoveModifier(damageStat.Index, this);
        }

        // 대쉬 직후 짧은 시간 동안 데미지 보너스 적용 — duration 내 첫 공격에 자동 소비
        public void ApplyDashBonus(float bonus, float duration)
        {
            if (_dashBonusCoroutine != null) StopCoroutine(_dashBonusCoroutine);
            _agentStat?.RemoveModifier(damageStat.Index, DashBonusKey);
            _agentStat?.AddModifier(damageStat.Index, DashBonusKey, bonus);
            _dashBonusCoroutine = StartCoroutine(DashBonusExpire(duration));
        }

        // duration 후 자동 만료 — 별도 키(DashBonusKey)로 관리하므로 차징 버프와 독립 동작
        private IEnumerator DashBonusExpire(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            _agentStat?.RemoveModifier(damageStat.Index, DashBonusKey);
            _dashBonusCoroutine = null;
        }
    }
}
