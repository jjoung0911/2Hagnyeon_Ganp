using _00.Scripts.Enemy.BT;
using UnityEngine;

namespace Enemy
{
    public class MeleeEnemy : AbstractEnemy
    {
        protected override void Start()
        {
            base.Start();
            OnHit.AddListener(HandleHitEvent_Melee);
            OnDeath.AddListener(HandleDeathEvent);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnHit.RemoveListener(HandleHitEvent_Melee);
            OnDeath.RemoveListener(HandleDeathEvent);
        }

        private void HandleHitEvent_Melee()
        {
            StateChannel?.SendEventMessage(EnemyState.HIT);
        }

        private void HandleDeathEvent()
        {
            StateChannel?.SendEventMessage(EnemyState.DEATH);
        }
    }
}
