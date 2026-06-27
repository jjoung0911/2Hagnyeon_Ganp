using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.ViewModels
{
    // 인게임 UI(HP/울트/차지/콤보/스킬 쿨다운)가 공유하는 바인딩 데이터.
    // StatSO.Clone()/AgentStat 패턴과 동일하게 템플릿 에셋을 Clone해 개별 인스턴스로 보유한다.
    // [CreateProperty] 프로퍼티에 값을 대입하면 propertyChanged가 발화되어
    // dataSource로 연결된 UIDocument가 별도 호출 없이 자동 갱신된다.
    [CreateAssetMenu(fileName = "New Stat View Model", menuName = "SO/UI/StatViewModel", order = 0)]
    public class StatViewModel : ScriptableObject, INotifyBindablePropertyChanged, ICloneable
    {
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        [SerializeField] private int skillCooldownSlotCount = 4;

        private float _currentHp;
        private float _maxHp = 1f;
        private float _ultCurrent;
        private float _ultMax = 1f;
        private bool _isUltReady;
        private float _chargeProgress;
        private bool _isCharging;
        private int _comboCount;
        private float _comboTimeRatio = 1f;
        private int _currentExp;
        private int _requiredExp = 1;
        private int _level = 1;
        private List<SkillCooldownEntryViewModel> _skillCooldowns;

        [CreateProperty]
        public float CurrentHp
        {
            get => _currentHp;
            set
            {
                if (Mathf.Approximately(_currentHp, value)) return;
                _currentHp = value;
                Bindable.Raise(propertyChanged, this);
                Bindable.Raise(propertyChanged, this, nameof(NormalizedHp));
            }
        }

        [CreateProperty]
        public float MaxHp
        {
            get => _maxHp;
            set
            {
                if (Mathf.Approximately(_maxHp, value)) return;
                _maxHp = value;
                Bindable.Raise(propertyChanged, this);
                Bindable.Raise(propertyChanged, this, nameof(NormalizedHp));
            }
        }

        [CreateProperty]
        public float NormalizedHp => _maxHp > 0f ? Mathf.Clamp01(_currentHp / _maxHp) : 0f;

        [CreateProperty]
        public float UltCurrent
        {
            get => _ultCurrent;
            set
            {
                if (Mathf.Approximately(_ultCurrent, value)) return;
                _ultCurrent = value;
                Bindable.Raise(propertyChanged, this);
                Bindable.Raise(propertyChanged, this, nameof(NormalizedUlt));
            }
        }

        [CreateProperty]
        public float UltMax
        {
            get => _ultMax;
            set
            {
                if (Mathf.Approximately(_ultMax, value)) return;
                _ultMax = value;
                Bindable.Raise(propertyChanged, this);
                Bindable.Raise(propertyChanged, this, nameof(NormalizedUlt));
            }
        }

        [CreateProperty]
        public float NormalizedUlt => _ultMax > 0f ? Mathf.Clamp01(_ultCurrent / _ultMax) : 0f;

        [CreateProperty]
        public bool IsUltReady
        {
            get => _isUltReady;
            set
            {
                if (_isUltReady == value) return;
                _isUltReady = value;
                Bindable.Raise(propertyChanged, this);
            }
        }

        [CreateProperty]
        public float ChargeProgress
        {
            get => _chargeProgress;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(_chargeProgress, value)) return;
                _chargeProgress = value;
                Bindable.Raise(propertyChanged, this);
                Bindable.Raise(propertyChanged, this, nameof(IsChargeFull));
            }
        }

        [CreateProperty]
        public bool IsCharging
        {
            get => _isCharging;
            set
            {
                if (_isCharging == value) return;
                _isCharging = value;
                Bindable.Raise(propertyChanged, this);
            }
        }

        [CreateProperty]
        public bool IsChargeFull => _chargeProgress >= 1f;

        [CreateProperty]
        public int ComboCount
        {
            get => _comboCount;
            set
            {
                if (_comboCount == value) return;
                _comboCount = value;
                Bindable.Raise(propertyChanged, this);
            }
        }

        [CreateProperty]
        public float ComboTimeRatio
        {
            get => _comboTimeRatio;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(_comboTimeRatio, value)) return;
                _comboTimeRatio = value;
                Bindable.Raise(propertyChanged, this);
            }
        }

        [CreateProperty]
        public int CurrentExp
        {
            get => _currentExp;
            set
            {
                if (_currentExp == value) return;
                _currentExp = value;
                Bindable.Raise(propertyChanged, this);
                Bindable.Raise(propertyChanged, this, nameof(NormalizedExp));
            }
        }

        [CreateProperty]
        public int RequiredExp
        {
            get => _requiredExp;
            set
            {
                if (_requiredExp == value) return;
                _requiredExp = value;
                Bindable.Raise(propertyChanged, this);
                Bindable.Raise(propertyChanged, this, nameof(NormalizedExp));
            }
        }

        [CreateProperty]
        public float NormalizedExp => _requiredExp > 0 ? Mathf.Clamp01((float)_currentExp / _requiredExp) : 0f;

        [CreateProperty]
        public int Level
        {
            get => _level;
            set
            {
                if (_level == value) return;
                _level = value;
                Bindable.Raise(propertyChanged, this);
            }
        }

        [CreateProperty]
        public IReadOnlyList<SkillCooldownEntryViewModel> SkillCooldowns => _skillCooldowns;

        // StatSO.Clone()과 동일하게 Instantiate로 개별 인스턴스를 만들고,
        // 슬롯별 쿨다운 엔트리는 Clone 시점에 새로 생성해 인스턴스 간 공유를 막는다.
        public object Clone()
        {
            var clone = (StatViewModel)Instantiate(this);
            clone.InitializeSkillSlots();
            return clone;
        }

        private void InitializeSkillSlots()
        {
            _skillCooldowns = new List<SkillCooldownEntryViewModel>(skillCooldownSlotCount);
            for (int i = 0; i < skillCooldownSlotCount; i++)
                _skillCooldowns.Add(new SkillCooldownEntryViewModel());
        }
    }
}
