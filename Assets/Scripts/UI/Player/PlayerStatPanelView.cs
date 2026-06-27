using System.Collections.Generic;
using JWLib.EventChannelSystem;
using UI.Events;
using UnityEngine;

namespace UI.Player
{
    // 플레이어의 현재 스탯을 표시하는 uGUI 패널. playerChannel의 PlayerStatsChangedEvent만 구독한다.
    // 행은 rowPrefab을 풀링해 생성하며, 스탯 수만큼 활성화한다.
    public class PlayerStatPanelView : MonoBehaviour
    {
        [SerializeField] private EventChannelSO playerChannel;
        [Tooltip("행이 들어갈 부모(VerticalLayoutGroup 권장)")]
        [SerializeField] private RectTransform rowsParent;
        [SerializeField] private StatRowView rowPrefab;

        private readonly List<StatRowView> _rows = new();

        private void Start()
        {
            // EventChannelSO는 구독 시 마지막 발행 값을 즉시 전달하므로 최초 표시도 자동으로 채워진다.
            playerChannel.AddListener<PlayerStatsChangedEvent>(HandleStatsChanged);
        }

        private void OnDestroy()
        {
            playerChannel.RemoveListener<PlayerStatsChangedEvent>(HandleStatsChanged);
        }

        private void HandleStatsChanged(PlayerStatsChangedEvent evt)
        {
            var stats = evt.Stats;
            if (stats == null || rowPrefab == null || rowsParent == null) return;

            while (_rows.Count < stats.Count)
                _rows.Add(Instantiate(rowPrefab, rowsParent));

            for (int i = 0; i < _rows.Count; i++)
            {
                bool active = i < stats.Count;
                if (_rows[i].gameObject.activeSelf != active)
                    _rows[i].gameObject.SetActive(active);
                if (active)
                    _rows[i].Bind(stats[i]);
            }
        }
    }
}
