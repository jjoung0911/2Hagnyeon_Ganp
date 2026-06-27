using Agents;

namespace Enemy
{
    // 히트 스태거 테스트용 더미 에너미.
    // BT / NavMesh 없이 Agent 파이프라인만 사용.
    // 필수 자식 모듈: AgentStat, HealthModule, ActionDataModule, HitHandler
    public class DummyEnemy : Agent { }
}
