using UnityEngine;

namespace Player.Skills.Active
{
    // 모든 액티브 스킬 데이터 SO의 공통 베이스.
    public abstract class ActiveSkillDataSO : System.IndexedAsset
    {
        public const int MaxLevel = 8;

        [field: SerializeField] public string SkillName { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }

        [field: TextArea]
        [field: SerializeField]
        public string Description { get; private set; }
    }
}
