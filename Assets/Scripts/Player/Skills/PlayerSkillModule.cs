using System;
using System.Collections.Generic;
using System.Linq;
using __.GameModules.PlayerData;
using Agents.CombatSystem;
using Agents.Modules;
using JWLib.AnimationSystem;
using JWLib.EventChannelSystem;
using UnityEngine;

namespace Player.Skills
{
    public class PlayerSkillModule : MonoBehaviour, ISkillModule, IModule
    {
        public ModuleOwner Owner { get; private set; }
        public Player Player { get; private set; }
        public event Action OnSkillEnd;

        [SerializeField] private SkillDataTable skillDataTable;
        [SerializeField] private InputReader input;
        [SerializeField] private EventChannelSO playerChannel;

        [Header("복귀 애니메이션")] [SerializeField] private AnimParamSO combatIdleClip;

        public const float BufferDuration = 0.3f;

        public bool IsSkillActive => CurrentSkill?.IsUsing ?? false;
        public string CurrentSkillName => CurrentSkill?.SkillData?.skillName ?? "없음";
        public int? BufferedSkillIndex => _bufferedSkillIndex;

        public float BufferRemainingNormalized => _bufferedSkillIndex.HasValue
            ? Mathf.Clamp01((_bufferExpireTime - Time.unscaledTime) / BufferDuration)
            : 0f;

        public float BufferRemainingSeconds => _bufferedSkillIndex.HasValue
            ? Mathf.Max(0f, _bufferExpireTime - Time.unscaledTime)
            : 0f;

        public string BufferedSkillName => _bufferedSkillIndex.HasValue
                                           && _skills.TryGetValue(_bufferedSkillIndex.Value, out var s)
            ? s.SkillData.skillName
            : "없음";

        public T GetSkill<T>() where T : AbstractPlayerSkill
            => _skills.Values.OfType<T>().FirstOrDefault();

        public AbstractPlayerSkill GetSkill(int skillIndex)
            => _skills.TryGetValue(skillIndex, out var s) ? s : null;

        public IReadOnlyCollection<AbstractPlayerSkill> AllSkills => _skills.Values;

        // 에너미 전용 메서드 — 플레이어에서는 사용 안 함
        public int GetSkillIndex(object skill) => -1;

        private Dictionary<int, AbstractPlayerSkill> _skills = new();
        // 비시스템 스킬이 직접 제어하는 인덱스 — 이 인덱스는 HandleSkillKeyState에서 무시
        private readonly HashSet<int> _gatedIndices = new();
        public ISkill CurrentSkill;
        private IRenderer _renderer;
        private int? _bufferedSkillIndex;
        private float _bufferExpireTime;

        // 비시스템 스킬(차징 등)이 Initialize 시점에 자신의 키 인덱스를 등록해 중복 처리를 방지
        public void RegisterGatedIndex(int index) => _gatedIndices.Add(index);

        // 비시스템 스킬이 짧은 누름을 콤보로 위임할 때 호출
        public void RequestSkillUse(int index) => TryUseSkill(index);

        public void Initialize(ModuleOwner owner)
        {
            Owner = owner;
            Player = Owner as Player;
            _renderer = owner.GetModule<IRenderer>();

            foreach (var caster in GetComponentsInChildren<AbstractDamageCaster>())
                caster.InitCaster(owner);

            // enabled=false 컴포넌트는 진화 대기 중인 스킬 — 수집 및 초기화에서 제외
            var skills = GetComponentsInChildren<AbstractPlayerSkill>()
                .Where(s => s.enabled).ToArray();
            foreach (var skill in skills)
                skill.Initialize(this);
            // IsSystemTriggered=false인 스킬은 자체 입력으로 발동 — 딕셔너리에 포함하지 않음
            _skills = skills
                .Where(s => s.IsSystemTriggered)
                .ToDictionary(s => s.SkillData.skillIndex);

            input.OnSkillKeyStateChanged += HandleSkillKeyState;
        }

        // InputReader는 에셋(SO)이라 씬 리로드 후에도 살아남는다.
        // 해제하지 않으면 파괴된 이 모듈을 가리키는 묵은 델리게이트가 남아,
        // 다음 입력 시 MissingReferenceException으로 멀티캐스트 체인이 끊겨 새 플레이어 입력이 막힌다.
        private void OnDestroy()
        {
            if (input != null)
                input.OnSkillKeyStateChanged -= HandleSkillKeyState;
        }

        private void HandleSkillKeyState(int skillIndex, bool pressed)
        {
            // 게이팅된 인덱스는 해당 비시스템 스킬이 직접 처리
            if (_gatedIndices.Contains(skillIndex)) return;
            if (pressed) TryUseSkill(skillIndex);
        }

        private void TryUseSkill(int skillIndex)
        {
            if (CurrentSkill is { IsUsing: true })
            {
                if (_skills.TryGetValue(skillIndex, out var incoming) && incoming.CanInterruptCurrentSkill)
                {
                    _bufferedSkillIndex = skillIndex;
                    _bufferExpireTime = Time.unscaledTime + BufferDuration;
                    CurrentSkill.StopSkill();
                    return;
                }

                _bufferedSkillIndex = skillIndex;
                _bufferExpireTime = Time.unscaledTime + BufferDuration;
                return;
            }

            if (CanUseSkill(skillIndex))
                UseSkill(skillIndex);
        }

        public bool CanUseSkill(int index, GameObject target = null)
        {
            if (IsSkillActive) return false;
            if (!_skills.TryGetValue(index, out var skill)) return false;
            return skill.CanUseSkill();
        }

        public void UseSkill(int index, GameObject target = null)
        {
            if (!_skills.TryGetValue(index, out var skill)) return;

            Debug.Log($"Use Skill {index}");
            if (CurrentSkill != null)
                CurrentSkill.OnSkillEnd -= HandleSkillEnd;
            CurrentSkill = skill;
            CurrentSkill.OnSkillEnd += HandleSkillEnd;
            CurrentSkill.UseSkill();
        }

        private void HandleSkillEnd()
        {
            CurrentSkill.OnSkillEnd -= HandleSkillEnd;

            if (_bufferedSkillIndex.HasValue && Time.unscaledTime < _bufferExpireTime)
            {
                int buffered = _bufferedSkillIndex.Value;
                _bufferedSkillIndex = null;
                if (CanUseSkill(buffered))
                {
                    UseSkill(buffered);
                    return;
                }
            }

            _bufferedSkillIndex = null;
            _renderer.PlayClip(combatIdleClip.ParamHash, 0.1f, 0.1f);
            InvokeAttackEnd();
        }

        // 버퍼에 캔슬 가능한 스킬이 유효하게 대기 중인지 확인
        public bool HasCancelSkillBuffered()
        {
            if (!_bufferedSkillIndex.HasValue || Time.unscaledTime >= _bufferExpireTime) return false;
            if (!_skills.TryGetValue(_bufferedSkillIndex.Value, out var skill)) return false;
            return skill.CanCancelAttack && skill.CanUseSkill();
        }

        // 스킬 진화 — current를 비활성화하고 next를 초기화·활성화한 뒤 같은 슬롯으로 교체
        public void EvolveSkill(AbstractPlayerSkill current, AbstractPlayerSkill next)
        {
            int index = current.SkillData.skillIndex;
            next.enabled = true;
            next.Initialize(this);
            // 진화체는 정의상 이미 해금된 활성 스킬 — 해금하지 않으면 같은 인덱스의
            // SkillUnlockUpgradeSO가 다시 '해금 가능'으로 판정해 획득 카드가 재등장한다
            next.Unlock();
            current.enabled = false;
            if (_skills.ContainsKey(index))
                _skills[index] = next;
        }

        public void InvokeAttackEnd()
        {
            OnSkillEnd?.Invoke();
        }

        // 착지 애니메이션 종료 후 점프가 외부에서 중단된 경우 호출 — 레이어 무관 아이들 복귀
        public void PlayIdle()
        {
            _renderer.PlayClip(combatIdleClip.ParamHash, 0.1f, 0.1f);
        }
    }
}
