using System.Collections;
using System.Core;
using Enemy;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using UI.Events;
using UnityEngine;
using UnityEngine.AI;

namespace System.DifficultySystem
{
    // 무한 스폰 난이도 매니저. enemyPool에서 무작위로 적을 골라 계속 스폰한다.
    // 웨이브 같은 단계 구분 없이 경과 시간(ElapsedTime)에 비례해 스폰 간격이 점점 짧아지고
    // 한 번에 스폰되는 적 수도 늘어나며, 적의 스탯/데미지도 연속적으로 강해진다.
    // 진행 상황(경과 시간/생존 적 수/처치 수)은 uiChannel을 통해 UI에 전달된다.
    public class DifficultyManager : MonoSingleton<DifficultyManager>
    {
        [SerializeField] private PoolManagerSO createChannel;
        [SerializeField] private EventChannelSO enemyChannel;
        [SerializeField] private EventChannelSO uiChannel;
        [SerializeField] private PoolItemSO[] enemyPool;
        [SerializeField] private int maxSpawnRetries = 5;

        [Header("보스 등장")]
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private Transform bossSpawnPoint;
        [Tooltip("이 경과 시간(초)에 도달하면 보스가 등장한다")]
        [SerializeField] private float bossSpawnTime = 135f;

        [Header("스폰 진행 (경과 시간 기준 연속 증가)")]
        [SerializeField] private float initialSpawnInterval = 1.5f;
        [SerializeField] private float minSpawnInterval = 0.2f;
        [Tooltip("분당 스폰 간격 감소량(초)")]
        [SerializeField] private float spawnIntervalDecreasePerMinute = 0.4f;
        [Tooltip("분당 동시 스폰 개수 증가량")]
        [SerializeField] private float spawnCountIncreasePerMinute = 1.33f;
        [SerializeField] private float minRadius = 8f;
        [SerializeField] private float maxRadius = 20f;

        [Header("적 스탯 스케일링 (경과 시간 기준 연속 증가)")]
        [Tooltip("분당 적 HP 등 스탯 증가율 (0.48 = 1분마다 +48%, 시작 시점 배율 1)")]
        [SerializeField] private float statIncreasePerMinute = 0.48f;
        [Tooltip("분당 적 데미지 증가율 (0.32 = 1분마다 +32%, 시작 시점 배율 1)")]
        [SerializeField] private float damageIncreasePerMinute = 0.32f;

        [Header("플레이어 업그레이드 보정")]
        [Tooltip("적 스탯 증가 속도를 상쇄하기 위해 분당 업그레이드 효과치를 증폭하는 비율 (0.4 = 1분마다 효과 +40%)")]
        [SerializeField] private float upgradeAmplifyPerMinute = 0.4f;

        private Transform _playerTrm;
        private int _aliveCount;
        private int _totalSpawnedCount;
        private int _killCount;
        private float _elapsedTime;
        private float _spawnInterval;
        private int _spawnCountPerTick = 1;
        private Coroutine _spawnLoopCoroutine;
        private bool _stopped;
        private bool _bossSpawned;

        public float ElapsedTime => _elapsedTime;
        public int KillCount => _killCount;

        protected override void Awake()
        {
            base.Awake();
            _playerTrm = FindFirstObjectByType<Player.Player>().transform;
            enemyChannel.AddListener<PushEvent>(HandlePush);
        }

        private void OnDestroy()
        {
            enemyChannel.RemoveListener<PushEvent>(HandlePush);
        }

        private void Start()
        {
            _spawnInterval = initialSpawnInterval;
            _spawnLoopCoroutine = StartCoroutine(RunSpawnLoop());
        }

        private void Update()
        {
            if (_stopped) return;

            _elapsedTime += Time.deltaTime;
            float elapsedMinutes = _elapsedTime / 60f;

            _spawnInterval = Mathf.Max(minSpawnInterval, initialSpawnInterval - elapsedMinutes * spawnIntervalDecreasePerMinute);
            _spawnCountPerTick = 1 + Mathf.FloorToInt(elapsedMinutes * spawnCountIncreasePerMinute);

            if (!_bossSpawned && _elapsedTime >= bossSpawnTime)
                SpawnBoss();

            RaiseProgress();
        }

        // 게임오버 등으로 난이도 진행을 멈춘다. 스폰 코루틴과 시간 누적을 정지한다.
        public void Stop()
        {
            if (_stopped) return;
            _stopped = true;

            if (_spawnLoopCoroutine != null) StopCoroutine(_spawnLoopCoroutine);
            _spawnLoopCoroutine = null;
        }

        private void HandlePush(PushEvent evt)
        {
            createChannel.Push(evt.Target);

            _aliveCount = Mathf.Max(0, _aliveCount - 1);
            _killCount++;
            RaiseProgress();
        }

        // bossSpawnTime에 도달하면 보스를 1회 생성한다.
        // 생성된 보스의 BossIntroController가 자신의 AfterInit()에서 등장 연출 이벤트를 발행한다.
        [ContextMenu("SPAWN BOS")]
        private void SpawnBoss()
        {
            _bossSpawned = true;
            if (bossPrefab == null) return;

            Vector3 spawnPosition = bossSpawnPoint != null ? bossSpawnPoint.position : _playerTrm.position;
            Quaternion spawnRotation = bossSpawnPoint != null ? bossSpawnPoint.rotation : Quaternion.identity;
            Instantiate(bossPrefab, spawnPosition, spawnRotation);
        }

        private IEnumerator RunSpawnLoop()
        {
            while (true)
            {
                for (int i = 0; i < _spawnCountPerTick; i++)
                    SpawnEnemy(enemyPool[UnityEngine.Random.Range(0, enemyPool.Length)]);

                yield return new WaitForSeconds(_spawnInterval);
            }
        }

        private void SpawnEnemy(PoolItemSO item)
        {
            if (_aliveCount >= 300) return;
            _totalSpawnedCount++;
            for (int i = 0; i < maxSpawnRetries; i++)
            {
                if (!TryFindSpawnPosition(out Vector3 spawnPosition))
                    continue;

                AbstractEnemy monster = createChannel.Pop<AbstractEnemy>(item);
                if (monster == null)
                {
                    Debug.LogWarning($"DifficultyManager: 풀에서 적을 가져오지 못했습니다. PoolItem({item?.name}) 등록 여부를 확인하세요.");
                    return;
                }
                monster.NavMovement.Warp(spawnPosition);
                // 스폰 시점(ResetItem 이후)에 현재 경과 시간 배율로 스탯/데미지 스케일링
                monster.ApplyDifficultyScale(GetStatMultiplier(), GetDamageMultiplier());
                monster.TryEngageNearestTarget();
                _aliveCount++;
                RaiseProgress();
                return;
            }

            Debug.LogWarning("DifficultyManager: 스폰 위치를 찾지 못해 적을 생성하지 못했습니다.");
        }

        // 시작 시점 = 1배, 이후 경과 시간에 비례해 선형 증가
        private float GetStatMultiplier() => 1f + (_elapsedTime / 60f) * statIncreasePerMinute;
        private float GetDamageMultiplier() => 1f + (_elapsedTime / 60f) * damageIncreasePerMinute;

        // 적 스탯 증가를 상쇄하기 위한 업그레이드 효과 증폭 배율 — UpgradeDataSO가 Apply()/GetEffectDetail()에서 사용
        public float GetUpgradeAmplifier() => 1f + (_elapsedTime / 60f) * upgradeAmplifyPerMinute;

        private bool TryFindSpawnPosition(out Vector3 spawnPosition)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(minRadius, maxRadius);
            spawnPosition = _playerTrm.position + new Vector3(offset.x, 0f, offset.y);

            if (!NavMesh.SamplePosition(spawnPosition, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                return false;

            spawnPosition = hit.position;
            return true;
        }

        private void RaiseProgress()
        {
            uiChannel.RaiseEvent(UIEventCache.DifficultyProgress.Init(_elapsedTime, _aliveCount, _totalSpawnedCount, _killCount));
        }
    }
}
