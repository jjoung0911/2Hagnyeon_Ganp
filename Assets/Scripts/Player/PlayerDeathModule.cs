using __.GameModules.PlayerData;
using Agents.Modules;
using Agents.Modules.Movement;
using JWLib.EventChannelSystem;
using UnityEngine;

namespace Player
{
    // 플레이어 사망 시 입력을 완전히 잠그고(이동·대시·스킬·달리기 전부),
    // 씬 리로드로 새 판이 시작될 때 입력을 복구한다.
    // InputReader는 ScriptableObject라 씬 리로드 후에도 비활성 상태가 유지되므로,
    // 새 판 시작(AfterInit) 때 방어적으로 다시 켜 주는 것이 핵심이다.
    public class PlayerDeathModule : MonoBehaviour, IModule, IAfterInit
    {
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private InputReader input;

        private IMoveData _moveData;

        public void Initialize(ModuleOwner owner)
        {
            _moveData = owner.GetModule<IMoveData>();
        }

        public void AfterInit()
        {
            // 새 판 시작 시 입력 복구 (이전 판 사망으로 꺼진 채 남았을 수 있음)
            input?.SetInputEnabled(true);
            playerChannel?.AddListener<PlayerDiedEvent>(HandlePlayerDied);
        }

        private void OnDestroy()
        {
            playerChannel?.RemoveListener<PlayerDiedEvent>(HandlePlayerDied);
        }

        private void HandlePlayerDied(PlayerDiedEvent _)
        {
            input?.SetInputEnabled(false);
            if (_moveData != null)
                _moveData.CanManualMove = false;
        }
    }
}
