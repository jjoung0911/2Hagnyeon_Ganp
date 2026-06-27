using csiimnida.CSILib.SoundManager.RunTime;
using System.GameModes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Menu
{
    // 타이틀 화면 컨트롤러. UGUI(Canvas + Button) 기반.
    // 일반 모드는 삭제되어 모드 선택 없이 "게임 시작" 클릭 시 곧바로 무한 모드로 진입한다.
    // Inspector:
    //   settingsPanel  — 설정 버튼에서 열 SettingsPanelController (같은 씬의 SettingsPanel)
    //   gameSceneName  — "시작하기" 클릭 시 로드할 씬 이름
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private SettingsPanelController settingsPanel;
        [SerializeField] private string gameSceneName = "Work Scene";

        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("사운드")]
        [SerializeField] private SoundSo clickSfx;
        [SerializeField] private SoundSo bgmTitle;

        private void OnEnable()
        {
            // 타이틀은 항상 마우스로 조작한다. 게임 플레이 중 잠긴 커서가 남아 클릭이 막히지 않도록 해제한다.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            startButton?.onClick.AddListener(HandleStartClicked);
            settingsButton?.onClick.AddListener(HandleSettingsClicked);
            quitButton?.onClick.AddListener(HandleQuitClicked);
            startButton?.onClick.AddListener(PlayClickSfx);
            settingsButton?.onClick.AddListener(PlayClickSfx);
            quitButton?.onClick.AddListener(PlayClickSfx);

            if (bgmTitle != null) SoundManager.Instance.PlayBGM(bgmTitle);
        }

        private void OnDisable()
        {
            startButton?.onClick.RemoveListener(HandleStartClicked);
            settingsButton?.onClick.RemoveListener(HandleSettingsClicked);
            quitButton?.onClick.RemoveListener(HandleQuitClicked);
            startButton?.onClick.RemoveListener(PlayClickSfx);
            settingsButton?.onClick.RemoveListener(PlayClickSfx);
            quitButton?.onClick.RemoveListener(PlayClickSfx);
        }

        private void PlayClickSfx()
        {
            if (clickSfx != null) SoundManager.Instance.PlaySound(clickSfx);
        }

        private void HandleStartClicked()
        {
            GameModeContext.Selected = GameMode.Endless;
            SceneManager.LoadScene(gameSceneName);
        }

        private void HandleSettingsClicked()
        {
            settingsPanel?.Open();
        }

        private void HandleQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
