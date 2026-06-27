using System.Collections.Generic;
using UnityEngine;

namespace System.StatSystem
{
    [CreateAssetMenu(fileName = "New Stat", menuName = "SO/Stat/StatSO", order = 0)]
    public class StatSO : IndexedAsset, ICloneable
    {
        public delegate void ValueChangeHandler(StatSO stat, float current, float previous);

        public event ValueChangeHandler OnValueChanged;

        [field: SerializeField] public string StatName { get; private set; }
        [field: SerializeField] public Sprite StatIcon { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public bool IsPercent{ get; private set; }

        [SerializeField] private float baseVal;
        [SerializeField] private float minVal, maxVal;
        [SerializeField] private string describtion;

        private Dictionary<object, float> _modifyValueByKey = new();
        private float _combinedModifyVal;

        public float MaxVal => maxVal;
        public float MinVal => minVal;

        public float Value => Mathf.Clamp(baseVal + _combinedModifyVal, MinVal, MaxVal);
        public bool IsMax => Mathf.Approximately(Value, MaxVal);
        public bool IsMin => Mathf.Approximately(Value, MinVal);

        public float BaseValue
        {
            get => baseVal;
            set
            {
                float previous = Value;
                baseVal = Mathf.Clamp(value, MinVal, MaxVal);
                TryInvokeValueChangedEvent(Value, previous);
            }
        }

        public void AddModifier(object key, float val)
        {
            if (_modifyValueByKey.ContainsKey(key)) return;

            float previous = Value;
            _combinedModifyVal += val;
            _modifyValueByKey.Add(key, val);

            TryInvokeValueChangedEvent(Value, previous);
        }

        public void RemoveModifier(object key)
        {
            if (_modifyValueByKey.TryGetValue(key, out var value))
            {
                float previous = Value;
                _modifyValueByKey.Remove(key);
                _combinedModifyVal -= value;
                TryInvokeValueChangedEvent(Value, previous);
            }
        }
        private void TryInvokeValueChangedEvent(float current, float previous)
        {
            if(Mathf.Approximately(current, previous) == false)
                OnValueChanged?.Invoke(this, current, previous);
        }

        public object Clone()
        {
            return Instantiate(this);
        }
    }
}