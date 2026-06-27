using Agents.CombatSystem;
using Agents.Modules;
using Enemy.Skills;
using UnityEngine;

namespace Enemy.Boss.Skills
{
    // 패턴 1: 근거리 기본 베기.
    // 0.6초 예비동작(애니메이션 윈드업) 동안 바닥 텔레그래프를 표시한 뒤
    // AbstractAttackSkill의 OnAttackStart 시점에 전방 부채꼴(EnemyConeDamageCaster)로 1회 판정한다.
    // HitType은 기본값(Light)을 사용해 경직을 유발하지 않는다 — 회피 가능한 기본 패턴.
    public class BasicSlashAttack : AbstractAttackSkill, ISkillCondition
    {
        [Header("사거리")]
        [SerializeField] private float minRange = 0f;
        [SerializeField] private float maxRange = 4f;

        [Header("바닥 텔레그래프")]
        [SerializeField] private AttackTelegraphView telegraphPrefab;
        [SerializeField] private float telegraphRadius = 4f;
        [SerializeField] private float telegraphAngle = 120f;
        [SerializeField] private float windupDuration = 0.6f;

        private BossPatternController _patternController;

        public override void Initialize(ISkillModule module)
        {
            base.Initialize(module);
            _patternController = _enemy?.GetModule<BossPatternController>();
        }

        public bool CheckCondition(GameObject target)
        {
            if (target == null) return false;
            float dist = Vector3.Distance(_enemy.transform.position, target.transform.position);
            return dist >= minRange && dist <= maxRange
                   && (_patternController == null || _patternController.CanUsePattern(BossPatternType.BasicSlash));
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return !IsUsing && NormalizedCooldown >= 1f && CheckCondition(target);
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _patternController?.RecordPatternUsed(BossPatternType.BasicSlash);

            if (telegraphPrefab == null) return;
            var telegraph = Instantiate(telegraphPrefab, transform.position, transform.rotation);
            telegraph.SetAngle(telegraphAngle);
            telegraph.Show(telegraphRadius, windupDuration);
        }
    }
}
