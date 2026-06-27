using Agents.Modules;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;

namespace Player
{
    public class FootStepModule : MonoBehaviour, IModule
    {
        [SerializeField] private SoundSo walkFootStep;
        [SerializeField] private SoundSo runFootStep;


        private AudioModule _audio;
        
        public void Initialize(ModuleOwner owner)
        {
            _audio = owner.GetModule<AudioModule>();
            
            
        }

        public void PlayWalkSFX() => _audio.PlaySfx(walkFootStep);
        public void PlayRunSFX() => _audio.PlaySfx(runFootStep);
    }
}