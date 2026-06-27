using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace System.Leaderboard
{
    // 리더보드 기록 한 줄. JsonUtility 직렬화 대상이라 public 필드로 둔다.
    [Serializable]
    public class LeaderboardEntry
    {
        public string name;
        public int kills;
    }

    // JsonUtility는 최상위 List를 직렬화하지 못하므로 래퍼로 감싼다.
    [Serializable]
    internal class LeaderboardData
    {
        public List<LeaderboardEntry> entries = new();
    }

    // 로컬 리더보드 저장소(순수 C#, DI/싱글톤 없음).
    // persistentDataPath/leaderboard.json 에 {이름, 처치 수} 목록을 JSON으로 보관한다.
    // 정렬 기준: 처치 수 내림차순(동률 시 입력 순서 유지 — OrderByDescending은 안정 정렬).
    public static class LeaderboardService
    {
        private const string FileName = "leaderboard.json";
        private const string AnonymousName = "익명";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        // 저장된 기록을 처치 수 내림차순으로 정렬해 반환.
        public static List<LeaderboardEntry> GetSorted()
        {
            return Load().OrderByDescending(e => e.kills).ToList();
        }

        // 새 기록을 추가하고 정렬·저장한다. 추가된 엔트리를 반환(하이라이트용).
        // 이름이 비었으면 익명 처리.
        public static LeaderboardEntry Add(string name, int kills)
        {
            var entries = Load();

            var entry = new LeaderboardEntry
            {
                name = string.IsNullOrWhiteSpace(name) ? AnonymousName : name.Trim(),
                kills = kills
            };
            entries.Add(entry);

            var sorted = entries.OrderByDescending(e => e.kills).ToList();
            Save(sorted);
            return entry;
        }

        // 정렬된 목록에서 해당 엔트리의 1-based 순위. 못 찾으면 목록 크기 반환.
        public static int GetRank(IReadOnlyList<LeaderboardEntry> sorted, LeaderboardEntry entry)
        {
            for (int i = 0; i < sorted.Count; i++)
                if (ReferenceEquals(sorted[i], entry))
                    return i + 1;
            return sorted.Count;
        }

        private static List<LeaderboardEntry> Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return new List<LeaderboardEntry>();

                string json = File.ReadAllText(FilePath);
                var data = JsonUtility.FromJson<LeaderboardData>(json);
                return data?.entries ?? new List<LeaderboardEntry>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LeaderboardService] 로드 실패 — 빈 리더보드로 시작: {e.Message}");
                return new List<LeaderboardEntry>();
            }
        }

        private static void Save(List<LeaderboardEntry> entries)
        {
            try
            {
                var data = new LeaderboardData { entries = entries };
                File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LeaderboardService] 저장 실패: {e.Message}");
            }
        }
    }
}
