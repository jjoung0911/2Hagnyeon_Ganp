using Agents.CombatSystem;
using Agents.Modules;
using UnityEngine.Events;

namespace Agents
{
    public abstract class Agent : ModuleOwner, IDamageable, IKillable
    {
        public bool IsDead { get; protected set; }

        public UnityEvent OnDeath;
        public UnityEvent OnHit;
        
        public ActionDataModule ActionData { get; private set; }
        public HealthModule Health { get; private set; }

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void InitializeModules()
        {
            base.InitializeModules();
            ActionData = GetModule<ActionDataModule>();
            Health = GetModule<HealthModule>();
        }

        protected override void AfterInit()
        {
            base.AfterInit();
            if (Health != null) Health.OnDeath += HandleDeath;
        }

        protected virtual void OnDestroy()
        {
            if (Health != null) Health.OnDeath -= HandleDeath;
        }

        protected virtual void HandleHitEvent() { }

        protected virtual void HandleDeath()
        {
            IsDead = true;
            OnDeath?.Invoke();
        }

        
        public void TakeDamage(DamageData damageData)
        {
            if (IsDead) return;
            if (Health != null && Health.IsInvincible) return;

            if (ActionData != null)
            {
                ActionData.HitPoint = damageData.HitPoint;
                ActionData.HitNormal = damageData.HitNormal;
                ActionData.Attacker = damageData.Attacker;
                ActionData.HitType = damageData.HitType;
            }

            OnHit?.Invoke();

            Health?.ApplyDamage(damageData.DamageAmount);
        }
    }
}
