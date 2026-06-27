using Agents.Modules;
using System.DifficultySystem;
using UnityEngine;

namespace System.UpgradeSystem
{
    // 업그레이드 하나를 정의하는 SO. 새 업그레이드 효과는 이 클래스를 상속한
    // 새 서브클래스로 추가한다 (OCP) — 기존 서브클래스/모듈 수정 불필요.
    public abstract class UpgradeDataSO : IndexedAsset
    {
        [field: SerializeField] public string UpgradeName { get; private set; }
        [field: TextArea]
        [field: SerializeField] public string Description { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField] public UpgradeGrade Grade { get; private set; }

        public abstract UpgradeType Type { get; }

        // 현재 상태에서 이 업그레이드를 선택지로 제시/적용할 수 있는지 여부
        public abstract bool CanApply(ModuleOwner owner, IUpgradeProgress progress);

        // 선택 즉시 효과를 적용
        public abstract void Apply(ModuleOwner owner, IUpgradeProgress progress);

        // 효과 수치(스탯 변화량, 스킬 레벨 효과 등)를 사람이 읽을 수 있는 텍스트로 반환. 수치 효과가 없으면 null
        public virtual string GetEffectDetail(ModuleOwner owner) => null;

        // 같은 대상(스킬/스탯 등)을 업그레이드하는 데이터인지 여부. 아이콘 교체 판단에 사용
        public virtual bool IsSameTarget(UpgradeDataSO other) => this == other;

        // 경과 시간에 비례해 적이 강해지는 만큼, 스탯형 업그레이드의 효과치도 같은 비율로 증폭한다.
        protected static float UpgradeAmplifier => DifficultyManager.Instance.GetUpgradeAmplifier();
    }
}
