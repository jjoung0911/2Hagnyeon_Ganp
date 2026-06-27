using UnityEngine;

namespace Enemy.Skills
{
    // RangedEnemy 전용 다중 발사 스킬 — EnemyMultiShotDamageCaster와 같은 GameObject에 부착
    // BT의 RotateToTargetAction 이후 호출해야 부채꼴이 정면을 향함
    public class EnemyRangedMultiShotSkill : AbstractAttackSkill
    {
        public override bool CanUseSkill(GameObject target = null)
        {
            return !IsUsing && NormalizedCooldown >= 1f;
        }
    }
}
