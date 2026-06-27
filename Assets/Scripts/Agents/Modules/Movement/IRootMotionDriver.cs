using UnityEngine;

namespace Agents.Modules.Movement
{
    public interface IRootMotionDriver
    {
        // scale: 애니메이션 이동량 배율 (1 = 원본, 2 = 두 배)
        void Begin(Vector3 worldDir, float scale = 1f);
        void End();
    }
}
