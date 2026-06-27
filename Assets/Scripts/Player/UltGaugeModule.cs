using System;
using Agents.Modules;
using UnityEngine;

namespace Player
{
    // Player GameObject에 컴포넌트로 부착. 스킬 히트·킬 등 외부 코드에서 AddGauge()를 호출해 채운다.
    public class UltGaugeModule : MonoBehaviour, IModule
    {
        [SerializeField] private float maxGauge = 100f;

        public event Action<float, float> OnGaugeChanged;
        public event Action OnUltReady;

        public float CurrentGauge { get; private set; }
        public float MaxGauge => maxGauge;
        public bool IsReady => CurrentGauge >= maxGauge;

        public void Initialize(ModuleOwner owner) { }

        public void AddGauge(float amount)
        {
            if (amount <= 0f) return;
            bool wasReady = IsReady;
            CurrentGauge = Mathf.Clamp(CurrentGauge + amount, 0f, maxGauge);
            OnGaugeChanged?.Invoke(CurrentGauge, maxGauge);
            if (!wasReady && IsReady)
                OnUltReady?.Invoke();
        }

        public bool TryConsumeUlt()
        {
            if (!IsReady) return false;
            CurrentGauge = 0f;
            OnGaugeChanged?.Invoke(CurrentGauge, maxGauge);
            return true;
        }
    }
}
