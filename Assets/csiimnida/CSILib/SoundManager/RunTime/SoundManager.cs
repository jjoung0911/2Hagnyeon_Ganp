using System.Collections.Generic;
using CSILib.SoundManager.RunTime;
using UnityEngine;
using UnityEngine.Audio;

namespace csiimnida.CSILib.SoundManager.RunTime
{
    // 사운드 재생 서비스.
    // - 게임 코드는 SoundSo 에셋을 직접 넘겨 재생한다(PlaySound(SoundSo)). 문자열 식별자를 쓰지 않는다.
    // - 재생마다 GameObject를 생성/파괴하지 않고 AudioSource 풀을 재사용해 GC 부하를 없앤다.
    // - 문자열 API는 라이브러리/에디터 호환을 위해서만 남겨두며 내부적으로 SoundSo 경로로 위임한다.
    public class SoundManager : MonoSingleton<SoundManager>
    {
        [SerializeField] private SoundListSo _soundListSo;
        [SerializeField] private AudioMixer _mixer;

        [Tooltip("시작 시 미리 생성해 둘 AudioSource 풀 크기")]
        [SerializeField] private int initialPoolSize = 16;

        private readonly List<AudioSource> _pool = new();

        // 루프 SFX 추적용 — 같은 SoundSo의 중복 재생 방지 및 StopSound 대상
        private readonly Dictionary<SoundSo, AudioSource> _activeLoops = new();

        // BGM은 동시에 한 곡만 재생
        private AudioSource _bgmSource;
        private SoundSo _currentBgm;

        private AudioMixerGroup _sfxGroup;
        private AudioMixerGroup _bgmGroup;
        private AudioMixerGroup _masterGroup;

        private void Awake()
        {
            if (_soundListSo == null)
                Debug.LogWarning("[SoundManager] SoundListSo가 할당되지 않았습니다. 문자열 기반 재생 API를 사용할 수 없습니다.");

            CacheMixerGroups();

            for (int i = 0; i < initialPoolSize; i++)
                _pool.Add(CreateSource());
        }

        private void CacheMixerGroups()
        {
            if (_mixer == null)
            {
                Debug.LogWarning("[SoundManager] AudioMixer가 할당되지 않았습니다. MixerGroup 라우팅 없이 재생됩니다.");
                return;
            }

            _sfxGroup = FirstGroup("SFX");
            _bgmGroup = FirstGroup("BGM");
            _masterGroup = FirstGroup("Master");
        }

        private AudioMixerGroup FirstGroup(string name)
        {
            AudioMixerGroup[] groups = _mixer.FindMatchingGroups(name);
            return groups.Length > 0 ? groups[0] : null;
        }

        private AudioSource CreateSource()
        {
            var go = new GameObject("PooledAudioSource");
            go.transform.SetParent(transform, false);
            AudioSource src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            return src;
        }

        // 재생 중이 아닌 소스를 재사용한다. 모두 사용 중이면 풀을 확장한다(워밍업 이후엔 거의 발생하지 않음).
        private AudioSource GetFreeSource()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].isPlaying)
                    return _pool[i];
            }

            AudioSource src = CreateSource();
            _pool.Add(src);
            return src;
        }

        // ===== SoundSo API (게임 코드 진입점) =====

        // position이 주어지면 해당 위치에서 재생(3D), 없으면 매니저 위치에서 재생.
        public void PlaySound(SoundSo so, Vector3? position = null)
        {
            if (so == null)
            {
                Debug.LogWarning("[SoundManager] 재생할 SoundSo가 null입니다.");
                return;
            }
            if (so.clip == null)
            {
                Debug.LogWarning($"[SoundManager] \"{so.soundName}\" 사운드에 AudioClip이 할당되어 있지 않습니다.");
                return;
            }

            if (so.soundType == SoundType.BGM)
            {
                PlayBGM(so);
                return;
            }

            // 같은 루프 SFX가 이미 재생 중이면 중복 생성 방지
            if (so.loop && _activeLoops.TryGetValue(so, out AudioSource active) && active != null && active.isPlaying)
                return;

            AudioSource source = GetFreeSource();
            ConfigureSource(source, so, _sfxGroup, position);
            source.Play();

            if (so.loop)
                _activeLoops[so] = source;
        }

        // 루프 SFX를 정지한다. 일회성 SFX는 재생이 끝나면 풀이 자동 회수하므로 호출할 필요가 없다.
        public void StopSound(SoundSo so)
        {
            if (so == null) return;
            if (_activeLoops.TryGetValue(so, out AudioSource source))
            {
                if (source != null) source.Stop();
                _activeLoops.Remove(so);
            }
        }

        // 이전 BGM을 정지하고 새 BGM을 재생한다(동시에 1곡 유지). 같은 곡이 이미 재생 중이면 무시.
        public void PlayBGM(SoundSo so)
        {
            if (so == null || so.clip == null) return;
            if (_currentBgm == so && _bgmSource != null && _bgmSource.isPlaying) return;

            StopBGM();
            _bgmSource = GetFreeSource();
            ConfigureSource(_bgmSource, so, _bgmGroup, null);
            _bgmSource.Play();
            _currentBgm = so;
        }

        public void StopBGM()
        {
            if (_bgmSource != null)
            {
                _bgmSource.Stop();
                _bgmSource = null;
            }
            _currentBgm = null;
        }

        // 추적 중인 모든 루프 사운드와 BGM을 정지한다.
        public void StopAllSounds()
        {
            foreach (AudioSource source in _activeLoops.Values)
            {
                if (source != null) source.Stop();
            }
            _activeLoops.Clear();
            StopBGM();
        }

        private void ConfigureSource(AudioSource source, SoundSo so, AudioMixerGroup group, Vector3? position)
        {
            source.transform.position = position ?? transform.position;
            source.clip = so.clip;
            source.loop = so.loop;
            source.priority = so.Priority;
            source.volume = so.volume;
            source.pitch = so.RandomPitch ? Random.Range(so.MinPitch, so.MaxPitch) : so.pitch;
            source.panStereo = so.stereoPan;
            source.spatialBlend = so.SpatialBlend;
            source.outputAudioMixerGroup = group != null ? group : _masterGroup;

            // 역재생(피치 음수)일 때는 클립 끝에서 시작
            source.time = source.pitch < 0f ? Mathf.Max(0f, so.clip.length - 0.01f) : 0f;
        }

        // ===== 문자열 API (라이브러리/에디터 호환 전용) =====
        // 게임 코드에서는 사용하지 않는다. SoundListSo에 등록된 이름으로 SoundSo를 조회해 위임한다.

        public void PlaySound(string soundName)
        {
            if (TryGetByName(soundName, out SoundSo so))
                PlaySound(so);
        }

        public void StopSound(string soundName)
        {
            if (TryGetByName(soundName, out SoundSo so))
                StopSound(so);
        }

        public void PlayBGM(string soundName)
        {
            if (TryGetByName(soundName, out SoundSo so))
                PlayBGM(so);
        }

        private bool TryGetByName(string soundName, out SoundSo so)
        {
            so = null;
            if (_soundListSo == null || _soundListSo.SoundsDictionary == null)
            {
                Debug.LogWarning("[SoundManager] SoundListSo가 없어 문자열로 사운드를 조회할 수 없습니다.");
                return false;
            }
            if (!_soundListSo.SoundsDictionary.TryGetValue(soundName, out so))
            {
                Debug.LogWarning($"[SoundManager] SoundListSo에 \"{soundName}\" 사운드가 등록되어 있지 않습니다.");
                return false;
            }
            return true;
        }
    }

    public enum SoundType
    {
        BGM,
        SFX
    }
}
