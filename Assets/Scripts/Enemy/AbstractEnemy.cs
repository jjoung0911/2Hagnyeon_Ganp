using System;
using System.Collections;
using _00.Scripts.Enemy.BT;
using Agents;
using Agents.CombatSystem;
using Agents.Modules;
using Enemy.BT;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using Unity.Behavior;
using UnityEngine;

namespace Enemy
{
    public abstract class AbstractEnemy : Agent, IPoolable, IDifficultyScalable
    {
        [field: SerializeField] public float StopDistance { get; private set; } = 1f; // 정지 최소 거리

        [Header("Pool")]
        [SerializeField] private EventChannelSO enemyChannel;
        [SerializeField] private float despawnDelay = 2f; // 사망 연출 후 풀로 반환되기까지의 대기 시간

        public INavMovement NavMovement { get; private set; }
        public HealthModule HealthModule { get; private set; }
        public IRenderer Renderer { get; private set; }
        public ISensor Sensor { get; private set; }
        public ISkillModule SkillModule { get; private set; }
        public IAnimationTrigger AnimationTrigger { get; private set; }
        public BehaviorGraphAgent BtAgent { get; private set; }
        public IAnimationTrigger Trigger { get; private set; }
        
        public StateChannel StateChannel { get; private set; }
        

        private Coroutine _pushCoroutine;
        // 웨이브 데미지 스케일링 대상 — 모듈 초기화 시 1회 캐싱
        private AbstractDamageCaster[] _damageCasters;

        protected virtual void Start()
        {
            if(!GetVariable(BtVars.StateChannel, out BlackboardVariable<StateChannel> stateChannelVariable))
            {
                Debug.LogError($"StateChannel 변수가 BT에 존재하지 않습니다. 변수 이름: {BtVars.StateChannel}");
                return;
            }

            StateChannel = stateChannelVariable.Value;
            SetVariableValue(BtVars.Enemy, this);

            var hitHandler = GetModule<IHitHandler>();
            if (hitHandler != null)
                hitHandler.OnStaggerBegin += HandleStaggerBegin;

            OnDeath.AddListener(HandlePushToPool);

            // 풀에서 스폰되지 않고 씬에 직접 배치된 적도 플레이어를 즉시 인식하도록 처리
            TryEngageNearestTarget();
        }

        protected override void OnDestroy()
        {
            var hitHandler = GetModule<IHitHandler>();
            if (hitHandler != null)
                hitHandler.OnStaggerBegin -= HandleStaggerBegin;

            OnDeath.RemoveListener(HandlePushToPool);
        }

        private void HandleStaggerBegin()
        {
            StateChannel?.SendEventMessage(EnemyState.HIT);
        }

        // 스폰 위치가 정해진 직후 호출되어, 탐지 범위와 무관하게 플레이어를 즉시 타겟으로 고정하고 전투 상태로 전환한다.
        // SpawnManager/DifficultyManager가 transform.position을 설정한 뒤 호출해야 한다.
        public void TryEngageNearestTarget()
        {
            if (Sensor == null) return;
            if (!GetVariable(BtVars.StateChannel, out BlackboardVariable<StateChannel> stateChannelVar)) return;
            if (!GetVariable(BtVars.TargetGameObject, out BlackboardVariable<GameObject> targetVar)) return;

            Collider closest = Sensor.FindClosestTarget();
            if (closest == null) return;

            targetVar.Value = closest.gameObject;
            stateChannelVar.Value?.SendEventMessage(EnemyState.COMBAT);
        }

        // 사망 연출이 끝날 시간만큼 대기한 뒤 PushEvent를 발행해 풀로 반환을 요청
        private void HandlePushToPool()
        {
            if (_pushCoroutine != null) StopCoroutine(_pushCoroutine);
            _pushCoroutine = StartCoroutine(PushAfterDelay());
        }

        private IEnumerator PushAfterDelay()
        {
            yield return new WaitForSeconds(despawnDelay);
            enemyChannel?.RaiseEvent(EnemyEvents.PushEvent.Init(this));
        }

        protected override void InitializeModules()
        {
            base.InitializeModules();

            NavMovement = GetModule<INavMovement>();
            BtAgent = GetComponent<BehaviorGraphAgent>();
            Renderer = GetModule<IRenderer>();
            Sensor = GetModule<ISensor>();
            SkillModule = GetModule<ISkillModule>();
            AnimationTrigger = GetModule<IAnimationTrigger>();
            Trigger = GetModule<IAnimationTrigger>();
            HealthModule = GetModule<HealthModule>();
            _damageCasters = GetComponentsInChildren<AbstractDamageCaster>(true);
        }

        // 난이도 스케일 — HealthModule(HP 스탯)과 데미지 캐스터에 위임한다.
        // DifficultyManager가 스폰 직후(ResetItem 이후) 호출하므로 풀 재사용 시에도 항상 현재 경과 시간 배율로 갱신된다.
        public void ApplyDifficultyScale(float statMult, float damageMult)
        {
            HealthModule?.ApplyDifficultyScale(statMult);

            if (_damageCasters == null) return;
            foreach (var caster in _damageCasters)
                if (caster != null) caster.DamageMultiplier = damageMult;
        }

        #region BT Helpers
        // BTAgent의 변수를 세팅하는 헬퍼함수. 
        public void SetVariableValue<T>(string variableName, T value)
        {
            Debug.Assert(BtAgent != null, "BehaviorTreeAgent이 존재하지 않습니다.");
            Debug.Assert(!string.IsNullOrEmpty(variableName), "변수 이름이 null이 될 수 없습니다.");
            //BTAgent에서 variableName에 해당하는 블랙보드 변수를 가져오고, 가져온 별수의 Value를 세팅.
            if (BtAgent.GetVariable<T>(variableName, out BlackboardVariable<T> variable))
            {
                variable.Value = value;
            }
            else
            {
                Debug.LogError($"변수 '{variableName}'을(를) 찾을 수 없습니다.");
            }
        }

        public bool GetVariable<T>(string variableName, out BlackboardVariable<T> variable)
        {
            Debug.Assert(BtAgent != null, "BehaviorTreeAgent이 존재하지 않습니다.");
            Debug.Assert(!string.IsNullOrEmpty(variableName), "변수 이름이 null이 될 수 없습니다.");

            return BtAgent.GetVariable<T>(variableName, out variable);
        }
        #endregion

        [field: SerializeField] public PoolItemSO Item { get; set; }
        public GameObject GameObject  => this != null ? gameObject : null;
        public void ResetItem()
        {
            if (_pushCoroutine != null)
            {
                StopCoroutine(_pushCoroutine);
                _pushCoroutine = null;
            }

            IsDead = false;
            if(StateChannel != null)
                StateChannel.SendEventMessage(EnemyState.IDLE);
            HealthModule.CurrentHp = HealthModule.MaxHp;
            GetModule<IStatusEffectModule>()?.ResetEffects();
        }
    }
}