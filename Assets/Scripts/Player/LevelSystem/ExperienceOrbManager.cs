using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using UnityEngine;

namespace Player.LevelSystem
{
    // ExperienceOrbDropEvent를 구독해 풀에서 경험치 오브를 꺼내 배치하고,
    // 오브가 플레이어에게 흡수되면 EnemyKilledEvent를 발행해 경험치를 지급한 뒤 풀로 반환한다
    public class ExperienceOrbManager : MonoBehaviour
    {
        [SerializeField] private EventChannelSO eventChannel;
        [SerializeField] private PoolManagerSO poolManagerAsset;
        [SerializeField] private PoolItemSO orbItem;

        private void Awake()
        {
            eventChannel.AddListener<ExperienceOrbDropEvent>(HandleOrbDrop);
        }

        private void OnDestroy()
        {
            eventChannel.RemoveListener<ExperienceOrbDropEvent>(HandleOrbDrop);
        }

        private void HandleOrbDrop(ExperienceOrbDropEvent evt)
        {
            ExperienceOrb orb = poolManagerAsset.Pop<ExperienceOrb>(orbItem);
            orb.Init(evt.Position, evt.ExpAmount);
            orb.OnCollected += HandleOrbCollected;
        }

        private void HandleOrbCollected(ExperienceOrb orb, int expAmount)
        {
            orb.OnCollected -= HandleOrbCollected;
            eventChannel.RaiseEvent(LevelEvents.EnemyKilledEvent.Init(expAmount));
            poolManagerAsset.Push(orb);
        }
    }
}
