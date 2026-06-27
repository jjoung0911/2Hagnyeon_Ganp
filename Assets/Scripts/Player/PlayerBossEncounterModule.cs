using Agents.Modules;
using Agents.Modules.Movement;
using Enemy.Boss;
using JWLib.EventChannelSystem;
using UnityEngine;

namespace Player
{
    // 보스 등장 연출 동안 플레이어 입력을 잠그고, 연출이 끝나면 복원한다.
    public class PlayerBossEncounterModule : MonoBehaviour, IModule, IAfterInit
    {
        [Header("보스 이벤트")]
        [SerializeField] private EventChannelSO bossEventChannel;

        private IMoveData _moveData;

        public void Initialize(ModuleOwner owner)
        {
            _moveData = owner.GetModule<IMoveData>();
        }

        public void AfterInit()
        {
            bossEventChannel?.AddListener<BossIntroStartedEvent>(HandleIntroStarted);
            bossEventChannel?.AddListener<BossIntroEndedEvent>(HandleIntroEnded);
        }

        private void OnDestroy()
        {
            bossEventChannel?.RemoveListener<BossIntroStartedEvent>(HandleIntroStarted);
            bossEventChannel?.RemoveListener<BossIntroEndedEvent>(HandleIntroEnded);
        }

        private void HandleIntroStarted(BossIntroStartedEvent evt)
        {
            if (_moveData != null)
                _moveData.CanManualMove = false;
        }

        private void HandleIntroEnded(BossIntroEndedEvent evt)
        {
            if (_moveData != null)
                _moveData.CanManualMove = true;
        }
    }
}
