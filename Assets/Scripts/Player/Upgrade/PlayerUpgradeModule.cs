using System;
using System.Collections.Generic;
using Agents.Modules;
using JWLib.EventChannelSystem;
using System.UpgradeSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Upgrade
{
    // Player 하위 GameObject에 부착하는 IModule.
    // LevelUpEvent를 받아 무작위 업그레이드 선택지를 제시하고, 선택된 업그레이드를 즉시 적용한다.
    public class PlayerUpgradeModule : MonoBehaviour, IModule, IAfterInit
    {
        [SerializeField] private UpgradeTableSO upgradeTable;
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private int choiceCount = 3;
        [SerializeReference] private IUpgradeSelector selector = new RandomUpgradeSelector();

        public IReadOnlyList<UpgradeDataSO> CurrentChoices => _currentChoices;

        private ModuleOwner _owner;
        private readonly UpgradeProgressTracker _progress = new();
        private UpgradeDataSO[] _currentChoices = Array.Empty<UpgradeDataSO>();

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
        }

        private void Update()
        {
            if(Keyboard.current.pKey.wasPressedThisFrame)
                Test();
        }

        private void Test()
        {
            playerChannel.RaiseEvent(UpgradeEvents.LevelUpEvent.Init(1));
        }
        public void AfterInit()
        {
            playerChannel.AddListener<LevelUpEvent>(HandleLevelUp);
            playerChannel.AddListener<UpgradeSelectedEvent>(HandleSelectUpgrade);
        }

        private void OnDestroy()
        {
            playerChannel.RemoveListener<LevelUpEvent>(HandleLevelUp);
            playerChannel.RemoveListener<UpgradeSelectedEvent>(HandleSelectUpgrade);
        }

        private void HandleLevelUp(LevelUpEvent evt)
        {
            // 이미 선택창이 열려 있으면 무시 — 중복 Pause() 방지
            if (_currentChoices.Length > 0) return;
            _currentChoices = selector.SelectChoices(upgradeTable.Upgrades, _owner, _progress, choiceCount);
            playerChannel.RaiseEvent(UpgradeEvents.UpgradeChoicesReadyEvent.Init(_currentChoices));
        }

        private void HandleSelectUpgrade(UpgradeSelectedEvent evt)
        {
            SelectUpgrade(evt);
        }


        // View가 플레이어의 선택을 전달할 때 호출 — 즉시 효과를 적용한다
        public void SelectUpgrade(UpgradeSelectedEvent evt)
        {
            var selected = evt.Selected;
            if (selected == null || Array.IndexOf(_currentChoices, selected) < 0) return;

            // CanApply 실패 시에도 Resume 보장 — 선택창 표시 이후 상태가 변한 경우 대비
            if (!selected.CanApply(_owner, _progress))
            {
                _currentChoices = Array.Empty<UpgradeDataSO>();
                playerChannel.RaiseEvent(UpgradeEvents.UpgradeAppliedEvent.Init(null, _owner));
                return;
            }

            selected.Apply(_owner, _progress);
            _progress.AddStack(selected.Index);
            _currentChoices = Array.Empty<UpgradeDataSO>();

            playerChannel.RaiseEvent(UpgradeEvents.UpgradeAppliedEvent.Init(selected, _owner));
        }
    }
}
