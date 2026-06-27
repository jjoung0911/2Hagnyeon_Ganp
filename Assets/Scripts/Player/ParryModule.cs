using Agents;
using Agents.CombatSystem;
using Agents.Modules;
using UnityEngine;

namespace Player
{
    public class ParryModule : MonoBehaviour, IModule, IAfterInit, IParryable
    {
        [SerializeField] private float parryRadius = 2.5f; // 투사체 패링 판정 반경

        private Agent _owner;
        private PlayerCombatModule _combatModule;

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner as Agent;
        }

        public void AfterInit()
        {
            _combatModule = _owner.GetModule<PlayerCombatModule>();
        }

        // 1순위: 잠금 대상이 근접 스킬 사용 중 → 공격 즉시 중단
        // 2순위: 반경 내 IParryableAttack(투사체 등) → OnParried 호출
        public bool TryParry(out ModuleOwner attacker, out Vector3 hitPoint)
        {
            attacker = null;
            hitPoint = _owner.transform.position;

            // 근접 공격 패링
            Transform target = _combatModule?.LockOnTarget;
            if (target != null)
            {
                var enemyOwner = target.GetComponentInParent<ModuleOwner>();
                if (enemyOwner != null)
                {
                    var enemySkills = enemyOwner.GetModule<ISkillModule>();
                    if (enemySkills != null && enemySkills.IsSkillActive)
                    {
                        enemySkills.InvokeAttackEnd();
                        attacker = enemyOwner;
                        hitPoint = target.position;
                        return true;
                    }
                }
            }

            return TryParryProjectile(out attacker, out hitPoint);
        }

        // 패링 윈도우 중 Update마다 호출 — 투사체만 지속 감지
        public bool TryParryProjectile(out ModuleOwner attacker, out Vector3 hitPoint)
        {
            attacker = null;
            hitPoint = _owner.transform.position;

            var parryable = ParryableRegistry.FindNearest(_owner.transform.position, parryRadius, out hitPoint);
            if (parryable == null) return false;

            attacker = parryable.Attacker;
            parryable.OnParried(hitPoint);
            return true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, parryRadius);
        }
#endif
    }
}
