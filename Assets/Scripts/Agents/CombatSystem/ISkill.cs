using System;
using UnityEngine;

namespace Agents.CombatSystem
{
    public interface ISkill
    {
        event Action OnSkillEnd;
        public SkillDataSO SkillData { get; }
        public float NormalizedCooldown { get; }
        public bool IsUsing { get; }

        public void Initialize(ISkillModule module);

        public bool CanUseSkill(GameObject target = null);
        public void UseSkill(GameObject target = null);
        public void StopSkill();

    }
}