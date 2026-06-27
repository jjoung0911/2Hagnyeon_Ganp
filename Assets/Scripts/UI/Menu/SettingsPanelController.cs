using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace UI.Menu
{
    // 설정 패널 컨트롤러. UGUI(Slider/TMP_Dropdown/Toggle/Button) 기반.
    // 마스터/음악/효과음 볼륨(AudioMixer) / 그래픽 품질 / 전체화면을 PlayerPrefs에 저장하고 시작 시 복원한다.
    // 메인 메뉴와 일시정지 메뉴 양쪽에서 Open()/Close()로 공유해 사용한다 (DRY).
    // Inspector:
    //   panelRoot — Open/Close 시 활성/비활성 토글할 패널 GameObject (이 컴포넌트가 붙은 오브젝트는 항상 활성 상태 유지)
    //   audioMixer — 노출 파라미터 "Master" / "Music" / "SFX"를 가진 AudioMixer
    public class SettingsPanelController : MonoBehaviour
    {
        private const string MasterVolumeKey = "Settings.MasterVolume";
        private const string MusicVolumeKey = "Settings.MusicVolume";
        private const string SfxVolumeKey = "Settings.SfxVolume";
        private const string QualityKey = "Settings.QualityIndex";
        private const string FullscreenKey = "Settings.Fullscreen";

        private const string MasterParam = "Master";
        private const string MusicParam = "BGM";
        private const string SfxParam = "SFX";

        // 볼륨 0(무음) 처리를 위한 최소 dB 값
        private const float MinDecibel = -80f;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Button closeButton;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
            }

            LoadSettings();
        }

        private void Start()
        {
            // AudioMixer.SetFloat은 믹서가 로드된 직후 한두 프레임 동안 적용에 실패(반환값 false)할 수 있다(Unity 제약).
            // 게임 시작 시 저장된 볼륨이 확실히 반영되도록, 적용에 성공할 때까지 프레임을 넘기며 재시도한다.
            StartCoroutine(ApplySavedVolumesWhenReady());
        }

        private IEnumerator ApplySavedVolumesWhenReady()
        {
            float master = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            float music = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
            float sfx = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);

            for (int frame = 0; frame < 10; frame++)
            {
                // 비단락 &로 세 파라미터를 매 시도마다 모두 적용(멱등)하고, 모두 성공하면 종료
                bool applied = ApplyVolume(MasterParam, master)
                             & ApplyVolume(MusicParam, music)
                             & ApplyVolume(SfxParam, sfx);
                if (applied) yield break;
                yield return null;
            }
        }

        private void OnEnable()
        {
            masterVolumeSlider?.onValueChanged.AddListener(HandleMasterVolumeChanged);
            musicVolumeSlider?.onValueChanged.AddListener(HandleMusicVolumeChanged);
            sfxVolumeSlider?.onValueChanged.AddListener(HandleSfxVolumeChanged);
            qualityDropdown?.onValueChanged.AddListener(HandleQualityChanged);
            fullscreenToggle?.onValueChanged.AddListener(HandleFullscreenChanged);
            closeButton?.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            masterVolumeSlider?.onValueChanged.RemoveListener(HandleMasterVolumeChanged);
            musicVolumeSlider?.onValueChanged.RemoveListener(HandleMusicVolumeChanged);
            sfxVolumeSlider?.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
            qualityDropdown?.onValueChanged.RemoveListener(HandleQualityChanged);
            fullscreenToggle?.onValueChanged.RemoveListener(HandleFullscreenChanged);
            closeButton?.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            // PauseMenu의 Dim 오버레이(raycastTarget)가 항상 위에 그려져 설정 패널의
            // 입력을 가로채는 것을 막기 위해, 열릴 때 Canvas 내 형제 순서를 최상단으로 올린다.
            transform.SetAsLastSibling();
            panelRoot?.SetActive(true);
        }

        public void Close()
        {
            PlayerPrefs.Save();
            panelRoot?.SetActive(false);
        }

        private void LoadSettings()
        {
            float masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            float musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
            float sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            int qualityIndex = Mathf.Clamp(
                PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()),
                0, QualitySettings.names.Length - 1);
            bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

            // 볼륨의 믹서 적용은 Start()에서 수행한다(Awake 시점 AudioMixer.SetFloat 미적용 이슈 회피).
            ApplyQuality(qualityIndex);
            ApplyFullscreen(fullscreen);

            masterVolumeSlider?.SetValueWithoutNotify(masterVolume);
            musicVolumeSlider?.SetValueWithoutNotify(musicVolume);
            sfxVolumeSlider?.SetValueWithoutNotify(sfxVolume);
            qualityDropdown?.SetValueWithoutNotify(qualityIndex);
            fullscreenToggle?.SetIsOnWithoutNotify(fullscreen);
        }

        private void HandleMasterVolumeChanged(float value)
        {
            ApplyVolume(MasterParam, value);
            PlayerPrefs.SetFloat(MasterVolumeKey, value);
        }

        private void HandleMusicVolumeChanged(float value)
        {
            ApplyVolume(MusicParam, value);
            PlayerPrefs.SetFloat(MusicVolumeKey, value);
        }

        private void HandleSfxVolumeChanged(float value)
        {
            ApplyVolume(SfxParam, value);
            PlayerPrefs.SetFloat(SfxVolumeKey, value);
        }

        private void HandleQualityChanged(int index)
        {
            ApplyQuality(index);
            PlayerPrefs.SetInt(QualityKey, index);
        }

        private void HandleFullscreenChanged(bool fullscreen)
        {
            ApplyFullscreen(fullscreen);
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        }

        // SetFloat 적용 성공 여부 반환 — 믹서 미준비(false) 시 호출부가 재시도할 수 있게 한다.
        private bool ApplyVolume(string exposedParam, float volume)
        {
            if (audioMixer == null) return false;
            return audioMixer.SetFloat(exposedParam, LinearToDecibel(volume));
        }

        private static float LinearToDecibel(float volume)
            => volume > 0.0001f ? Mathf.Log10(volume) * 20f : MinDecibel;

        private static void ApplyQuality(int index) => QualitySettings.SetQualityLevel(index, true);
        private static void ApplyFullscreen(bool fullscreen) => Screen.fullScreen = fullscreen;
    }
}
