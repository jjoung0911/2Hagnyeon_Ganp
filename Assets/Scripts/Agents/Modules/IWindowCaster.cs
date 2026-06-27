using System;
using Agents.CombatSystem;
using UnityEngine;

namespace Agents.Modules
{
    public interface IWindowCaster
    {
        HitType CurrentHitType { get; set; }
        event Action<DamageData> OnHit;
        event Action<Collider, DamageData> OnKill;
        void SetDamageOverride(float amount);
        void StartCasting();
        void StopCasting();
    }
}
