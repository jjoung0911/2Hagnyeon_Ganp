using Agents.Modules;
using UnityEngine;

namespace Player.Skills.Passive
{
    // 모든 패시브 스킬의 공통 베이스.
    // PassiveSkillModule이 자식에서 수집해 초기화하며, 업그레이드 카드 선택 시
    // ApplyUpgrade(level)로 레벨이 올라간다 (ActiveSkill 프레임워크와 동일한 구조).
    public abstract class AbstractPassiveSkill : MonoBehaviour, IModule
    {
        public const int MaxLevel = 5;

        [field: SerializeField] public PassiveSkillEnum SkillIndex { get; private set; }

        protected Player Player { get; private set; }

        // 0 = 미보유, 1~5 = 보유 레벨
        public int Level { get; private set; }
        public bool IsUnlocked => Level >= 1;

        public void Initialize(ModuleOwner owner)
        {
            Player = owner as Player;
            OnInitialize();
        }

        // 파생 클래스 초기화 훅 — Player 캐싱 이후 호출
        protected virtual void OnInitialize() { }

        // 업그레이드 카드 선택 시 호출 — 레벨 적용 후 파생 클래스에 알림
        public void ApplyUpgrade(int level)
        {
            level = Mathf.Clamp(level, 0, MaxLevel);
            if (level == Level) return;

            Level = level;
            OnLevelChanged(Level);
        }

        // 레벨 변경 시 수치 갱신 — 파생 클래스에서 구현
        protected abstract void OnLevelChanged(int level);

        // 해당 레벨의 효과 수치를 사람이 읽을 수 있는 텍스트로 반환 (업그레이드 툴팁용). 없으면 null
        public virtual string GetEffectDetail(int level) => null;
    }
}
