using System;
using Enemy.Skills;
using Unity.Behavior;
using UnityEngine;

namespace Enemy.BT.Conditions
{
    // ISkillCondition을 구현한 스킬이 하나라도 조건을 만족하면 true
    // SkillNumber 하드코딩 없이 조건 스킬들을 자동 탐색
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "CanUseSkillCondition", story: "[Enemy] can use skill to [TargetGameObject]", category: "Conditions", id: "82342f5f9aa85f96c22467877ae6526a")]
    public partial class CanUseSkillCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        public override bool IsTrue()
        {
            if (Enemy.Value == null || TargetGameObject.Value == null || Enemy.Value.SkillModule == null) return false;

            var conditions = Enemy.Value.GetComponentsInChildren<ISkillCondition>();
            foreach (var c in conditions)
            {
                if (c is AbstractEnemySkill skill && skill.CanUseSkill(TargetGameObject.Value))
                    return true;
            }
            return false;
        }
    }
}
