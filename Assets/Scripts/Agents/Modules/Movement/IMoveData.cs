using System;
using UnityEngine;

namespace Agents.Modules.Movement
{
    public interface IMoveData
    {
        Vector3 MoveVelocity { get; }
        Vector3 NormalizedMoveDir { get; }
        Vector3 TargetMoveDir { get; }
        float VerticalVelocity { get; }

        bool CanManualMove { get; set; }
        bool IsRootMotionActive { get; set; }
        bool IsGrounded { get; }
        // 일시적 속도 배율 — 1f가 기본값, 스킬에서 임시로 낮춰 사용
        float SpeedMultiplier { get; set; }
        event Action<bool> OnGroundChanged;
        event Action<bool> OnRunStateChanged;
        void SetMoveDir(Vector3 dir);
        void AddForce(Vector3 force);
        void SetForcedVelocity(Vector3 force);
        void StopImmediately(bool stopMove, bool stopForce, bool stopVertical);
        void SetGravity(float gravity);
        void SetVerticalVelocity(float verticalVelocity);
        // CharacterController를 비활성화하고 위치를 설정한 후 재활성화 — 직접 transform.position 설정 시 발생하는 위치 복원 버그 방지
        void Warp(Vector3 position);
    }
}