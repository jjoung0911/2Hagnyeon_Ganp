using System;
using System.Collections;
using System.StatSystem;
using Agents.CombatSystem;
using UnityEngine;

namespace Agents.Modules
{
    public class HealthModule : MonoBehaviour, IModule, IAfterInit, IKillable
    {
        public event Action OnDeath;

        [SerializeField] private StatSO healthStatSO;

        public bool IsInvincible { get; set; }
        public bool IsDead => CurrentHp <= 0;
        public float MaxHp => _maxHp;
        private Coroutine _invincibleCoroutine;

        private float _maxHp;
        public delegate void OnHealthChange(float currentVal, float previous, float maxVal);
        public event OnHealthChange OnHealthChangeEvent;


        public float CurrentHp
        {
            get => _currentHp;
            set
            {
                float previous = _currentHp;
                _currentHp = Mathf.Clamp(value, 0, _maxHp);
                if(!Mathf.Approximately(previous, _currentHp))
                    OnHealthChangeEvent?.Invoke(_currentHp, previous, _maxHp);
            }
        }
        
        private float _currentHp;
        private IAgentStat _statModule;
        private Agent _owner;
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner as Agent;
            
            _statModule = owner.GetModule<IAgentStat>();
        }

        public void AfterInit()
        {
            if (_statModule != null)
            {
                // SubscribeStat 반환값 = 현재 스탯 값 → maxHp/CurrentHp 초기화에 사용
                float initialHp = _statModule.SubscribeStat(healthStatSO.Index, HandleChangeMaxHp, _maxHp);
                _maxHp = initialHp;
                CurrentHp = initialHp;
            }
            else
            {
                CurrentHp = _maxHp;
            }
        }

        private void HandleChangeMaxHp(StatSO stat, float current, float previous)
        {
            float hpDiff = current - previous;
            _maxHp += hpDiff;
            CurrentHp += hpDiff;
        }

        // 난이도 스케일 — HP 스탯 기본값에 (mult-1) 비율의 모디파이어를 적용한다.
        // 동일 키로 remove→add 하므로 풀에서 재사용돼도 누적되지 않고 항상 현재 경과 시간 배율로 갱신된다.
        // 스탯 구독(HandleChangeMaxHp)을 통해 _maxHp/CurrentHp가 자동 반영된다.
        public void ApplyDifficultyScale(float mult)
        {
            if (_statModule == null || healthStatSO == null) return;

            var stat = _statModule.GetStat(healthStatSO.Index);
            if (stat == null) return;

            _statModule.RemoveModifier(healthStatSO.Index, DifficultyScaleKey);
            _statModule.AddModifier(healthStatSO.Index, DifficultyScaleKey, stat.BaseValue * (mult - 1f));
        }

        private static readonly object DifficultyScaleKey = new();

        // duration 동안 피해를 무시. WaitForSecondsRealtime — hitstop TimeScale의 영향을 받지 않음
        public void SetInvincible(float duration)
        {
            if (_invincibleCoroutine != null) StopCoroutine(_invincibleCoroutine);
            _invincibleCoroutine = StartCoroutine(InvincibleCoroutine(duration));
        }

        private IEnumerator InvincibleCoroutine(float duration)
        {
            IsInvincible = true;
            yield return new WaitForSecondsRealtime(duration);
            IsInvincible = false;
            _invincibleCoroutine = null;
        }

        public void ApplyDamage(float damageAmount)
        {
            if (IsInvincible) return;
            CurrentHp -= damageAmount;
            if (CurrentHp <= 0)
            {
                CurrentHp = 0;
                OnDeath?.Invoke();
            }
        }
    }
}