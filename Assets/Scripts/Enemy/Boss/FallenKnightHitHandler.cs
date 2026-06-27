using System.Collections;
using Agents.Modules;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using UnityEngine;

namespace Enemy.Boss
{
    // 모든 피격에 대해 Hit VFX + Hit Flash를 추가로 재생한다 (경직 발생 여부와 무관).
    // 경직/포이즈/넉백 로직은 EnemyHitHandler(AbstractHitHandler)의 기존 구현을 그대로 사용한다.
    // -> "일반 공격은 경직 없음, 강공격만 짧은 경직"은 AbstractHitHandler의 포이즈 튜닝으로 충족된다.
    public class FallenKnightHitHandler : EnemyHitHandler
    {
        [Header("피격 VFX")]
        [SerializeField] private PoolItemSO hitVfx;
        [SerializeField] private EventChannelSO createChannel;

        [Header("히트 플래시")]
        [SerializeField] private Renderer[] flashRenderers;
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashDuration = 0.1f;
        [SerializeField] private string flashColorProperty = "_EmissionColor";

        private MaterialPropertyBlock _propertyBlock;
        private Coroutine _flashCoroutine;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _propertyBlock = new MaterialPropertyBlock();
        }

        public override void HandleHit()
        {
            PlayHitVfx();
            PlayHitFlash();
            base.HandleHit();
        }

        private void PlayHitVfx()
        {
            if (hitVfx == null) return;

            Vector3 hitPoint = _owner.ActionData != null ? _owner.ActionData.HitPoint : transform.position;
            createChannel?.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(hitVfx, hitPoint, Quaternion.identity));
        }

        private void PlayHitFlash()
        {
            if (flashRenderers == null || flashRenderers.Length == 0) return;

            if (_flashCoroutine != null)
                StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashCoroutine());
        }

        private IEnumerator FlashCoroutine()
        {
            SetFlashColor(flashColor);
            yield return new WaitForSeconds(flashDuration);
            SetFlashColor(Color.black);
            _flashCoroutine = null;
        }

        private void SetFlashColor(Color color)
        {
            foreach (var r in flashRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(flashColorProperty, color);
                r.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
