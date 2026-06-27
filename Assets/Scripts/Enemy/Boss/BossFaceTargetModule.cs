using Agents.CombatSystem;
using Agents.Modules;
using Enemy.BT;
using Unity.Behavior;
using UnityEngine;

namespace Enemy.Boss
{
    // 보스가 전투 중 항상 타겟(BtVars.TargetGameObject)을 바라보게 한다.
    // 스킬 사용 중에는 회전하지 않아 각 스킬의 모션/판정 방향을 보존한다.
    public class BossFaceTargetModule : MonoBehaviour, IModule, IAfterInit
    {
        [SerializeField] private float rotateSpeed = 480f; // 도/초

        private AbstractEnemy _enemy;
        private ISkillModule _skillModule;

        public void Initialize(ModuleOwner owner)
        {
            _enemy = owner as AbstractEnemy;
        }

        public void AfterInit()
        {
            _skillModule = _enemy?.GetModule<ISkillModule>();

            // 회전을 이 모듈이 직접 제어하므로 NavMeshAgent의 자동 회전과 충돌하지 않도록 비활성화
            if (_enemy?.NavMovement?.NavMeshAgent != null)
                _enemy.NavMovement.NavMeshAgent.updateRotation = false;
        }

        private void Update()
        {
            if (_enemy == null || _enemy.IsDead) return;
            if (_skillModule != null && _skillModule.IsSkillActive) return;
            if (!_enemy.GetVariable(BtVars.TargetGameObject, out BlackboardVariable<GameObject> targetVar)) return;

            GameObject target = targetVar.Value;
            if (target == null) return;

            Vector3 dir = target.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(dir), rotateSpeed * Time.deltaTime);
        }
    }
}
