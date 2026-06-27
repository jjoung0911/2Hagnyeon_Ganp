using UnityEngine;

namespace Player.LevelSystem
{
    // AnimationCurve 기반 경험치 테이블 — 디자이너가 곡선을 직접 그려 비선형 성장 곡선 적용
    [CreateAssetMenu(fileName = "New Curve Exp Table", menuName = "SO/Level/CurveExpTableSO", order = 0)]
    public class CurveExpTableSO : ExpTableSO
    {
        [SerializeField] private AnimationCurve expCurve = AnimationCurve.Linear(1, 100, 50, 5000);
        [SerializeField] private float multiplier = 1f;

        public override int GetRequiredExp(int level)
            => Mathf.Max(1, Mathf.RoundToInt(expCurve.Evaluate(level) * multiplier));
    }
}
