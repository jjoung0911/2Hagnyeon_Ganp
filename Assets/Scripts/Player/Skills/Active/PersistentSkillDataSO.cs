using UnityEngine;

namespace Player.Skills.Active
{
    // 지속형(Persistent) 스킬 — 레벨별 틱 간격(초)을 추가로 관리. 0이면 OnTick 미실행.
    public abstract class PersistentSkillDataSO<TLevel> : ActiveSkillDataSO<TLevel> where TLevel : struct
    {
        [SerializeField] private float[] tickIntervals = new float[MaxLevel];

        // level: 1~5
        public float GetTickInterval(int level) => tickIntervals[Mathf.Clamp(level, 1, MaxLevel) - 1];

        protected override void OnValidate()
        {
            base.OnValidate();
            if (tickIntervals == null || tickIntervals.Length != MaxLevel)
                global::System.Array.Resize(ref tickIntervals, MaxLevel);
        }
    }
}
