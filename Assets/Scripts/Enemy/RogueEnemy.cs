using _00.Scripts.Enemy.BT;
using UnityEngine;

namespace Enemy
{
    public class RogueEnemy : AbstractEnemy
    {
        protected override void Start()
        {
            base.Start();
            OnHit.AddListener(HandleHitEvent);
            OnDeath.AddListener(HandleDeathEvent);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnHit.RemoveListener(HandleHitEvent);
            OnDeath.RemoveListener(HandleDeathEvent);
        }

        protected override void HandleHitEvent()
        {
            StateChannel?.SendEventMessage(EnemyState.HIT);
        }

        private void HandleDeathEvent()
        {
            StateChannel?.SendEventMessage(EnemyState.DEATH);
        }
    }
}
