using System.Collections.Generic;
using Agents.Modules;
using JWLib.EventChannelSystem;
using System.StatSystem;
using UI.Events;
using UnityEngine;

namespace Player
{
    // 플레이어의 지정 스탯들을 구독해 값이 바뀔 때마다 playerChannel로 현재 스탯 스냅샷을 중계한다.
    // UI는 Player를 직접 참조하지 않고 PlayerStatsChangedEvent만 구독한다 (PlayerUIRelayModule과 동일 패턴).
    public class PlayerStatRelayModule : MonoBehaviour, IModule, IAfterInit
    {
        [SerializeField] private EventChannelSO playerChannel;
        [Tooltip("패널에 표시할 스탯 목록 — 표시 순서대로 등록")]
        [SerializeField] private StatSO[] trackedStats;

        private IAgentStat _stat;
        // 플레이어의 클론 StatSO들(값이 실시간 반영됨). 이벤트로 이 리스트 참조를 그대로 넘긴다.
        private readonly List<StatSO> _live = new();
        private readonly List<int> _subscribedIndices = new();

        public void Initialize(ModuleOwner owner) => _stat = owner.GetModule<IAgentStat>();

        public void AfterInit()
        {
            if (_stat == null || trackedStats == null) return;

            foreach (var so in trackedStats)
            {
                if (so == null || !_stat.TryGetStat(so.Index, out var live)) continue;
                _live.Add(live);
                _stat.SubscribeStat(so.Index, HandleStatChanged, live.Value);
                _subscribedIndices.Add(so.Index);
            }

            Raise();
        }

        private void OnDestroy()
        {
            if (_stat == null) return;
            foreach (var idx in _subscribedIndices)
                _stat.UnSubscribeStat(idx, HandleStatChanged);
        }

        private void HandleStatChanged(StatSO stat, float current, float previous) => Raise();

        private void Raise() => playerChannel.RaiseEvent(UIEventCache.PlayerStatsChanged.Init(_live));
    }
}
