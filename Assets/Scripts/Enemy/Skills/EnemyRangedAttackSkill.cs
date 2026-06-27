using UnityEngine;

namespace Enemy.Skills
{
    // EnemyRangedDamageCaster와 같은 GameObject에 부착해 사용하는 원거리 공격 스킬
    // BT의 RotateToTargetAction이 선행되어 적이 타겟을 바라본 뒤 UseSkill을 호출해야 함
    public class EnemyRangedAttackSkill : AbstractAttackSkill
    {
        public override bool CanUseSkill(GameObject target = null)
        {
            return !IsUsing && NormalizedCooldown >= 1f;
        }
    }
}
