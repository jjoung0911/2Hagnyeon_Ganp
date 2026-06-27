using JWLib.EventChannelSystem;
using Player.LevelSystem;
using UnityEngine;

namespace Agents.Modules
{
    // 이 Agent가 사망하면 경험치 보상 이벤트를 발행한다. 적 GameObject 하위에 부착.
    public class ExpRewardModule : MonoBehaviour, IModule, IAfterInit
    {
        [SerializeField] private int expReward = 10;
        [SerializeField] private EventChannelSO eventChannel;

        private HealthModule _health;

        public void Initialize(ModuleOwner owner)
        {
            _health = owner.GetModule<HealthModule>();
        }

        public void AfterInit()
        {
            if (_health != null) _health.OnDeath += HandleDeath;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnDeath -= HandleDeath;
        }

        private void HandleDeath()
        {
            eventChannel.RaiseEvent(LevelEvents.ExperienceOrbDropEvent.Init(transform.position, expReward));
        }
    }
}
