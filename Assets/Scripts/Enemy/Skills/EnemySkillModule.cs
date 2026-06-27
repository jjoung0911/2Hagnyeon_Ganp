using System;
using System.Collections;
using System.Collections.Generic;
using _00.Scripts.Enemy.BT;
using Agents.CombatSystem;
using Agents.Modules;
using Player;
using Player.Skills;
using UnityEngine;

namespace Enemy.Skills
{
    public class EnemySkillModule : MonoBehaviour, IModule, ISkillModule
    {
        public ModuleOwner Owner { get; private set; }
        public AbstractEnemy Enemy { get; private set; }
        public bool IsSkillActive => _currentSkill?.IsUsing ?? false;
        public event Action OnSkillEnd;

        private List<AbstractEnemySkill> _skills;
        private AbstractEnemySkill _currentSkill;

        public void Initialize(ModuleOwner owner)
        {
            Owner = owner;
            Enemy = Owner as AbstractEnemy;
            
            foreach (var caster in GetComponentsInChildren<AbstractDamageCaster>())
                caster.InitCaster(owner);

            _skills = new List<AbstractEnemySkill>(GetComponentsInChildren<AbstractEnemySkill>());
            foreach (var skill in _skills)
                skill.Initialize(this);

            StartCoroutine(CheckCombatState());
        }

        public IEnumerator CheckCombatState()
        {
            yield return new WaitForSeconds(0.8f);
            foreach (var skill in _skills)
            {
                if (skill.CanUseSkill())
                {
                    Enemy.StateChannel.SendEventMessage(EnemyState.COMBAT);
                    yield break;
                }
            }
        }

        public bool CanUseSkill(int index, GameObject target = null)
        {
            // -1: 사용 가능한 스킬이 하나라도 있으면 true
            if (index == -1) return _skills.Exists(s => s.CanUseSkill(target));
            if (index < 0 || index >= _skills.Count) return false;
            return _skills[index].CanUseSkill(target);
        }

        public void UseSkill(int index, GameObject target = null)
        {
            // -1: 높은 인덱스(강한 스킬)부터 우선 시도
            if (index == -1)
            {
                for (int i = _skills.Count - 1; i >= 0; i--)
                {
                    if (_skills[i].CanUseSkill(target))
                    {
                        index = i;
                        break;
                    }
                }
                if (index == -1) return;
            }
            if (!CanUseSkill(index, target)) return;
            _currentSkill = _skills[index];
            _currentSkill.OnSkillEnd += HandleSkillEnd;
            _currentSkill.UseSkill(target);
        }

        public int GetSkillIndex(object skill)
        {
            for (int i = 0; i < _skills.Count; i++)
                if (ReferenceEquals(_skills[i], skill)) return i;
            return -1;
        }

        // 플레이어 전용 메서드 — 에너미에서는 사용 안 함
        public T GetSkill<T>() where T : AbstractPlayerSkill => null;

        public void InvokeAttackEnd()
        {
            _currentSkill?.StopSkill();
        }

        private void HandleSkillEnd()
        {
            if (_currentSkill != null)
            {
                _currentSkill.OnSkillEnd -= HandleSkillEnd;
                _currentSkill = null;
            }
            OnSkillEnd?.Invoke();
        }
    }
}
