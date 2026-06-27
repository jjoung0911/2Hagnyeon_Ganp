using Agents.CombatSystem;
using UnityEngine;

namespace Enemy.Skills
{
    // MeleeEnemy 전용 강타 스킬 — EnemyMeleeDamageCaster와 같은 GameObject에 부착
    // attackRange·attackDamage를 일반 공격보다 크게 설정해 위협적인 일격을 구현
    public class EnemyMeleeHeavySkill : AbstractAttackSkill
    {
        public override bool CanUseSkill(GameObject target = null)
        {
            return !IsUsing && NormalizedCooldown >= 1f;
        }

        protected override HitType GetHitType() => HitType.Heavy;
    }
}
