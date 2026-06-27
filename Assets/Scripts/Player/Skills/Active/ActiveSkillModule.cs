using Agents.Modules;
using UnityEngine;

namespace Player.Skills.Active
{
    // 5개 액티브 스킬 컴포넌트를 캐싱하고, 잠금 해제(IsUnlocked)된 스킬만 매 프레임 Tick.
    public class ActiveSkillModule : MonoBehaviour, IModule, IActiveSkillModule
    {
        private AbstractActiveSkill[] _skills;

        public void Initialize(ModuleOwner owner)
        {
            var skills = GetComponentsInChildren<AbstractActiveSkill>(true);
            _skills = new AbstractActiveSkill[global::System.Enum.GetValues(typeof(ActiveSkillEnum)).Length];

            foreach (var skill in skills)
            {
                _skills[(int)skill.SkillIndex] = skill;
                skill.Initialize(owner);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _skills.Length; i++)
            {
                var skill = _skills[i];
                if (skill != null && skill.IsUnlocked)
                    skill.Tick(dt);
            }
        }

        public AbstractActiveSkill GetSkill(ActiveSkillEnum index) => _skills[(int)index];
        public AbstractActiveSkill GetSkill(int index) => _skills[index];
    }
}
