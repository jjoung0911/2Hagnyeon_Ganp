using System;
using Agents.CombatSystem;
using Agents.Modules;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using Player.Skills.Finishers;
using System.Managers;
using System.StatSystem;
using __.GameModules.PlayerData;
using UnityEngine;

namespace Player.Skills
{
    // 검성(평타) 루트 핵심 스킬 — 레벨업으로 콤보 확장 → 추가 공격(검기/잔상/충격파) → 검신 스택 → 차원참으로 강화된다.
    // 데미지는 모두 GetAtk() 계수 기반: (GetAtk() * 타별계수 + 버프) * DamageMultiplier * 스택배율.
    // 레벨별 수치는 SkillUpgradeSO.targetFields(리플렉션으로 명명된 필드에 세팅)로 데이터 드리븐 관리한다.
    public class PlayerAttackCombo : AbstractPlayerSkill
    {
        [SerializeField] private EventChannelSO createChannel;
        [SerializeField] private float comboWindow = 0.5f;
        [SerializeField] private ComboEntry[] comboEntries;
        [SerializeField] private PoolItemSO hitVfx;
        [SerializeField] private PoolItemSO deathVfx;
        // 기본 사용 가능 콤보 수 — 업그레이드 전 comboEntries의 앞 N개만 활성화
        [SerializeField] private int baseComboCount = 1;

        [SerializeField] private float ultGaugePerHit = 8f;

        [Header("스탯")]
        // 공격력 계수 계산용 스탯(GetAtk)
        [SerializeField] private StatSO atkStatSo;
        // 검술 숙련(Blade Mastery) 패시브 — 공격 속도 배율 스탯
        [SerializeField] private StatSO attackSpeedStatSo;
        // 검술 숙련(Blade Mastery) 패시브 — 공격 범위 배율 스탯
        [SerializeField] private StatSO attackRangeStatSo;

        [Header("검신 스택")]
        [Tooltip("마지막 적중 후 이 시간(초)이 지나면 스택이 0으로 리셋")]
        [SerializeField] private float stackDecayTime = 5f;

        private PlayerCameraModule _cameraModule;
        private IAgentStat _stat;
        private PlayerAttackBuffModule _attackBuffModule;
        // InputReader(SO) 캐시 — OnDestroy에서 구독 해제용 (파괴된 _player 접근 불가 대비)
        private InputReader _input;

        // 자식 오브젝트에 부착된 피니셔 효과들(검기/잔상/충격파/차원참) — 레벨에 따라 자동 활성
        private IFinisherEffect[] _finishers;

        private int _comboCount;
        private float _lastStopTime = float.MinValue;
        private ComboEntry _currentEntry;
        // 공격 키를 계속 누르고 있는지 여부 — StopSkill에서 콤보 연속 실행 판단에 사용
        private bool _attackKeyHeld;
        // 한 번의 휘두름에서 스택을 이미 획득했는지 — 다중 타격 시 중복 획득 방지
        private bool _stackGainedThisSwing;
        // 업그레이드로 늘어나는 현재 최대 콤보 수
        private int _activeComboCount;

        // ── 레벨별 캐시 — SkillUpgradeSO.targetFields(리플렉션)로 직접 세팅 ──
        private int _comboBonus;                  // 추가 콤보 수 (baseComboCount에 가산)
        private float _attackSpeedBonus;          // 공속 보너스 (0.1 = +10%)
        private float _comboCoef1 = 1f;           // 1타 데미지 계수 (0이면 1로 처리)
        private float _comboCoef2 = 1f;           // 2타 데미지 계수
        private float _comboCoef3 = 1f;           // 3타 데미지 계수
        private float _stackAtkSpeedPerStack;     // 스택당 공속 증가 (0.03 = +3%)
        private float _stackDamagePerStack;       // 스택당 데미지 증가 (0.03 = +3%)
        private int _maxStacks;                    // 최대 스택 (0이면 스택 시스템 비활성)

        // ── 검신 스택 런타임 ──────────────────────────────────────────
        private int _stack;
        private float _lastHitTime;

        // 스택 변화 알림 — UI 표시용 (current, max)
        public event Action<int, int> OnStackChanged;
        public int Stack => _stack;
        public int MaxStack => _maxStacks;

        public int ComboCount => _comboCount;
        public int ComboMax => _activeComboCount;

        public override void Initialize(ISkillModule module)
        {
            base.Initialize(module);
            _activeComboCount = Mathf.Clamp(baseComboCount, 1, comboEntries?.Length ?? 1);
            _cameraModule = _player.GetModule<PlayerCameraModule>();
            _moveData = _player.GetModule<PlayerMoveData>();
            _stat = _player.GetModule<IAgentStat>();
            _attackBuffModule = _player.GetModule<PlayerAttackBuffModule>();

            // 자식 피니셔 수집 후 현재 레벨 기준으로 해금 상태 동기화
            _finishers = GetComponentsInChildren<IFinisherEffect>(true);
            foreach (var finisher in _finishers)
            {
                finisher.Init(_player);
                finisher.UpdateUnlock(UpgradeLevel);
            }

            // InputReader(SO)를 캐시 — 파괴 시점에 _player는 이미 파괴돼 접근할 수 없으므로 SO 참조로 해제한다
            _input = _player.Input;
            _input.OnSkillKeyStateChanged += HandleAttackKeyState;
        }

        // 공격 키 입력 상태 추적 — 누르고 있는 동안 StopSkill에서 콤보를 계속 이어간다
        private void HandleAttackKeyState(int skillIndex, bool pressed)
        {
            if (skillIndex != SkillData.skillIndex) return;
            _attackKeyHeld = pressed;
        }

        // InputReader(SO)는 씬 리로드 후에도 살아남으므로 반드시 해제 — 묵은 델리게이트로 인한 입력 먹통 방지
        private void OnDestroy()
        {
            if (_input != null)
                _input.OnSkillKeyStateChanged -= HandleAttackKeyState;
        }

        private void Update() => DecayStacksIfNeeded();

        public override bool CanUseSkill(GameObject target = null) => DefaultCanUse();

        protected override void OnUpgraded(int level, SkillUpgradeSO data)
        {
            // 수치 필드(_comboBonus/_attackSpeedBonus/_comboCoefN/_maxStacks 등)는
            // SkillUpgradeSO.targetFields(리플렉션)로 이미 세팅됨 — 여기선 파생 처리만 수행
            _activeComboCount = Mathf.Clamp(baseComboCount + _comboBonus, 1, comboEntries?.Length ?? 1);
            if (_stack > _maxStacks) SetStack(_maxStacks);

            ApplyRangeMultiplier();

            // 피니셔 해금은 레벨 기준 자동 — 별도 분기 없이 모든 효과에 위임
            foreach (var finisher in _finishers)
                finisher.UpdateUnlock(level);
        }

        // 타별 데미지 계수 — 미설정(0)이면 1.0(변화 없음)으로 처리
        private float GetComboCoefficient(int comboIndex)
        {
            float coefficient = comboIndex switch
            {
                0 => _comboCoef1,
                1 => _comboCoef2,
                2 => _comboCoef3,
                _ => 1f
            };
            return coefficient > 0f ? coefficient : 1f;
        }

        // 공격력 계수 계산 — 다른 스킬과 동일한 GetAtk 패턴
        private float GetAtk() => _stat?.GetStat(atkStatSo.Index)?.Value ?? 0f;

        // 검술 숙련(Blade Mastery) 패시브로 변동되는 스탯 배율 — 변경 시점마다 다시 읽어 반영
        private float GetStatMultiplier(StatSO statSo)
            => _stat != null && statSo != null && _stat.TryGetStat(statSo.Index, out var stat) ? stat.Value : 1f;

        // 업그레이드 배율과 패시브 스탯 배율을 곱해 모든 콤보 캐스터의 사거리에 반영
        private void ApplyRangeMultiplier()
        {
            float rangeMultiplier = GetStatMultiplier(attackRangeStatSo);
            foreach (var entry in comboEntries)
                entry.caster?.SetRangeMultiplier(rangeMultiplier);
        }

        // 공속 = (1 + 레벨공속 + 스택공속) * 패시브 배율
        private float GetAttackSpeedMultiplier()
            => (1f + _attackSpeedBonus + _stack * _stackAtkSpeedPerStack) * GetStatMultiplier(attackSpeedStatSo);

        private float GetStackDamageMultiplier() => 1f + _stack * _stackDamagePerStack;
        private float GetBonusDamage() => _attackBuffModule?.CurrentDamage ?? 0f;

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);

            bool countOver = _comboCount >= _activeComboCount;
            bool comboWindowExhaust = Time.unscaledTime >= _lastStopTime + comboWindow;
            if (countOver || comboWindowExhaust)
                _comboCount = 0;

            _currentEntry = comboEntries[_comboCount];
            _stackGainedThisSwing = false;
            LockMovement();
            Vector3 attackDir = _moveData.TargetMoveDir.magnitude > 0.1f
                ? _moveData.TargetMoveDir.normalized
                : _player.transform.forward;
            _player.transform.rotation = Quaternion.LookRotation(attackDir);
            _rootMotion.Begin(attackDir);
            _audio?.PlaySfx(_currentEntry.data.SwingSfx);

            ApplyRangeMultiplier();
            _renderer.PlayClip(_currentEntry.data.Param.ParamHash, 0.1f, 0.3f);
            _renderer.SetAnimatorSpeed(GetAttackSpeedMultiplier());
            _trigger.OnAttack += HandleAttack;
            _trigger.OnAttackEnd += HandleAttackEnd;
            SubscribeAutoStopOnAnimationEnd();
        }

        private void HandleAttack()
        {
            _trigger.OnAttack -= HandleAttack;
            if (_currentEntry.data.ComboVfx != null)
                createChannel.RaiseEvent(CreateEvents.ShowPoolingVfx
                    .InitData(_currentEntry.data.ComboVfx, transform.position, Quaternion.LookRotation(transform.forward)));

            var caster = _currentEntry.caster;
            if (caster != null)
            {
                caster.CurrentHitType = _currentEntry.data.HitType;
                caster.SetDamageOverride(CalculateComboDamage());
                caster.OnHit += HandleHit;
                caster.OnKill += HandleKill;
                caster.CastOnce(); // HandleHit이 동기 호출되어 스택이 먼저 갱신됨
                caster.OnHit -= HandleHit;
                caster.OnKill -= HandleKill;
            }

            // 피니셔(마지막 타)일 때만 추가 공격 실행 — 레벨 분기 없이 해금된 효과 일괄 발동
            bool isFinisher = _comboCount == _activeComboCount - 1;
            if (isFinisher) RunFinishers();
        }

        // 타별 콤보 데미지 = (GetAtk * 타계수 + 버프) * DamageMultiplier * 스택배율
        // 광전사의 영혼은 공격력 스탯 자체를 올리므로 GetAtk()에 이미 반영되어 있음
        private float CalculateComboDamage()
            => (GetAtk() * GetComboCoefficient(_comboCount) + GetBonusDamage())
               * DamageMultiplier * GetStackDamageMultiplier();

        // 해금된 모든 피니셔 발동. 차원참(스택 소모형)이 발동하면 스택을 0으로 리셋한다.
        private void RunFinishers()
        {
            if (_finishers == null || _finishers.Length == 0) return;

            var ctx = new FinisherContext(
                _player,
                transform.position,
                Quaternion.LookRotation(transform.forward),
                GetAtk(),
                GetBonusDamage(),
                DamageMultiplier * GetStackDamageMultiplier(),
                _currentEntry.data.HitType,
                _stack,
                _maxStacks);

            bool consumeStacks = false;
            foreach (var finisher in _finishers)
            {
                if (!finisher.IsUnlocked) continue;
                if (finisher.Execute(in ctx)) consumeStacks = true;
            }

            if (consumeStacks) ResetStacks();
        }

        private void HandleHit(DamageData data)
        {
            _audio?.PlaySfx(_currentEntry.data.HitSfx);
            createChannel.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(
                hitVfx, data.HitPoint, Quaternion.LookRotation(data.HitNormal)));
            ApplyHitFeedback(data);
            playerChannel?.RaiseEvent(CombatEventCache.PlayerHit);

            // 평타 적중 시 검신 스택 획득(휘두름당 1회)
            if (!_stackGainedThisSwing)
            {
                _stackGainedThisSwing = true;
                AddStack();
            }
        }

        private void HandleKill(Collider col, DamageData data)
        {
            createChannel.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(
                deathVfx, data.HitPoint, Quaternion.LookRotation(data.HitNormal)));
        }

        // ── 검신 스택 ─────────────────────────────────────────────────

        private void AddStack()
        {
            if (_maxStacks <= 0) return;
            _lastHitTime = Time.time;
            SetStack(Mathf.Min(_stack + 1, _maxStacks));
        }

        private void DecayStacksIfNeeded()
        {
            if (_stack > 0 && stackDecayTime > 0f && Time.time - _lastHitTime > stackDecayTime)
                ResetStacks();
        }

        private void ResetStacks() => SetStack(0);

        private void SetStack(int value)
        {
            if (_stack == value) return;
            _stack = value;
            OnStackChanged?.Invoke(_stack, _maxStacks);
        }

        private void HandleAttackEnd()
        {
            _trigger.OnAttackEnd -= HandleAttackEnd;
            if (_skillModule.HasCancelSkillBuffered())
                StopSkill();
        }

        private void ApplyHitFeedback(DamageData data)
        {
            float force = _currentEntry.data.ShakeForce;
            // 커스텀 방향이 없으면 피격 노멀(적에서 플레이어 방향)을 사용해 방향성 임펄스 전달
            Vector3 dir = _currentEntry.data.ShakeDirection.magnitude > 0.01f
                ? _currentEntry.data.ShakeDirection
                : data.HitNormal;
            float stopScale = _currentEntry.data.HitStopTimeScale > 0f ? _currentEntry.data.HitStopTimeScale : 0.001f;
            float stopDur = _currentEntry.data.HitStopDuration > 0f ? _currentEntry.data.HitStopDuration : 0.06f;

            // 점진적 슬로우 대신 즉각 동결(HitStop) + 즉각 쉐이크로 타격의 "스냅" 느낌을 강조
            // (3타 강타격감은 3타 ComboDataSO의 ShakeForce/HitStop을 강하게 설정해 표현)
            TimeManager.Instance.HitStop(stopDur, stopScale);
            _cameraModule.ShakeCamera(force, dir);
        }

        public override void StopSkill()
        {
            UnlockMovement();
            
            _audio.StopSfx(_currentEntry.data.SwingSfx);
            
            _lastStopTime = Time.unscaledTime;
            _comboCount++;

            _renderer.SetAnimatorSpeed(1f);
            _rootMotion.End();

            _trigger.OnAttack -= HandleAttack;
            _trigger.OnAttackEnd -= HandleAttackEnd;
            UnsubscribeAutoStopOnAnimationEnd();

            // 공격 키를 계속 누르고 있으면 다음 콤보를 버퍼에 등록해 자동으로 이어간다
            if (_attackKeyHeld)
                _skillModule.RequestSkillUse(SkillData.skillIndex);

            base.StopSkill();
        }
    }

    // 콤보 한 타에 대응하는 데이터(ComboDataSO)와 실제 피해 판정을 담당하는 RayDamageCaster의 짝
    [Serializable]
    public struct ComboEntry
    {
        public ComboDataSO data;
        public RayDamageCaster caster;
    }
}
