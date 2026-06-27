using System.Collections.Generic;
using System.Leaderboard;
using System.Text;
using TMPro;
using UnityEngine;

namespace UI.Leaderboard
{
    // 리더보드 목록을 단일 TextMeshProUGUI에 여러 줄로 렌더한다.
    // 게임오버 화면과 타이틀 화면 양쪽에서 재사용한다.
    // - Refresh()         : 저장된 기록만 표시 (타이틀)
    // - ShowProvisional() : 아직 커밋하지 않은 이번 판 기록을 끼워 넣어 강조 표시 (게임오버)
    public class LeaderboardView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI listText;
        [SerializeField] private int maxRows = 10;
        [SerializeField] private string highlightColorHex = "#FFD54A";
        [SerializeField] private string emptyMessage = "기록 없음";
        [Tooltip("켜지면(OnEnable) 자동으로 저장된 기록을 갱신 — 타이틀 화면 패널용")]
        [SerializeField] private bool refreshOnEnable = false;

        private void OnEnable()
        {
            if (refreshOnEnable) Refresh();
        }

        // 저장된 기록만 표시
        public void Refresh()
        {
            Render(LeaderboardService.GetSorted(), null);
        }

        // 이번 판 기록(미커밋)을 정렬 위치에 끼워 넣어 강조 표시. 이름 입력이 바뀔 때마다 호출.
        public void ShowProvisional(int kills, string name)
        {
            var ordered = LeaderboardService.GetSorted();

            var provisional = new LeaderboardEntry
            {
                name = string.IsNullOrWhiteSpace(name) ? "익명" : name.Trim(),
                kills = kills
            };

            // 처치 수 내림차순 유지 — 동률이면 기존 기록 뒤(이번 판이 최신)에 삽입
            int insertIdx = ordered.Count;
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].kills < kills)
                {
                    insertIdx = i;
                    break;
                }
            }
            ordered.Insert(insertIdx, provisional);

            Render(ordered, provisional);
        }

        private void Render(List<LeaderboardEntry> ordered, LeaderboardEntry highlight)
        {
            if (ordered.Count == 0)
            {
                listText.text = emptyMessage;
                return;
            }

            var sb = new StringBuilder();
            int shown = Mathf.Min(maxRows, ordered.Count);
            for (int i = 0; i < shown; i++)
                sb.AppendLine(FormatRow(i + 1, ordered[i], ReferenceEquals(ordered[i], highlight)));

            // 강조 기록이 상위 maxRows 밖이면 구분선과 함께 별도 줄로 덧붙인다
            if (highlight != null)
            {
                int idx = ordered.IndexOf(highlight);
                if (idx >= maxRows)
                {
                    sb.AppendLine("...");
                    sb.AppendLine(FormatRow(idx + 1, highlight, true));
                }
            }

            listText.text = sb.ToString().TrimEnd();
        }

        private string FormatRow(int rank, LeaderboardEntry entry, bool highlighted)
        {
            string name = string.IsNullOrEmpty(entry.name) ? "익명" : entry.name;
            string row = $"{rank,2}.  {name,-8}  처치 {entry.kills}";
            return highlighted ? $"<color={highlightColorHex}>{row}</color>" : row;
        }
    }
}
