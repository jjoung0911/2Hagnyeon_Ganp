using UnityEngine;

namespace Agents.CombatSystem
{
    [CreateAssetMenu(fileName = "new SkillDataTable", menuName = "SO/Skill/Table", order = 0)]
    public class SkillDataTable : ScriptableObject
    {
        [field: SerializeField] public SkillDataSO[] skills;
    }
}