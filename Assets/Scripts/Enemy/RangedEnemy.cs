using _00.Scripts.Enemy.BT;
using Agents.CombatSystem;
using Enemy.BT;
using Unity.Behavior;
using UnityEngine;

namespace Enemy
{
    public class RangedEnemy : AbstractEnemy
    {
        protected override void Start()
        {
            base.Start();
            OnHit.AddListener(HandleHitEvent_Ranged);
            OnDeath.AddListener(HandleDeathEvent);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnHit.RemoveListener(HandleHitEvent_Ranged);
            OnDeath.RemoveListener(HandleDeathEvent);
        }

        private void HandleHitEvent_Ranged()
        {
            // Light 히트는 BT에 HIT 상태를 전달하지 않음
            // (HIT 브랜치가 즉시 종료되며 다시 전달되기 때문에, 콤보 중 매 타격마다
            //  BT가 중단·재시작되어 경로 재탐색·애니메이션 재생 등이 반복되어 렉이 발생함)
            // 포이즈가 누적되어 실제 경직이 발생하면 HandleStaggerBegin에서 HIT가 전달됨
            if (ActionData != null && ActionData.HitType == HitType.Light) return;

            // 이미 HIT 상태인 경우 중복 전달 방지
            if (GetVariable(BtVars.State, out BlackboardVariable<EnemyState> stateVar) &&
                stateVar.Value == EnemyState.HIT)
                return;

            StateChannel?.SendEventMessage(EnemyState.HIT);
        }

        private void HandleDeathEvent()
        {
            StateChannel?.SendEventMessage(EnemyState.DEATH);
        }
    }
}
