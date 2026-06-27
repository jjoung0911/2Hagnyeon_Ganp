using csiimnida.CSILib.SoundManager.RunTime;
using Agents.Modules;
using UnityEngine;

namespace Player
{
    public class PlayerCombatModule : MonoBehaviour, IModule
    {
        [SerializeField] private float detectRadius = 10f;
        [SerializeField] private float detectInterval = 0.2f;

        [Header("검 부착")]
        [SerializeField] private Transform sword;
        [SerializeField] private Transform backSocket;
        [SerializeField] private Transform handSocket;
        [SerializeField] private Vector3 backLocalPos;
        [SerializeField] private Vector3 backLocalEuler;
        [SerializeField] private Vector3 handLocalPos;
        [SerializeField] private Vector3 handLocalEuler;
        
        [Tooltip("평상시 필드 BGM")]
        [SerializeField] private SoundSo bgmField;
        [Tooltip("전투 BGM (근처에 적이 있을 때)")]
        [SerializeField] private SoundSo bgmCombat;

        public Transform LockOnTarget { get; private set; }

        private ISensor _sensor;
        private IAnimationTrigger _animTrigger;
        private Player _player;
        private AudioModule _audio;

        private float _lastDetectTime;
        private bool _enemyPresent;
        private bool _bgmInitialized;

        public void Initialize(ModuleOwner owner)
        {
            _player = owner as Player;
            _sensor = _player.GetModule<ISensor>();
            _animTrigger = _player.GetModule<IAnimationTrigger>();
            _audio = _player.GetModule<AudioModule>();
        }

        private void Start()
        {
            DetectEnemies();
        }

        private void Update()
        {
            if (Time.time < _lastDetectTime + detectInterval) return;
            _lastDetectTime = Time.time;
            DetectEnemies();
        }

        // 락온 대상 갱신용 적 감지
        private void DetectEnemies()
        {
            int count = _sensor.FindTargetInRadius(detectRadius);
            Transform nearest = FindNearest(count);
            bool enemyPresent = nearest != null;

            // 게임 시작 시 적이 없어도 필드 BGM이 한 번은 재생되도록 첫 감지를 항상 처리
            if (!_bgmInitialized || enemyPresent != _enemyPresent)
            {
                _audio?.PlayBGM(enemyPresent ? bgmCombat : bgmField);
                _bgmInitialized = true;
            }

            _enemyPresent = enemyPresent;
            LockOnTarget = nearest;
        }

        [ContextMenu("Attach Sword")]
        public void AttachSwordToHand()
        {
            if (sword == null || handSocket == null) return;
            sword.SetParent(handSocket, false);
            sword.SetLocalPositionAndRotation(handLocalPos, Quaternion.Euler(handLocalEuler));
        }

        [ContextMenu("Detach Sword")]
        public void AttachSwordToBack()
        {
            if (sword == null || backSocket == null) return;
            sword.SetParent(backSocket, false);
            sword.SetLocalPositionAndRotation(backLocalPos, Quaternion.Euler(backLocalEuler));
        }

        private Transform FindNearest(int count)
        {
            var results = _sensor.ColliderResults;
            Transform nearest = null;
            float minSqDist = float.MaxValue;
            Vector3 pos = _player.transform.position;

            for (int i = 0; i < count; i++)
            {
                if (results[i] == null) continue;
                float sqDist = (results[i].transform.position - pos).sqrMagnitude;
                if (sqDist < minSqDist)
                {
                    minSqDist = sqDist;
                    nearest = results[i].transform;
                }
            }
            return nearest;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectRadius);
        }
    }
}
