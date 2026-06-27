using UnityEngine;

namespace Player.Skills.Active
{
    // 발동형(Triggered) 스킬 — 레벨별 쿨다운(초)을 추가로 관리.
    public abstract class TriggeredSkillDataSO<TLevel> : ActiveSkillDataSO<TLevel> where TLevel : struct
    {
        [SerializeField] private float[] cooldowns = new float[MaxLevel];

        // level: 1~5
        public float GetCooldown(int level) => cooldowns[Mathf.Clamp(level, 1, MaxLevel) - 1];

        protected override void OnValidate()
        {
            base.OnValidate();
            if (cooldowns == null || cooldowns.Length != MaxLevel)
                global::System.Array.Resize(ref cooldowns, MaxLevel);
        }
    }
}
