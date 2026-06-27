using System;
using Enemy;
using Enemy.Skills;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace _00.Scripts.Enemy.BT.Actions
{
    // ISkillCondition 조건을 만족하는 스킬을 자동 탐색해서 실행
    // SkillNumber 하드코딩 불필요
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "UseSkillAction", story: "[Enemy] use skill to [TargetGameObject]", category: "Action/Combat", id: "a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0d1")]
    public partial class UseSkillAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        // 스킬 종료 후 다음 행동 전까지 대기 시간 (보스 행동 사이 자연스러운 텀)
        [SerializeField] private float postSkillDelay = 0f;

        private bool _skillEnded;
        private float _skillEndTime;

        protected override Status OnStart()
        {
            var enemy = Enemy?.Value;
            if (enemy?.SkillModule == null) return Status.Failure;

            // ISkillCondition 스킬 중 조건을 만족하는 것을 높은 인덱스(강한 스킬)부터 탐색
            int index = FindUsableSkillIndex(enemy, TargetGameObject?.Value);
            if (index < 0) return Status.Failure;

            // 스킬 판정이 엉뚱한 방향을 향하는 현상 방지 — 실행 직전 타겟 방향으로 즉시 스냅
            SnapFaceTarget(enemy.transform, TargetGameObject?.Value);

            _skillEnded = false;
            _skillEndTime = float.MaxValue;
            enemy.SkillModule.OnSkillEnd += HandleSkillEnd;
            enemy.SkillModule.UseSkill(index, TargetGameObject?.Value);
            return Status.Running;
        }

        private static void SnapFaceTarget(Transform self, GameObject target)
        {
            if (target == null) return;
            Vector3 dir = target.transform.position - self.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                self.rotation = Quaternion.LookRotation(dir);
        }

        protected override Status OnUpdate()
        {
            if (!_skillEnded) return Status.Running;
            return Time.time >= _skillEndTime + postSkillDelay ? Status.Success : Status.Running;
        }

        protected override void OnEnd()
        {
            if (Enemy?.Value?.SkillModule == null) return;
            Enemy.Value.SkillModule.OnSkillEnd -= HandleSkillEnd;
            if (!_skillEnded)
                Enemy.Value.SkillModule.InvokeAttackEnd();
        }

        private void HandleSkillEnd()
        {
            _skillEnded = true;
            _skillEndTime = Time.time;
        }

        private static int FindUsableSkillIndex(AbstractEnemy enemy, GameObject target)
        {
            var conditions = enemy.GetComponentsInChildren<ISkillCondition>();
            // 높은 인덱스(뒤쪽) 스킬부터 우선 시도
            for (int i = conditions.Length - 1; i >= 0; i--)
            {
                if (conditions[i] is not AbstractEnemySkill skill) continue;
                if (skill.CanUseSkill(target))
                {
                    // EnemySkillModule 내 _skills 리스트에서 해당 스킬의 인덱스 찾기
                    return enemy.SkillModule.GetSkillIndex(skill);
                }
            }
            return -1;
        }
    }
}
