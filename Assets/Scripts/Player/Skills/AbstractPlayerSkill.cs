using System;
using Agents.CombatSystem;
using Agents.Modules;
using Agents.Modules.Movement;
using csiimnida.CSILib.SoundManager.RunTime;
using JWLib.EventChannelSystem;
using UnityEngine;

namespace Player.Skills
{
    public abstract class AbstractPlayerSkill : MonoBehaviour, ISkill, ILockableSkill
    {
        public event Action OnSkillEnd;
        [field: SerializeField] public SkillDataSO SkillData { get; private set; }
        // true이면 현재 실행 중인 다른 스킬을 강제 종료하고 즉시 발동
        [field: SerializeField] public bool CanInterruptCurrentSkill { get; private set; }
        // false이면 하단 스킬 쿨타임 HUD에 표시하지 않음
        [SerializeField] private bool showInHUD = true;
        public bool ShowInHUD => showInHUD;
        [SerializeField] protected EventChannelSO playerChannel;

        // 콤보 등 기본 스킬은 true로 시작. 신규 스킬은 Reset()에서 false로 두어
        // SkillUnlockUpgradeSO(처음 획득 시 해금)로만 사용 가능해지도록 한다
        [SerializeField] protected bool startUnlocked = true;
        public bool IsUnlocked { get; private set; }
        public void Unlock() => IsUnlocked = true;

        [Header("사운드")]
        [Tooltip("UseSkill() 시 재생할 SFX — 오버라이드하지 않을 때 사용하는 기본값")]
        [SerializeField] private SoundSo useSfx;
        [Tooltip("StopSkill() 시 재생할 SFX — 오버라이드하지 않을 때 사용하는 기본값")]
        [SerializeField] private SoundSo endSfx;

        public bool IsUsing { get; private set; }
        [field: SerializeField] public bool CanCancelAttack { get; private set; }

        public int UpgradeLevel { get; private set; }
        protected float DamageMultiplier { get; private set; } = 1f;
        protected float CooldownMultiplier { get; private set; } = 1f;
        protected SkillUpgradeSO CurrentUpgradeTier => SkillData.GetTier(UpgradeLevel);

        public virtual float NormalizedCooldown
        {
            get
            {
                float cd = SkillData.cooldown * CooldownMultiplier;
                return Mathf.Approximately(cd, 0f) ? 1f : Mathf.Clamp01((Time.time - _lastUseTime) / cd);
            }
        }

        // 스킬을 즉시 사용할 수 있는지 — 기본은 쿨다운 완료 여부. 충전식 스킬은 CurrentCharges로 재정의
        public virtual bool IsReady => NormalizedCooldown >= 1f;

        // 충전식 스킬의 최대 충전 수 — 일반 쿨다운 스킬은 1
        public virtual int MaxCharges => 1;

        // 현재 사용 가능한 충전 수 — 일반 쿨다운 스킬은 IsReady 여부로 0 또는 1
        public virtual int CurrentCharges => IsReady ? 1 : 0;

        // false이면 PlayerSkillModule._skills에 등록되지 않음 — 자체 입력 감지로 발동하는 스킬이 오버라이드
        public virtual bool IsSystemTriggered => true;

        protected Player _player;
        protected IRenderer _renderer;
        protected PlayerSkillModule _skillModule;
        protected IMoveData _moveData;
        protected IRootMotionDriver _rootMotion;
        protected IAnimationTrigger _trigger;
        protected AudioModule _audio;
        protected float _lastUseTime;

        public virtual void Initialize(ISkillModule module)
        {
            _skillModule = module as PlayerSkillModule;
            _player = module.Owner as Player;
            _renderer = _player.GetModule<IRenderer>();
            _moveData = _player.GetModule<IMoveData>();
            _rootMotion = _player.GetModule<IRootMotionDriver>();
            _trigger = _player.GetModule<IAnimationTrigger>();
            _audio = _player.GetModule<AudioModule>();
            IsUsing = false;
            IsUnlocked = startUnlocked;
        }

        // 이동 잠금/해제 — 스킬 실행 중 수동 이동을 막을 때 사용
        protected void LockMovement() => _moveData.CanManualMove = false;
        protected void UnlockMovement() => _moveData.CanManualMove = true;

        // 표준 사용 조건 (해금됨 + 미사용 중 + 쿨다운 완료)
        protected bool DefaultCanUse() => IsUnlocked && !IsUsing && NormalizedCooldown >= 1f;

        // 애니메이션 종료 시 자동으로 StopSkill 호출 — 단발성 스킬에 사용
        protected void SubscribeAutoStopOnAnimationEnd() => _trigger.OnAnimationEnd += StopSkillOnAnimationEnd;
        protected void UnsubscribeAutoStopOnAnimationEnd() => _trigger.OnAnimationEnd -= StopSkillOnAnimationEnd;
        private void StopSkillOnAnimationEnd() => StopSkill();

        public abstract bool CanUseSkill(GameObject target = null);

        public virtual void UseSkill(GameObject target = null) => BeginSkill();

        // IsUsing 설정 + 이벤트 발행 + 쿨다운 기록 — 파생 클래스가 base.UseSkill() 없이 직접 호출 가능
        protected void BeginSkill()
        {
            IsUsing = true;
            playerChannel.RaiseEvent(SkillEvents.SkillUseEvent.Init(SkillData, SkillData.cooldown * CooldownMultiplier));
            _lastUseTime = Time.time;
            OnUseSound();
        }

        public virtual void StopSkill()
        {
            IsUsing = false;
            OnStopSound();
            OnSkillEnd?.Invoke();
        }

        // 스킬 시작 사운드 — Inspector의 useSfx가 기본값. 복잡한 사운드 로직은 오버라이드
        protected virtual void OnUseSound() => _audio?.PlaySfx(useSfx);

        // 스킬 종료 사운드 — Inspector의 endSfx가 기본값. 복잡한 사운드 로직은 오버라이드
        protected virtual void OnStopSound() => _audio?.PlaySfx(endSfx);

        // SkillUpgradeModule에서 호출 — 레벨 적용 후 파생 클래스에 알림
        internal void ApplyUpgrade(int level)
        {
            UpgradeLevel = level;
            var data = CurrentUpgradeTier;
            DamageMultiplier = data?.damageMultiplier ?? 1f;
            CooldownMultiplier = data?.cooldownMultiplier ?? 1f;
            // 티어별 수치/플래그를 리플렉션으로 이 스킬의 명명된 필드에 직접 세팅(targetFields)한 뒤,
            // 파생 처리(클램프/부수효과)는 OnUpgraded에 위임한다.
            data?.Upgrade(this);
            OnUpgraded(level, data);
        }

        // 스킬별 고유 수치·플래그 적용 — 업그레이드 시 파생 클래스에서 오버라이드
        protected virtual void OnUpgraded(int level, SkillUpgradeSO data) { }
    }
}