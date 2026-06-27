using UnityEngine;

namespace Player.LevelSystem
{
    // 레벨별 요구 경험치를 계산하는 테이블의 추상 베이스.
    // 새로운 경험치 곡선은 이 클래스를 상속한 새 SO로 추가한다 (OCP).
    public abstract class ExpTableSO : ScriptableObject
    {
        // level에서 다음 레벨로 올라가기 위해 필요한 경험치
        public abstract int GetRequiredExp(int level);
    }
}
