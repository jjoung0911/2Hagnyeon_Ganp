using System;
using Agents.Modules;
using Player;
using Player.Skills;
using UnityEngine;

namespace Agents.CombatSystem
{
    public interface ISkillModule
    {
        public ModuleOwner Owner { get; }
        public bool IsSkillActive { get; }

        public event Action OnSkillEnd;
        public bool CanUseSkill(int index, GameObject target = null );
        public void UseSkill(int index, GameObject target = null );
        public int GetSkillIndex(object skill);
        public void InvokeAttackEnd();
    }
}