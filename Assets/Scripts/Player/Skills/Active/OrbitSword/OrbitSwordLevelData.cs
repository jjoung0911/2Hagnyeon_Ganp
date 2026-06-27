namespace Player.Skills.Active
{
    // Orbit Sword 레벨별 수치.
    [System.Serializable]
    public struct OrbitSwordLevelData
    {
        public int swordCount;     // 동시에 존재할 수 있는 최대 검 개수
        public float floatRadius;  // 플레이어 주위 부유 반경
        public float floatHeight;  // 부유 높이 (플레이어 기준 y 오프셋)
        public float floatSpeed;   // 부유 시 좌우로 흔들리는 속도 (deg/sec)
        public float detectRadius; // 적 감지 반경 (플레이어 중심)
        public float launchSpeed;  // 적을 호밍할 때 이동 속도
        public float damage;
        public float reHitInterval; // 같은 적 재타격 쿨다운(초). <=0이면 코드 기본값 사용
        public float maxDistance;   // 플레이어로부터 벗어날 수 있는 최대 거리(리시). <=0이면 detectRadius 사용
        public float activeDuration; // 검 하나가 생성된 뒤 활동(부유+공격) 가능한 시간(초). <=0이면 무제한
        public float cooldownDuration; // 활동 시간 종료 후 사라져서 재등장까지 대기하는 시간(초)
    }
}
