using System;
using Agents.Modules;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using UnityEngine;

namespace Enemy
{
    public class EnemyHitHandler : AbstractHitHandler
    {
        // 사망 애니메이션은 BT에서 처리하므로 base.HandleDeath()는 호출하지 않지만,
        // 사망 SFX는 PlayDeathSfx()로 재생한다.
        protected override void HandleDeath()
        {
            PlayDeathSfx();
        }
    }
}
