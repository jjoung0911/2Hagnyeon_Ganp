using Agents.CombatSystem;
using UnityEngine;

namespace Enemy.Skills
{
    // Heavy HitType을 적용하는 탱커 전용 근접 공격 스킬
    // 거리 maxRange 이하일 때만 사용 — EnemyMeleeDamageCaster와 같은 GameObject에 부착해 사용
    public class EnemyTankerAttackSkill : AbstractAttackSkill, ISkillCondition
    {
        [SerializeField] private float maxRange = 1f;

        public bool CheckCondition(GameObject target)
        {
            if (target == null) return false;
            return Vector3.Distance(_enemy.transform.position, target.transform.position) <= maxRange;
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return !IsUsing && NormalizedCooldown >= 1f && CheckCondition(target);
        }

        protected override HitType GetHitType() => HitType.Heavy;
    }
}
