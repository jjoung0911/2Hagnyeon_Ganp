using UnityEngine;

namespace Enemy.Skills
{
    // 사용 거리·상태 등 별도 조건을 가진 스킬에 구현
    // CanUseSkillCondition이 이 인터페이스를 통해 조건을 자동 탐색
    public interface ISkillCondition
    {
        bool CheckCondition(GameObject target);
    }
}
