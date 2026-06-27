using Agents.CombatSystem;
using csiimnida.CSILib.SoundManager.RunTime;
using JWLib.AnimationSystem;
using JWLib.ObjectPool.Runtime;
using UnityEngine;

namespace Player.Skills
{
    // 콤보 한 타(1타/2타/3타 등)의 연출·수치 데이터를 정의하는 ScriptableObject.
    // 타수별로 별도 에셋을 만들어 데미지/히트스톱/카메라 연출을 독립적으로 설정한다.
    [CreateAssetMenu(fileName = "New Combo Data", menuName = "SO/Skill/ComboDataSO")]
    public class ComboDataSO : ScriptableObject
    {
        [Header("애니메이션")]
        public AnimParamSO Param;

        [Header("피해")]
        public float Damage = 10f;
        public HitType HitType = HitType.Light;

        [Header("이펙트")]
        public PoolItemSO ComboVfx;

        [Header("히트스톱")]
        [Range(0f, 1f)] public float HitStopTimeScale = 0.05f;
        public float HitStopDuration = 0.1f;

        [Header("카메라 연출")]
        public float ShakeForce = 1f;
        public Vector3 ShakeDirection = Vector3.up;

        [Header("사운드")]
        [Tooltip("공격 판정 시 재생할 SFX (비워두면 재생하지 않음)")]
        public SoundSo SwingSfx;
        [Tooltip("적 타격 시 재생할 SFX (비워두면 재생하지 않음)")]
        public SoundSo HitSfx;
    }
}
