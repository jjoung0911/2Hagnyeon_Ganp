using System.Collections;
using System.DifficultySystem;
using System.Leaderboard;
using System.Managers;
using JWLib.EventChannelSystem;
using Player;
using TMPro;
using UI.Leaderboard;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace System.GameFlow
{
    // 플레이어 사망 → 게임오버 흐름의 오케스트레이터.
    // PlayerDiedEvent를 구독해 난이도 진행 정지 → 적 슬로우모션 + 비네트 → 게임오버 패널을 띄운다.
    // 이름은 패널에서 편집만 하고, 재시작/타이틀로 "떠나는 순간" 단 한 번 리더보드에 커밋한다(익명 폴백).
    // 항상 활성인 오브젝트에 두어야 패널이 꺼져 있어도 PlayerDiedEvent를 받을 수 있다.
    public class GameOverController : MonoBehaviour
    {
        [Header("이벤트")]
        [SerializeField] private EventChannelSO playerChannel;

        [Header("패널 / UI")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private LeaderboardView leaderboardView;
        [SerializeField] private TextMeshProUGUI killText;
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button titleButton;
        [Tooltip("게임오버 중 ESC 일시정지를 막기 위해 비활성화할 PauseMenuController")]
        [SerializeField] private Behaviour pauseMenu;

        [Header("연출")]
        [SerializeField] private float slowMoTimeScale = 0.2f;
        [SerializeField] private float vignetteTargetIntensity = 0.5f;
        [SerializeField] private float vignetteLerpDuration = 0.6f;
        [SerializeField] private float panelDelay = 1.5f;
        [SerializeField] private int nameMaxLength = 8;

        [Header("씬")]
        [SerializeField] private string titleSceneName = "Title";

        private Volume _volume;
        private int _kills;
        private bool _isGameOver;
        private bool _committed;

        private void Awake()
        {
            playerChannel?.AddListener<PlayerDiedEvent>(HandlePlayerDied);

            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (nameInput != null) nameInput.characterLimit = nameMaxLength;

            _volume = FindAnyObjectByType<Volume>();
        }

        private void OnDestroy()
        {
            playerChannel?.RemoveListener<PlayerDiedEvent>(HandlePlayerDied);

            if (restartButton != null) restartButton.onClick.RemoveListener(HandleRestart);
            if (titleButton != null) titleButton.onClick.RemoveListener(HandleTitle);
            if (nameInput != null) nameInput.onValueChanged.RemoveListener(HandleNameChanged);
        }

        private void HandlePlayerDied(PlayerDiedEvent _)
        {
            if (_isGameOver) return;
            _isGameOver = true;

            // 난이도 진행 정지 + 사망 시점 처치 수 확정
            DifficultyManager.Instance.Stop();
            _kills = DifficultyManager.Instance.KillCount;

            // 적 슬로우모션 (UI는 unscaled로 동작하므로 영향 없음)
            TimeManager.Instance.SetTimeScale(slowMoTimeScale);

            // 게임오버 중 일시정지 차단
            if (pauseMenu != null) pauseMenu.enabled = false;

            StartCoroutine(GameOverRoutine());
        }

        private IEnumerator GameOverRoutine()
        {
            // 비네트 강조 (unscaled)
            float elapsed = 0f;
            float startIntensity = TryGetVignette(out Vignette vignette) ? vignette.intensity.value : 0f;
            while (elapsed < vignetteLerpDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (vignette != null)
                    vignette.intensity.value = Mathf.Lerp(startIntensity, vignetteTargetIntensity,
                        elapsed / vignetteLerpDuration);
                yield return null;
            }

            // 사망 애니메이션을 잠깐 보여준 뒤 패널 등장
            yield return new WaitForSecondsRealtime(panelDelay);

            ShowPanel();
        }

        private void ShowPanel()
        {
            // 커서 잠금 해제 (PauseMenuController와 동일 패턴)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (killText != null) killText.text = $"처치: {_kills}";

            if (nameInput != null)
            {
                nameInput.text = string.Empty;
                nameInput.onValueChanged.AddListener(HandleNameChanged);
            }

            if (restartButton != null) restartButton.onClick.AddListener(HandleRestart);
            if (titleButton != null) titleButton.onClick.AddListener(HandleTitle);

            // 이번 판 잠정 기록을 끼워 넣어 미리보기
            leaderboardView?.ShowProvisional(_kills, null);

            if (gameOverPanel != null) gameOverPanel.SetActive(true);
        }

        private void HandleNameChanged(string value)
        {
            leaderboardView?.ShowProvisional(_kills, value);
        }

        // 떠나는 순간 단 한 번 커밋 (익명 폴백)
        private void Commit()
        {
            if (_committed) return;
            _committed = true;
            LeaderboardService.Add(nameInput != null ? nameInput.text : null, _kills);
        }

        private void HandleRestart()
        {
            Commit();
            RestoreTimeScale();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void HandleTitle()
        {
            Commit();
            RestoreTimeScale();
            SceneManager.LoadScene(titleSceneName);
        }

        private void RestoreTimeScale()
        {
            // 새 씬의 TimeManager가 다시 1로 맞추지만, 로드 전 한 박자 방어
            TimeManager.Instance.SetTimeScale(1f);
            Time.timeScale = 1f;

            // EventChannelSO는 에셋이라 씬 리로드 후에도 살아남아, 이전 판의 마지막 이벤트가
            // 새 씬 구독자에게 리플레이된다(묵은 경험치/레벨업/선택창 이벤트 재발동). 로드 직전 캐시를 비운다.
            EventChannelSO.ClearAllCaches();
        }

        private bool TryGetVignette(out Vignette vignette)
        {
            vignette = null;
            return _volume != null && _volume.profile != null && _volume.profile.TryGet(out vignette);
        }
    }
}
