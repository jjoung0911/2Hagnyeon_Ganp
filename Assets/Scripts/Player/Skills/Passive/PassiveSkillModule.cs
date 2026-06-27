using Agents.Modules;
using UnityEngine;

namespace Player.Skills.Passive
{
    // 패시브 스킬 컴포넌트를 자식에서 수집해 인덱스로 캐싱한다.
    public class PassiveSkillModule : MonoBehaviour, IModule, IPassiveSkillModule
    {
        private AbstractPassiveSkill[] _skills;

        // 자식 패시브 스킬들은 ModuleOwner.Awake()가 IModule로 직접 수집해 Initialize()를 호출하므로
        // 여기서는 인덱스 캐싱만 수행한다 (중복 초기화 방지 — BloodFeastModule의 이벤트 중복 구독 등).
        public void Initialize(ModuleOwner owner)
        {
            var skills = GetComponentsInChildren<AbstractPassiveSkill>(true);
            _skills = new AbstractPassiveSkill[(int)PassiveSkillEnum.HpIncrease + 1];

            foreach (var skill in skills)
                _skills[(int)skill.SkillIndex] = skill;
        }

        public AbstractPassiveSkill GetSkill(PassiveSkillEnum index) => _skills[(int)index];
        public AbstractPassiveSkill GetSkill(int index) => _skills[index];
    }
}
