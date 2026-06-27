using System;
using Agents.CombatSystem;
using UnityEngine;

namespace Agents.Modules
{
    public interface IOneShotCaster
    {
        HitType CurrentHitType { get; set; }
        event Action<DamageData> OnHit;
        event Action<Collider, DamageData> OnKill;
        void SetDamageOverride(float amount);
        void CastOnce();
    }
}
