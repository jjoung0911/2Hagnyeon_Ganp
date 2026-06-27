using System.Collections;
using Enemy;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace System
{
    public class SpawnManager : MonoBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private PoolManagerSO createChannel;
        [SerializeField] private PoolItemSO[] enemies;
        [SerializeField] private EventChannelSO enemyChannel;
        [SerializeField] private float minRadius;
        [SerializeField] private float maxRadius;
        [SerializeField] private float minWaitT;
        [SerializeField] private float maxWaitT;
        [SerializeField] private int maxSpawnRetries = 5;

        private void Awake()
        {
            enemyChannel.AddListener<PushEvent>(HandlePush);
        }

        private void OnDestroy()
        {
            enemyChannel.RemoveListener<PushEvent>(HandlePush);
        }

        private void Start()
        {
            StartCoroutine(SpawnCoroutine());
        }

        private void HandlePush(PushEvent evt)
        {
            createChannel.Push(evt.Target);
        }

        private IEnumerator SpawnCoroutine()
        {
            SpawnEnemy();
            float waitT = UnityEngine.Random.Range(minWaitT, maxWaitT);
            yield return new WaitForSeconds(waitT);
            StartCoroutine(SpawnCoroutine());
        }

        private void SpawnEnemy()
        {
            for (int i = 0; i < maxSpawnRetries; i++)
            {
                if (!TryFindSpawnPosition(out Vector3 spawnPosition))
                {
                    continue;
                }

                int randomEnemy = UnityEngine.Random.Range(0, enemies.Length);
                AbstractEnemy monster = createChannel.Pop<AbstractEnemy>(enemies[randomEnemy]);
                monster.NavMovement.Warp(spawnPosition);
                monster.TryEngageNearestTarget();
                return;
            }
        }

        private bool TryFindSpawnPosition(out Vector3 spawnPosition)
        {
            spawnPosition = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].position;
            
            // float randomRadius = UnityEngine.Random.Range(minRadius, maxRadius);
            // Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * randomRadius;
            // Vector3 candidate = spawnPosition + new Vector3(randomCircle.x, 0, randomCircle.y);


            if (!NavMesh.SamplePosition(spawnPosition, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                Debug.Log("SPAWN FAIL");
                return false;
            }

            spawnPosition = hit.position;
            return true;
        }
    }
}