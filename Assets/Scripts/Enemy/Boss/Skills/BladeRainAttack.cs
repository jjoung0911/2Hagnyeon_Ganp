using System.Collections;
using Agents.CombatSystem;
using Agents.Modules;
using Enemy.Skills;
using JWLib.AnimationSystem;
using UnityEngine;

namespace Enemy.Boss.Skills
{
    // 패턴 5 (Phase2 전용): 검 들어올림 + 캐스팅 후 타겟 주변 랜덤 위치에 검을 낙하시킨다.
    // 플레이어가 이동해서 회피하도록 유도하는 광역 패턴.
    public class BladeRainAttack : AbstractEnemySkill, ISkillCondition
    {
        [Header("애니메이션")]
        [SerializeField] private AnimParamSO castClip;
        [SerializeField] private float crossDuration = 0.1f;

        [Header("검 낙하")]
        [SerializeField] private BladeRainProjectile bladePrefab;
        [SerializeField] private int bladeCount = 6;
        // 검 사이의 낙하 시작 간격 — 0이면 전부 동시에 떨어져 부자연스러움
        [SerializeField] private float bladeSpawnInterval = 0.12f;
        // 텔레그래프와 실제 낙하 범위가 이 값으로 통일된다
        [SerializeField] private float spawnRadius = 10f;
        [SerializeField] private float raycastHeight = 10f;
        [SerializeField] private LayerMask groundMask;

        [Header("범위 텔레그래프")]
        [SerializeField] private AttackTelegraphView telegraphPrefab;
        [SerializeField] private float telegraphDuration = 0.8f;

        private BossPatternController _patternController;
        private IAnimationTrigger _trigger;
        private GameObject _target;
        private Coroutine _spawnRoutine;

        public override void Initialize(ISkillModule module)
        {
            base.Initialize(module);
            _patternController = _enemy?.GetModule<BossPatternController>();
            _trigger = _enemy?.GetModule<IAnimationTrigger>();
        }

        public bool CheckCondition(GameObject target)
        {
            return target != null
                   && _patternController != null
                   && _patternController.CurrentPhase == BossPhase.Phase2
                   && _patternController.CanUsePattern(BossPatternType.BladeRain);
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return !IsUsing && NormalizedCooldown >= 1f && CheckCondition(target);
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _patternController?.RecordPatternUsed(BossPatternType.BladeRain);
            _target = target;

            if (telegraphPrefab != null && _target != null)
            {
                var telegraph = Instantiate(telegraphPrefab, _target.transform.position, Quaternion.identity);
                telegraph.SetAngle(360f);
                telegraph.Show(spawnRadius, telegraphDuration);
            }

            if (_trigger != null)
            {
                _trigger.OnAttackStart += HandleAttackStart;
                _trigger.OnAnimationEnd += HandleAnimationEnd;
            }

            if (castClip != null)
                _renderer?.PlayClip(castClip.ParamHash, crossDuration);
        }

        public override void StopSkill()
        {
            if (_trigger != null)
            {
                _trigger.OnAttackStart -= HandleAttackStart;
                _trigger.OnAnimationEnd -= HandleAnimationEnd;
            }

            if (_spawnRoutine != null)
            {
                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }

            base.StopSkill();
        }

        private void HandleAttackStart()
        {
            if (bladePrefab == null || _target == null) return;
            _spawnRoutine = StartCoroutine(SpawnBladesRoutine());
        }

        // spawnRadius 기반 랜덤 위치로 검을 떨어뜨려 텔레그래프 범위와 실제 낙하 범위를 일치시킨다
        private IEnumerator SpawnBladesRoutine()
        {
            Vector3 center = _target.transform.position;
            float groundY  = center.y;

            for (int i = 0; i < bladeCount; i++)
            {
                Vector2 rnd      = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPos = center + new Vector3(rnd.x, 0f, rnd.y);
                Vector3 rayOrigin = new Vector3(spawnPos.x, spawnPos.y + raycastHeight, spawnPos.z);

                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundMask))
                    spawnPos = hit.point;
                else
                    spawnPos.y = groundY;

                var blade = Instantiate(bladePrefab);
                blade.Begin(_enemy, spawnPos);

                if (bladeSpawnInterval > 0f)
                    yield return new WaitForSeconds(bladeSpawnInterval);
            }

            _spawnRoutine = null;
        }

        private void HandleAnimationEnd() => StopSkill();
    }
}
