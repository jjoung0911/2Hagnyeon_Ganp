using UnityEngine;

namespace System.GameModes
{
    // 게임 모드 종류. 타이틀에서 선택하며 게임 씬으로 이어진다.
    // 일반 모드는 삭제됨 — 현재는 무한 모드만 제공한다.
    public enum GameMode
    {
        Endless = 0, // 무한
        Event = 1,   // 이벤트
    }

    // 선택된 게임 모드를 씬 전환 사이에 유지한다.
    // 정적 필드는 씬 로드(LoadScene)에도 유지되며, PlayerPrefs로도 저장해 안전하게 복원한다.
    public static class GameModeContext
    {
        private const string PrefKey = "Game.SelectedMode";

        private static GameMode? _selected;

        public static GameMode Selected
        {
            get
            {
                _selected ??= (GameMode)PlayerPrefs.GetInt(PrefKey, (int)GameMode.Endless);
                return _selected.Value;
            }
            set
            {
                _selected = value;
                PlayerPrefs.SetInt(PrefKey, (int)value);
                PlayerPrefs.Save();
            }
        }
    }
}
