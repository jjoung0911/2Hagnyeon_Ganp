using System.Collections;
using System.Collections.Generic;
using Agents.CombatSystem;
using Agents.Modules;
using Enemy.Skills;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using UnityEngine;

namespace Enemy.Boss.Skills
{
    // 패턴 3: 플레이어 위치에 바닥 경고 → waitDuration 후 폭발 데미지.
    // Phase2 = 플레이어 위치 1개 + 주변 랜덤 (phase2ExplosionCount - 1)개 동시 폭발.
    public class DarkWaveAttack : AbstractEnemySkill, ISkillCondition
    {
        [Header("타이밍")]
        [SerializeField] private float waitDuration = 1f;

        [Header("바닥 텔레그래프")]
        [SerializeField] private AttackTelegraphView telegraphPrefab;
        // 공격 범위(표시 + 실제 폭발 판정 공용) — 좁게 설정해 회피 여지를 준다
        [SerializeField] private float telegraphRadius = 2.5f;

        [Header("폭발")]
        [SerializeField] private PoolItemSO explosionVfx;
        [SerializeField] private EventChannelSO createChannel;
        [SerializeField] private HitType hitType = HitType.Medium;
        [SerializeField] private LayerMask hitLayer;

        [Header("Phase 2")]
        [SerializeField] private int phase2ExplosionCount = 3;
        [SerializeField] private float phase2SpreadRadius = 3f;

        private BossPatternController _patternController;
        private Coroutine _attackCoroutine;

        public override void Initialize(ISkillModule module)
        {
            base.Initialize(module);
            _patternController = _enemy?.GetModule<BossPatternController>();

            if (hitLayer.value == 0)
                Debug.LogWarning($"[DarkWaveAttack] hitLayer이 설정되지 않았습니다 — Explode() 피해 판정이 발생하지 않습니다: {name}");
        }

        public bool CheckCondition(GameObject target)
        {
            return target != null
                && (_patternController == null || _patternController.CanUsePattern(BossPatternType.DarkWave));
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return !IsUsing && NormalizedCooldown >= 1f && CheckCondition(target);
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _patternController?.RecordPatternUsed(BossPatternType.DarkWave);

            List<Vector3> positions = BuildExplosionPositions(target);

            foreach (Vector3 pos in positions)
            {
                if (telegraphPrefab != null)
                {
                    var telegraph = Instantiate(telegraphPrefab, pos, Quaternion.identity);
                    telegraph.SetAngle(360f);
                    telegraph.Show(telegraphRadius, waitDuration);
                }
            }

            _attackCoroutine = StartCoroutine(ExplodeAfterDelay(positions));
        }

        public override void StopSkill()
        {
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
            }

            base.StopSkill();
        }

        private List<Vector3> BuildExplosionPositions(GameObject target)
        {
            var positions = new List<Vector3>();
            if (target == null) return positions;

            Vector3 playerPos = target.transform.position;
            bool isPhase2 = _patternController != null
                            && _patternController.CurrentPhase == BossPhase.Phase2;
            int count = isPhase2 ? phase2ExplosionCount : 1;

            positions.Add(playerPos);
            for (int i = 1; i < count; i++)
            {
                Vector2 rnd = Random.insideUnitCircle * phase2SpreadRadius;
                positions.Add(playerPos + new Vector3(rnd.x, 0f, rnd.y));
            }

            return positions;
        }

        private IEnumerator ExplodeAfterDelay(List<Vector3> positions)
        {
            yield return new WaitForSeconds(waitDuration);

            foreach (Vector3 pos in positions)
                Explode(pos);

            StopSkill();
        }

        private void Explode(Vector3 position)
        {
            if (explosionVfx != null)
                createChannel?.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(
                    explosionVfx, position, Quaternion.identity));

            Collider[] hits = Physics.OverlapSphere(position, telegraphRadius, hitLayer);
            foreach (Collider hit in hits)
            {
                if (hit.TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(new DamageData(
                        SkillData.damage, position, Vector3.up, _enemy, false, hitType));
            }
        }
    }
}
