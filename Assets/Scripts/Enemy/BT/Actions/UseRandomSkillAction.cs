using System;
using Enemy.Skills;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Enemy.BT.Actions
{
    // 전체 스킬 중 랜덤 인덱스를 뽑아, 그 스킬이 사용 가능하면 실행. 불가능하면 Failure → Chase로 복귀
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "UseRandomSkillAction", story: "[Enemy] use random skill to [TargetGameObject]", category: "Action/Combat", id: "b1c2d3e4f5a6b7c8d9e0f1a2b3c4d5e6")]
    public partial class UseRandomSkillAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        private bool _skillEnded;

        protected override Status OnStart()
        {
            var enemy = Enemy?.Value;
            if (enemy?.SkillModule == null) return Status.Failure;

            var skills = enemy.GetComponentsInChildren<AbstractEnemySkill>();
            if (skills.Length == 0) return Status.Failure;

            // 전체 스킬 중 랜덤 인덱스를 뽑아 사용 가능 여부 확인
            int randomSkillPos = UnityEngine.Random.Range(0, skills.Length);
            var pickedSkill = skills[randomSkillPos];

            if (!pickedSkill.CanUseSkill(TargetGameObject?.Value)) return Status.Failure;

            int index = enemy.SkillModule.GetSkillIndex(pickedSkill);
            if (index < 0) return Status.Failure;

            _skillEnded = false;
            enemy.SkillModule.OnSkillEnd += HandleSkillEnd;
            enemy.SkillModule.UseSkill(index, TargetGameObject?.Value);
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            return _skillEnded ? Status.Success : Status.Running;
        }

        protected override void OnEnd()
        {
            if (Enemy?.Value?.SkillModule == null) return;
            Enemy.Value.SkillModule.OnSkillEnd -= HandleSkillEnd;
            if (!_skillEnded)
                Enemy.Value.SkillModule.InvokeAttackEnd();
        }

        private void HandleSkillEnd() => _skillEnded = true;
    }
}
