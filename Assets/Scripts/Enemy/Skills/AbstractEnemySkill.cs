using System;
using Agents.CombatSystem;
using Agents.Modules;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;

namespace Enemy.Skills
{
    public abstract class AbstractEnemySkill : MonoBehaviour, ISkill
    {
        public event Action OnSkillEnd;

        [field: SerializeField] public SkillDataSO SkillData { get; private set; }

        [Tooltip("UseSkill() 시 재생할 SFX (비워두면 재생하지 않음)")]
        [SerializeField] private SoundSo useSfx;

        [Tooltip("UseSkill() 시 자신의 위치에 재생할 파티클 (VFXModule 등록 VFX, 비워두면 재생하지 않음)")]
        [SerializeField] protected AssetNameSO attackStartVfx;

        public float NormalizedCooldown => SkillData == null || Mathf.Approximately(SkillData.cooldown, 0f)
            ? 1f
            : Mathf.Clamp01((Time.unscaledTime - _lastUseTime) / SkillData.cooldown);

        public bool IsUsing { get; protected set; }

        protected AbstractEnemy _enemy;
        protected IRenderer _renderer;
        protected ISkillModule _skillModule;
        protected AudioModule _audio;
        protected VFXModule _vfxModule;
        protected float _lastUseTime;

        public virtual void Initialize(ISkillModule module)
        {
            _skillModule = module;
            _enemy = module.Owner as AbstractEnemy;
            _renderer = _enemy?.GetModule<IRenderer>();
            _audio = _enemy?.GetModule<AudioModule>();
            _vfxModule = _enemy?.GetModule<VFXModule>();
            IsUsing = false;
        }

        public abstract bool CanUseSkill(GameObject target = null);

        public virtual void UseSkill(GameObject target = null)
        {
            IsUsing = true;
            _lastUseTime = Time.unscaledTime;
            _audio?.PlaySfx(useSfx);

            if (attackStartVfx != null)
                _vfxModule?.PlayVFX(attackStartVfx.AssetHash);
        }

        public virtual void StopSkill()
        {
            IsUsing = false;
            OnSkillEnd?.Invoke();

            if (attackStartVfx != null)
                _vfxModule?.StopVFX(attackStartVfx.AssetHash);
        }
    }
}
