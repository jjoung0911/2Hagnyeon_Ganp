using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.ViewModels
{
    // 스킬 쿨다운 슬롯 1개의 바인딩 대상.
    // SkillCooldownGroupView가 슬롯 인덱스별로 이 객체를 dataSource로 직접 연결한다
    // (리스트 바인딩 없이, 기존 SkillCooldownView의 "skillIndex로 필터링" 패턴을
    //  "객체 참조 직접 연결"로 대체).
    public class SkillCooldownEntryViewModel : INotifyBindablePropertyChanged
    {
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        private int _skillIndex = -1;
        private string _skillName = string.Empty;
        private Sprite _icon;
        private float _normalizedCooldown;
        private float _remainingSeconds;
        private bool _isReady = true;

        [CreateProperty]
        public int SkillIndex
        {
            get => _skillIndex;
            set
            {
                if (_skillIndex == value) return;
                _skillIndex = value;
                Bindable.Raise(propertyChanged, this);
            }
        }

        [CreateProperty]
        public string SkillName
        {
            get => _skillName;
            set
            {
                if (_skillName == value) return;
                _skillName = value;
                Bindable.Raise(propertyChanged, this);
            }
        }

        [CreateProperty]
        public Sprite Icon
        {
            get => _icon;
            set
            {
                if (_icon == value) return;
                _icon = value;
                Bindable.Raise(propertyChanged, this);
            }
        }

        [CreateProperty]
        public float NormalizedCooldown
        {
            get => _normalizedCooldown;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(_normalizedCooldown, value)) return;
                _normalizedCooldown = value;
                Bindable.Raise(propertyChanged, this);
            }
        }

        [CreateProperty]
        public float RemainingSeconds
        {
            get => _remainingSeconds;
            set
            {
                if (Mathf.Approximately(_remainingSeconds, value)) return;
                _remainingSeconds = value;
                Bindable.Raise(propertyChanged, this);
            }
        }

        [CreateProperty]
        public bool IsReady
        {
            get => _isReady;
            set
            {
                if (_isReady == value) return;
                _isReady = value;
                Bindable.Raise(propertyChanged, this);
            }
        }
    }
}
