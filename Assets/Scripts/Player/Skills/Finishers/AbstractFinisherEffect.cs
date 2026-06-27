using Agents;
using Agents.Modules;
using csiimnida.CSILib.SoundManager.RunTime;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using UnityEngine;

namespace Player.Skills.Finishers
{
    // 피니셔 효과 공통 베이스 — 해금 레벨/데미지 계수/VFX·SFX 재생 로직을 공유해 중복을 제거한다.
    public abstract class AbstractFinisherEffect : MonoBehaviour, IFinisherEffect
    {
        [Tooltip("이 효과가 해금되는 스킬 레벨")]
        [SerializeField] protected int unlockLevel = 4;

        [Tooltip("GetAtk()에 곱할 데미지 계수")]
        [SerializeField] protected float damageCoefficient = 1f;

        [Header("이펙트")]
        [Tooltip("발동 위치에 재생할 VFX (검기/잔상/충격파/차원참 구분)")]
        [SerializeField] protected PoolItemSO vfx;
        [SerializeField] protected EventChannelSO createChannel;
        [Tooltip("발동 시 재생할 SFX (SoundSo 직접 참조)")]
        [SerializeField] protected SoundSo castSfx;

        protected Agent _owner;
        // 소유자(플레이어)의 사운드 진입점 — SoundManager를 직접 참조하지 않는다
        protected AudioModule _audio;

        public bool IsUnlocked { get; private set; }

        public virtual void Init(Agent owner)
        {
            _owner = owner;
            _audio = owner != null ? owner.GetModule<AudioModule>() : null;
        }

        public void UpdateUnlock(int skillLevel) => IsUnlocked = skillLevel >= unlockLevel;

        public abstract bool Execute(in FinisherContext ctx);

        // 발동 위치에 VFX(풀링) + SFX를 함께 재생
        protected void PlayCastFeedback(Vector3 pos, Quaternion rot)
        {
            if (vfx != null && createChannel != null)
                createChannel.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(vfx, pos, rot));
            if (castSfx != null)
                _audio?.PlaySfx(castSfx);
        }
    }
}
