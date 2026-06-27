using System.Managers;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Menu
{
    // 일시정지 메뉴 컨트롤러. UGUI(Canvas + Button) 기반.
    // ESC 키로 토글하며 Time.timeScale을 0으로 만들어 게임을 멈춘다.
    // 플레이어 액션맵(Controls)을 거치지 않고 Keyboard.current로 직접 ESC를 읽는다 —
    // 메뉴 토글은 게임플레이 입력과 무관해 InputActions 에셋을 건드리지 않기 위함.
    // Inspector:
    //   panelRoot       — Open/Close 시 활성/비활성 토글할 패널 GameObject
    //                      (이 컴포넌트가 붙은 오브젝트는 항상 활성 상태 유지해야 ESC 입력을 받을 수 있음)
    //   settingsPanel   — 설정 버튼에서 열 SettingsPanelController (같은 씬의 SettingsPanel)
    //   titleSceneName  — "타이틀로" 클릭 시 로드할 씬 이름
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private SettingsPanelController settingsPanel;
        [SerializeField] private string titleSceneName = "Title";

        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button titleButton;

        [Header("사운드")]
        [SerializeField] private SoundSo clickSfx;
        [SerializeField] private SoundSo pauseOpenSfx;
        [SerializeField] private SoundSo pauseCloseSfx;

        private bool _isPaused;
        private float _previousTimeScale = 1f;

        private void OnEnable()
        {
            resumeButton?.onClick.AddListener(Resume);
            settingsButton?.onClick.AddListener(HandleSettingsClicked);
            titleButton?.onClick.AddListener(HandleTitleClicked);
            resumeButton?.onClick.AddListener(PlayClickSfx);
            settingsButton?.onClick.AddListener(PlayClickSfx);
            titleButton?.onClick.AddListener(PlayClickSfx);
        }

        private void OnDisable()
        {
            resumeButton?.onClick.RemoveListener(Resume);
            settingsButton?.onClick.RemoveListener(HandleSettingsClicked);
            titleButton?.onClick.RemoveListener(HandleTitleClicked);
            resumeButton?.onClick.RemoveListener(PlayClickSfx);
            settingsButton?.onClick.RemoveListener(PlayClickSfx);
            titleButton?.onClick.RemoveListener(PlayClickSfx);

            // 메뉴 오브젝트가 비활성화되는 와중에 타임스케일·커서 상태가 고착되지 않도록 방어
            if (_isPaused)
            {
                Time.timeScale = _previousTimeScale;
                // 업그레이드 선택 등 다른 시스템이 여전히 일시정지 중이면 커서를 숨기지 않는다
                bool stillPaused = TimeManager.Instance != null && TimeManager.Instance.IsPaused;
                if (!stillPaused)
                {
                    UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                    UnityEngine.Cursor.visible = false;
                }
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                TogglePause();
        }

        private void TogglePause()
        {
            if (_isPaused) Resume();
            else Pause();
        }

        private void PlayClickSfx()
        {
            if (clickSfx != null) SoundManager.Instance.PlaySound(clickSfx);
        }

        private void Pause()
        {
            _isPaused = true;
            _previousTimeScale = Time.timeScale;
            TimeManager.Instance.Pause();
            if (pauseOpenSfx != null) SoundManager.Instance.PlaySound(pauseOpenSfx);

            // PlayerCameraModule이 게임플레이 중 커서를 Locked로 고정해두므로,
            // 메뉴에서 마우스 클릭이 통하도록 잠금을 풀고 커서를 보여준다.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 잠금 중 누적된 InputSystem 포인터 위치를 화면 중앙으로 리셋.
            // 리셋하지 않으면 UI 드래그 기준점이 화면 밖에 남아 슬라이더가 정상 동작하지 않는다.
            UnityEngine.InputSystem.Mouse.current?.WarpCursorPosition(
                new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));

            panelRoot?.SetActive(true);
        }

        private void Resume()
        {
            _isPaused = false;
            TimeManager.Instance.Resume();
            settingsPanel?.Close();
            panelRoot?.SetActive(false);

            // 업그레이드 선택 등 다른 시스템이 여전히 게임을 멈춰 마우스를 필요로 하면 커서를 숨기지 않는다.
            // (TimeManager는 일시정지를 refcount로 관리 — Resume() 후에도 잔여 정지가 있으면 IsPaused가 true)
            if (!TimeManager.Instance.IsPaused)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (pauseCloseSfx != null) SoundManager.Instance.PlaySound(pauseCloseSfx);
        }

        private void HandleSettingsClicked() => settingsPanel?.Open();

        private void HandleTitleClicked()
        {
            TimeManager.Instance.Resume();
            // 채널(SO)에 남은 이전 판 이벤트가 다음 씬으로 리플레이되지 않도록 캐시를 비운다
            JWLib.EventChannelSystem.EventChannelSO.ClearAllCaches();
            SceneManager.LoadScene(titleSceneName);
        }
    }
}
