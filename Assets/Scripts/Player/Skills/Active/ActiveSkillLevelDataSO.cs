using UnityEngine;

namespace Player.Skills.Active
{
    // 레벨별 수치를 typed struct 배열(길이 5)로 관리하는 제네릭 베이스.
    // 새 스킬은 전용 LevelData struct + 이 클래스를 상속한 DataSO만 추가하면 된다.
    public abstract class ActiveSkillDataSO<TLevel> : ActiveSkillDataSO where TLevel : struct
    {
        [SerializeField] private TLevel[] levels = new TLevel[MaxLevel];

        // level: 1~5
        public TLevel GetLevel(int level) => levels[Mathf.Clamp(level, 1, MaxLevel) - 1];

        protected virtual void OnValidate()
        {
            if (levels == null || levels.Length != MaxLevel)
                global::System.Array.Resize(ref levels, MaxLevel);
        }
    }
}
