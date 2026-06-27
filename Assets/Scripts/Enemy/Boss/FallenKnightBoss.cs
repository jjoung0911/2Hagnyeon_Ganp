using System.Collections;
using _00.Scripts.Enemy.BT;
using Enemy.BT;
using JWLib.EventChannelSystem;
using System.Managers;
using UnityEngine;

namespace Enemy.Boss
{
    // Fallen Knight 보스 전용 사망 시퀀스만 책임진다.
    // 이동/감지/스킬/체력/회전 등은 AbstractEnemy와 기존 모듈(BossPhaseController, BossPatternController, BossFaceTargetModule, 패턴 스킬)에 위임한다.
    public class FallenKnightBoss : AbstractEnemy, IBoss
    {
        [Header("무기 드롭")]
        [SerializeField] private Rigidbody weaponRigidbody;

        [Header("사망 연출 — 슬로우모션")]
        [SerializeField] private float slowMotionScale = 0.15f;
        [SerializeField] private float slowMotionDuration = 2.5f;

        [Header("보스 격파 알림")]
        [SerializeField] private EventChannelSO bossEventChannel;

        protected override void Start()
        {
            base.Start();
            OnDeath.AddListener(HandleBossDeath);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnDeath.RemoveListener(HandleBossDeath);
        }

        // 무기 드롭 -> (죽음 애니메이션/포즈는 EnemyHitHandler.HandleDeath가 재생) -> 슬로우모션 -> 격파 이벤트
        private void HandleBossDeath()
        {
            StateChannel?.SendEventMessage(EnemyState.DEATH);

            if (weaponRigidbody != null)
            {
                weaponRigidbody.transform.SetParent(null);
                weaponRigidbody.isKinematic = false;
            }

            // TimeManager.HandleTimeScale의 onComplete는 공유 이펙트 코루틴 슬롯에 묶여 있어
            // 슬로우모션 도중 플레이어가 다른 적을 공격해 HitStop이 호출되면 취소되어 콜백이 누락될 수 있다.
            // 보스 격파 알림은 별도 코루틴으로 분리해 항상 발행되도록 보장한다.
            TimeManager.Instance.HandleTimeScale(slowMotionScale, slowMotionDuration);
            StartCoroutine(RaiseBossDefeatedAfterDelay(slowMotionDuration));
        }

        private IEnumerator RaiseBossDefeatedAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            bossEventChannel?.RaiseEvent(BossEvents.BossDefeated);
        }
    }
}
