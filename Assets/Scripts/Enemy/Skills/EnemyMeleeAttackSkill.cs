using UnityEngine;

namespace Enemy.Skills
{
    // 일반 근접 공격 스킬 — maxRange 이하일 때만 사용 (UseSkillAction/CanUseSkillCondition이 자동 탐색)
    public class EnemyMeleeAttackSkill : AbstractAttackSkill, ISkillCondition
    {
        [SerializeField] private float maxRange = 2f;

        public bool CheckCondition(GameObject target)
        {
            if (target == null) return false;
            return Vector3.Distance(_enemy.transform.position, target.transform.position) <= maxRange;
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return !IsUsing && NormalizedCooldown >= 1f && CheckCondition(target);
        }
    }
}
