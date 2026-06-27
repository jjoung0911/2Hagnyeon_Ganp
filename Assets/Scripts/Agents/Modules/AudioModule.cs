using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;

namespace Agents.Modules
{
    // SoundManager 호출을 감싸는 모듈 — 다른 모듈/스킬은 SoundManager를 직접 참조하지 않고
    // owner.GetModule<AudioModule>()을 통해 사운드를 재생/정지한다.
    // 사운드는 SoundSo 에셋 참조로 전달한다(문자열 식별자 사용 안 함).
    public class AudioModule : MonoBehaviour, IModule
    {
        [Tooltip("true면 owner 위치에서 3D로 재생한다(SoundSo의 SpatialBlend 적용 시 거리감 발생).")]
        [SerializeField] private bool playAtOwnerPosition = true;

        private Transform _owner;

        public void Initialize(ModuleOwner owner) => _owner = owner.transform;

        public void PlaySfx(SoundSo sound)
        {
            if (sound == null) return;
            if (playAtOwnerPosition && _owner != null)
                SoundManager.Instance.PlaySound(sound, _owner.position);
            else
                SoundManager.Instance.PlaySound(sound);
        }

        public void StopSfx(SoundSo sound)
        {
            if (sound == null) return;
            SoundManager.Instance.StopSound(sound);
        }

        public void PlayBGM(SoundSo sound)
        {
            if (sound == null) return;
            SoundManager.Instance.PlayBGM(sound);
        }
    }
}
