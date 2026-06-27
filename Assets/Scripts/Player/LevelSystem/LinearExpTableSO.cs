using UnityEngine;

namespace Player.LevelSystem
{
    // 선형 증가 경험치 테이블: baseExp + (level - 1) * increasePerLevel
    [CreateAssetMenu(fileName = "New Linear Exp Table", menuName = "SO/Level/LinearExpTableSO", order = 0)]
    public class LinearExpTableSO : ExpTableSO
    {
        [SerializeField] private int baseExp = 100;
        [SerializeField] private int increasePerLevel = 50;

        public override int GetRequiredExp(int level)
            => baseExp + Mathf.Max(0, level - 1) * increasePerLevel;
    }
}
