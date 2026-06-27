using Agents.Modules;
using JWLib.EventChannelSystem;
using Player.Upgrade;
using System.StatSystem;
using UnityEngine;

namespace Player.LevelSystem
{
    // Player 하위 GameObject에 부착하는 IModule.
    // 경험치 누적과 레벨업 판정을 담당한다.
    // 레벨업 시 Player.Upgrade.LevelUpEvent를 발행 — PlayerUpgradeModule이 이를 구독해
    // 업그레이드 선택창을 띄운다 (시스템 간 직접 참조 없이 이벤트로만 연결).
    public class PlayerLevelModule : MonoBehaviour, IModule, IAfterInit
    {
        [SerializeField] private ExpTableSO expTable;
        [SerializeField] private EventChannelSO eventChannel;
        [SerializeField] private int startingLevel = 1;
        // 천공의 지혜(Wisdom of Heaven) 패시브 — 경험치 획득량 배율 스탯
        [SerializeField] private StatSO expMultiplierStatSo;

        public int CurrentLevel { get; private set; }
        public int CurrentExp { get; private set; }
        public int RequiredExp => expTable.GetRequiredExp(CurrentLevel);

        private IAgentStat _stat;

        public void Initialize(ModuleOwner owner)
        {
            CurrentLevel = Mathf.Max(1, startingLevel);
            CurrentExp = 0;
            _stat = owner.GetModule<IAgentStat>();
        }

        public void AfterInit()
        {
            eventChannel.AddListener<EnemyKilledEvent>(HandleEnemyKilled);

            // UI(PlayerUIBridge)가 직렬화된 Player 참조 없이 초기 경험치/레벨 값을 받도록
            // 시작 시점에 한 번 발행한다
            eventChannel.RaiseEvent(LevelEvents.ExpChangedEvent.Init(CurrentExp, RequiredExp, CurrentLevel));
        }

        private void OnDestroy()
        {
            eventChannel.RemoveListener<EnemyKilledEvent>(HandleEnemyKilled);
        }

        private void HandleEnemyKilled(EnemyKilledEvent evt) => AddExp(evt.ExpReward);

        public void AddExp(int amount)
        {
            if (amount <= 0) return;

            if (_stat != null && expMultiplierStatSo != null && _stat.TryGetStat(expMultiplierStatSo.Index, out var expStat))
                amount = Mathf.RoundToInt(amount * expStat.Value);

            CurrentExp += amount;
            while (CurrentExp >= RequiredExp)
            {
                CurrentExp -= RequiredExp;
                CurrentLevel++;
                eventChannel.RaiseEvent(UpgradeEvents.LevelUpEvent.Init(CurrentLevel));
            }

            eventChannel.RaiseEvent(LevelEvents.ExpChangedEvent.Init(CurrentExp, RequiredExp, CurrentLevel));
        }
    }
}
