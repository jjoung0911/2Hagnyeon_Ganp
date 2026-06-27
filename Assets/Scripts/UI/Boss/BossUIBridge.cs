using Agents.Modules;
using Enemy.Boss;
using JWLib.EventChannelSystem;
using UI.Events;
using UnityEngine;

namespace UI.Boss
{
    // 보스 프리팹에 부착. 자신의 HealthModule을 구독해 uiChannel에 BossHpChangedEvent를 발행한다.
    // bossEventChannel이 연결돼 있으면 BossIntroStartedEvent 시점에 HP바를 처음 표시하고,
    // 연결돼 있지 않으면 기존처럼 Start() 시점에 즉시 표시한다.
    public class BossUIBridge : MonoBehaviour
    {
        [SerializeField] private string bossName = "BOSS";
        [SerializeField] private EventChannelSO uiChannel;
        [SerializeField] private EventChannelSO bossEventChannel;

        private HealthModule _healthModule;

        private void Awake()
        {
            _healthModule = GetComponent<HealthModule>()
                            ?? GetComponentInParent<HealthModule>()
                            ?? GetComponentInChildren<HealthModule>();
        }

        private void Start()
        {
            if (_healthModule == null)
            {
                Debug.LogWarning($"[BossUIBridge] HealthModule을 찾지 못했습니다: {name}");
                return;
            }
            _healthModule.OnHealthChangeEvent += HandleHpChanged;

            if (bossEventChannel != null)
                bossEventChannel.AddListener<BossIntroStartedEvent>(HandleIntroStarted);
            else
                RaiseHpChanged();
        }

        private void OnDestroy()
        {
            if (_healthModule != null)
                _healthModule.OnHealthChangeEvent -= HandleHpChanged;

            bossEventChannel?.RemoveListener<BossIntroStartedEvent>(HandleIntroStarted);
        }

        private void HandleIntroStarted(BossIntroStartedEvent evt)
        {
            bossEventChannel.RemoveListener<BossIntroStartedEvent>(HandleIntroStarted);
            RaiseHpChanged();
        }

        private void HandleHpChanged(float current, float previous, float max)
        {
            uiChannel.RaiseEvent(UIEventCache.BossHpChanged.Init(bossName, current, max));
        }

        private void RaiseHpChanged()
        {
            uiChannel.RaiseEvent(UIEventCache.BossHpChanged.Init(bossName, _healthModule.CurrentHp, _healthModule.MaxHp));
        }
    }
}
