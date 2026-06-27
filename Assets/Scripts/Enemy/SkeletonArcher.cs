using UnityEngine;

namespace Enemy
{
    // 원거리 지원 잡몹 — 플레이어와 거리를 유지하며 화살을 발사하는 RangedEnemy
    public class SkeletonArcher : RangedEnemy
    {
        [field: SerializeField] public float PreferredMinRange { get; private set; } = 4f; // 이보다 가까우면 후퇴
        [field: SerializeField] public float PreferredMaxRange { get; private set; } = 8f; // 이보다 멀면 접근
        [field: SerializeField] public float RetreatStep { get; private set; } = 3f;       // 후퇴 1회 이동 거리
    }
}
