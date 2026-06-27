using Agents;
using Agents.CombatSystem;
using UnityEngine;

namespace Player.Skills.Finishers
{
    // 평타 콤보의 피니셔(마지막 타) 적중 시 발동하는 추가 공격의 공통 인터페이스.
    // 검기/잔상/충격파/차원참 등 각 효과를 독립 컴포넌트로 구현해 OCP를 만족한다.
    // 새 피니셔 = 새 IFinisherEffect 구현체를 자식 오브젝트에 부착(기존 코드 수정 불필요).
    public interface IFinisherEffect
    {
        // 현재 스킬 레벨이 해금 레벨 이상이면 true
        bool IsUnlocked { get; }

        // 초기화 — 소유자(플레이어) 참조 캐싱
        void Init(Agent owner);

        // 스킬 레벨 변경 시 호출 — 해금 여부 갱신
        void UpdateUnlock(int skillLevel);

        // 피니셔 발동. 검신 스택을 소모해야 하는 효과(차원참)는 true를 반환한다.
        bool Execute(in FinisherContext ctx);
    }

    // 피니셔 발동에 필요한 정보 묶음(값 타입, 무할당).
    // 최종 데미지 = (Atk * 계수 + BonusDamage) * DamageMultiplier — Damage() 헬퍼로 계산.
    public readonly struct FinisherContext
    {
        public readonly Agent Owner;
        public readonly Vector3 Origin;
        public readonly Quaternion Rotation;
        public readonly float Atk;
        public readonly float BonusDamage;
        public readonly float DamageMultiplier;
        public readonly HitType HitType;
        public readonly int CurrentStacks;
        public readonly int MaxStacks;

        public FinisherContext(Agent owner, Vector3 origin, Quaternion rotation,
            float atk, float bonusDamage, float damageMultiplier, HitType hitType,
            int currentStacks, int maxStacks)
        {
            Owner = owner;
            Origin = origin;
            Rotation = rotation;
            Atk = atk;
            BonusDamage = bonusDamage;
            DamageMultiplier = damageMultiplier;
            HitType = hitType;
            CurrentStacks = currentStacks;
            MaxStacks = maxStacks;
        }

        // 계수를 적용한 최종 데미지 계산 헬퍼
        public float Damage(float coefficient) => (Atk * coefficient + BonusDamage) * DamageMultiplier;
    }
}
