using Agents.CombatSystem;
using _00.Scripts.Enemy.BT;
using UnityEngine;

namespace Enemy
{
    // 경타(Light)를 무시하고 중타(Heavy) 이상만 스태거 반응하는 탱커 에너미
    public class TankerEnemy : AbstractEnemy
    {
        protected override void Start()
        {
            base.Start();
            OnHit.AddListener(HandleHitEvent_Tanker);
            OnDeath.AddListener(HandleDeathEvent);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnHit.RemoveListener(HandleHitEvent_Tanker);
            OnDeath.RemoveListener(HandleDeathEvent);
        }

        private void HandleHitEvent_Tanker()
        {
            // ActionData는 TakeDamage에서 OnHit 발화 전에 이미 세팅됨
            if (ActionData != null && ActionData.HitType == HitType.Light) return;
            StateChannel?.SendEventMessage(EnemyState.HIT);
        }

        private void HandleDeathEvent()
        {
            StateChannel?.SendEventMessage(EnemyState.DEATH);
        }
    }
}
