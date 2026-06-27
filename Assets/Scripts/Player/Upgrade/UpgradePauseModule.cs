using System.Managers;
using Agents.Modules;
using JWLib.EventChannelSystem;
using UnityEngine;

namespace Player.Upgrade
{
    // 업그레이드 선택창이 떠 있는 동안 게임을 일시정지한다.
    // UpgradeChoicesReadyEvent(선택창 표시) → 정지, UpgradeAppliedEvent(선택 완료) → 재개
    public class UpgradePauseModule : MonoBehaviour, IModule, IAfterInit
    {
        [SerializeField] private EventChannelSO playerChannel;

        public void Initialize(ModuleOwner owner) { }

        public void AfterInit()
        {
            playerChannel.AddListener<UpgradeChoicesReadyEvent>(HandleChoicesReady);
            playerChannel.AddListener<UpgradeAppliedEvent>(HandleUpgradeApplied);
        }

        private void OnDestroy()
        {
            playerChannel.RemoveListener<UpgradeChoicesReadyEvent>(HandleChoicesReady);
            playerChannel.RemoveListener<UpgradeAppliedEvent>(HandleUpgradeApplied);
        }

        private void HandleChoicesReady(UpgradeChoicesReadyEvent evt)
        {
            // 제시할 선택지가 없으면(전부 적용 불가) 정지하지 않음 — 재개 신호가 없어 멈춰버리는 것을 방지
            if (evt.Choices == null || evt.Choices.Length == 0) return;
            TimeManager.Instance.Pause();
        }

        private void HandleUpgradeApplied(UpgradeAppliedEvent evt)
        {
            TimeManager.Instance.Resume();
        }
    }
}
