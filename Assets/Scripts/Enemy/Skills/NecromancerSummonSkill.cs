using Agents.CombatSystem;
using Agents.Modules;
using JWLib.AnimationSystem;
using JWLib.ObjectPool.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy.Skills
{
    // 소환 스킬 — 주변 적 수가 populationThreshold 이하이거나 쿨다운이 찬 경우 사용 가능.
    // (UseSkillAction/CanUseSkillCondition이 ISkillCondition 구현 스킬을 자동 탐색하므로
    //  Necromancer 프리팹에서 이 스킬을 EnemyProjectileSkill(Shadow Bolt)보다
    //  하위(나중) 자식으로 배치해야 소환이 우선 시도된다)
    public class NecromancerSummonSkill : AbstractEnemySkill, ISkillCondition
    {
        [Header("애니메이션")]
        [SerializeField] private AnimParamSO summonClip;
        [SerializeField] private float crossDuration = 0.1f;

        [Header("소환")]
        [SerializeField] private PoolManagerSO poolManagerAsset;
        [SerializeField] private PoolItemSO summonItem;          // Skeleton Warrior
        [SerializeField] private int summonCount = 2;
        [SerializeField] private float summonRadius = 5f;
        [SerializeField] private float navSampleDistance = 2f;

        [Header("인구 조건")]
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private int populationThreshold = 3;    // 주변 적 수가 이하면 쿨다운 무시
        [SerializeField] private int maxDetectCount = 16;

        private IAnimationTrigger _trigger;
        private Collider[] _detectBuffer;

        public override void Initialize(ISkillModule module)
        {
            base.Initialize(module);
            _trigger = _enemy?.GetModule<IAnimationTrigger>();
            _detectBuffer = new Collider[maxDetectCount];
        }

        private bool IsPopulationLow()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, summonRadius, _detectBuffer, enemyLayer);
            return count <= populationThreshold;
        }

        public bool CheckCondition(GameObject target)
        {
            return NormalizedCooldown >= 1f || IsPopulationLow();
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return !IsUsing && CheckCondition(target);
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);

            if (_trigger != null)
            {
                _trigger.OnAttack += HandleSummon;
                _trigger.OnAnimationEnd += HandleAnimationEnd;
            }

            if (summonClip != null)
                _renderer?.PlayClip(summonClip.ParamHash, crossDuration);
        }

        public override void StopSkill()
        {
            if (_trigger != null)
            {
                _trigger.OnAttack -= HandleSummon;
                _trigger.OnAnimationEnd -= HandleAnimationEnd;
            }

            base.StopSkill();
        }

        private void HandleSummon()
        {
            _trigger.OnAttack -= HandleSummon;
            if (summonItem == null || poolManagerAsset == null) return;

            for (int i = 0; i < summonCount; i++)
                SpawnOne();
        }

        private void SpawnOne()
        {
            Vector2 offset = Random.insideUnitCircle * summonRadius;
            Vector3 point = transform.position + new Vector3(offset.x, 0f, offset.y);

            if (!NavMesh.SamplePosition(point, out NavMeshHit hit, navSampleDistance, NavMesh.AllAreas))
                return;

            IPoolable warrior = poolManagerAsset.Pop<IPoolable>(summonItem);
            warrior.GameObject.transform.SetPositionAndRotation(hit.position, Quaternion.identity);
        }

        private void HandleAnimationEnd() => StopSkill();
    }
}
