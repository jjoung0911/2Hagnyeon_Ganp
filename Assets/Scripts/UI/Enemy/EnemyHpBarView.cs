using System.Collections;
using Agents.Modules;
using DG.Tweening;
using UI.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Enemy
{
    // 적 머리 위 World Space HP 바.
    // 피격 시 FadeIn, 일정 시간 후 FadeOut. Front bar 즉시 감소, Back bar 0.2초 후 천천히 감소.
    // Inspector:
    //   fillImage   — 앞 바 Image
    //   delayImage  — 뒤 바 Image
    //   canvasGroup — 전체 패널 CanvasGroup (FadeIn/Out)
    //   hideDelay   — 피격 후 숨김까지 대기 시간
    //   billboardCamera
    public class EnemyHpBarView : MonoBehaviour, IStatBar
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Image delayImage;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float hideDelay = 2f;
        [SerializeField] private Camera billboardCamera;

        private HealthModule _healthModule;
        private Tween _delayBarTween;
        private Tween _hideTween;
        private bool _isVisible;

        public void Setup(HealthModule healthModule)
        {
            _healthModule = healthModule;
            _healthModule.OnHealthChangeEvent += HandleHpChanged;
            if (billboardCamera == null) billboardCamera = Camera.main;
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            // 초기 Fill
            float init = healthModule.MaxHp > 0f ? healthModule.CurrentHp / healthModule.MaxHp : 1f;
            if (fillImage != null) fillImage.fillAmount = init;
            if (delayImage != null) delayImage.fillAmount = init;
        }

        private void OnDestroy()
        {
            if (_healthModule != null)
                _healthModule.OnHealthChangeEvent -= HandleHpChanged;
            _delayBarTween?.Kill();
            _hideTween?.Kill();
        }

        private void LateUpdate()
        {
            if (billboardCamera != null)
                transform.forward = billboardCamera.transform.forward;
        }

        public void UpdateBar(float current, float max)
        {
            float ratio = max > 0f ? current / max : 0f;

            // 앞 바: 즉시 감소
            fillImage?.DOFillAmount(ratio, 0.06f).SetEase(Ease.OutQuart);

            // 뒤 바: 0.2초 후 천천히 따라옴
            if (delayImage != null)
            {
                _delayBarTween?.Kill();
                _delayBarTween = DOVirtual.DelayedCall(0.2f, () =>
                    delayImage.DOFillAmount(ratio, 0.45f).SetEase(Ease.OutCubic)
                );
            }
        }

        private void HandleHpChanged(float current, float previous, float max)
        {
            UpdateBar(current, max);
            ShowTemporarily();
        }

        private void ShowTemporarily()
        {
            _hideTween?.Kill();

            if (!_isVisible)
            {
                _isVisible = true;
                canvasGroup?.DOFade(1f, 0.15f).SetEase(Ease.OutQuart);
            }

            _hideTween = DOVirtual.DelayedCall(hideDelay, () =>
            {
                _isVisible = false;
                canvasGroup?.DOFade(0f, 0.4f).SetEase(Ease.InQuart);
            });
        }
    }
}
