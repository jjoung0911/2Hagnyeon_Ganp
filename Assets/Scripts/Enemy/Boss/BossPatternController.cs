using Agents.Modules;
using UnityEngine;

namespace Enemy.Boss
{
    // "같은 패턴 연속 사용 금지" 규칙만 책임진다.
    // 현재 페이즈 조회는 BossPhaseController에 위임(중복 상태 보관 금지)
    public class BossPatternController : MonoBehaviour, IModule, IAfterInit
    {
        // 동일 패턴 연속 재사용 방지 외에, 패턴 간 최소 간격을 두어 행동이 자연스럽게 이어지도록 한다
        [SerializeField] private float minTimeBetweenPatterns = 0.8f;

        private ModuleOwner _owner;
        private BossPhaseController _phaseController;
        private BossPatternType? _lastPattern;
        private float _lastPatternTime = float.MinValue;

        public BossPhase CurrentPhase => _phaseController != null ? _phaseController.CurrentPhase : BossPhase.Phase1;

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
        }

        public void AfterInit()
        {
            _phaseController = _owner.GetModule<BossPhaseController>();
        }

        // 동일 패턴 연속 사용 금지 + 최소 패턴 간격 미충족 시 false
        public bool CanUsePattern(BossPatternType type)
        {
            return _lastPattern != type && Time.time >= _lastPatternTime + minTimeBetweenPatterns;
        }

        public void RecordPatternUsed(BossPatternType type)
        {
            _lastPattern = type;
            _lastPatternTime = Time.time;
        }
    }
}
