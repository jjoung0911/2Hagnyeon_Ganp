using System.StatSystem;
using Agents.Modules;
using csiimnida.CSILib.SoundManager.RunTime;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using UnityEngine;

namespace Player.Skills.Active
{
    // 모든 액티브 스킬의 공통 베이스.
    // AbstractPlayerSkill(입력 기반 단일 슬롯)과는 별도 계층 — ActiveSkillModule이
    // IsUnlocked인 스킬을 매 프레임 자동으로 Tick한다.
    public abstract class AbstractActiveSkill : MonoBehaviour, ILockableSkill, IModule
    {
        [field: SerializeField] public ActiveSkillEnum SkillIndex { get; private set; }

        [Header("이벤트/풀")]
        [SerializeField] protected EventChannelSO playerChannel;
        [SerializeField] protected EventChannelSO createChannel;
        [SerializeField] protected PoolManagerSO poolManager;
        [SerializeField] protected PoolItemSO visualItem;

        [Header("스탯")]
        [Tooltip("ScaledDamage() 사용 시 공격력 스탯 SO 연결. 미연결 시 0 반환.")]
        [SerializeField] protected StatSO damageStat;

        [Header("사운드")]
        [Tooltip("스킬 발동(Activate/OnTick) 시 재생할 SFX (비워두면 재생하지 않음)")]
        [SerializeField] protected SoundSo activateSfx;

        protected Player Player { get; private set; }
        protected AudioModule Audio { get; private set; }

        // 0 = 미획득. 1~MaxLevel = 보유 레벨.
        public int Level { get; private set; }
        public bool IsUnlocked => Level >= 1;

        // 진화 상태 — Level >= MaxLevel 후 Evolve() 호출 시 true
        public bool IsEvolved { get; private set; }
        public bool CanEvolve => Level >= ActiveSkillDataSO.MaxLevel && !IsEvolved;

        private IAgentStat _agentStat;

        public void Initialize(ModuleOwner owner)
        {
            Player = owner as Player;
            Audio = Player.GetModule<AudioModule>();
            _agentStat = Player.GetModule<IAgentStat>();
            OnInitialize();
        }

        // 파생 클래스 초기화 훅 — Player 캐싱 이후 호출
        protected virtual void OnInitialize() { }

        // ActiveSkillModule이 IsUnlocked인 스킬에 한해 매 프레임 호출
        public abstract void Tick(float dt);

        public void Unlock() => ApplyUpgrade(1);

        // 레벨 적용 → 파생 클래스 알림 → 이벤트 발행
        public void ApplyUpgrade(int level)
        {
            level = Mathf.Clamp(level, 0, ActiveSkillDataSO.MaxLevel);
            if (level == Level) return;

            Level = level;
            OnLevelChanged(Level);
            playerChannel?.RaiseEvent(ActiveSkillEvents.ActiveSkillLevelChangedEvent.Init(SkillIndex, Level));
        }

        // 진화 — CanEvolve가 true일 때만 실행
        public void Evolve()
        {
            if (!CanEvolve) return;
            IsEvolved = true;
            OnEvolved();
            playerChannel?.RaiseEvent(ActiveSkillEvents.ActiveSkillEvolvedEvent.Init(SkillIndex));
        }

        // 레벨 변경 시 호출 — 카테고리 베이스(Triggered/Persistent)가 sealed로 구현
        protected abstract void OnLevelChanged(int level);

        // 진화 시 호출되는 훅 — 파생 클래스가 비주얼/캐시 갱신 등에 override
        protected virtual void OnEvolved() { }

        // 공격력 스탯 기반 피해 계산 헬퍼
        protected float AttackPower => damageStat != null ? _agentStat?.GetStat(damageStat.Index)?.Value ?? 0f : 0f;
        protected float ScaledDamage(float multiplier) => AttackPower * multiplier;

        protected IPoolable PopVisual()
        {
            if (poolManager == null || visualItem == null) return null;
            return poolManager.Pop<IPoolable>(visualItem);
        }

        protected void PushVisual(IPoolable item)
        {
            if (item == null) return;
            poolManager?.Push(item);
        }
    }
}
