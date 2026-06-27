using Agents.Modules;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UI.Enemy
{
    // 적 절단 가능 상태 표시. 임계값 이하 HP가 되면 Punch + Pulse 시작.
    // Inspector:
    //   canvasGroup    — CanvasGroup (FadeIn/Out)
    //   rectTransform  — Scale Punch 대상
    //   sliceLabel     — TextMeshProUGUI ("SLICE")
    //   sliceThreshold — 절단 가능 HP 비율 (0~1)
    public class EnemySliceIndicator : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TextMeshProUGUI sliceLabel;
        [Range(0f, 1f)]
        [SerializeField] private float sliceThreshold = 0.25f;

        private HealthModule _healthModule;
        private Tween _pulseTween;
        private bool _isShowing;

        public void Setup(HealthModule healthModule)
        {
            _healthModule = healthModule;
            _healthModule.OnHealthChangeEvent += HandleHpChanged;
            if (sliceLabel != null) sliceLabel.text = "SLICE";
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        private void OnDestroy()
        {
            if (_healthModule != null)
                _healthModule.OnHealthChangeEvent -= HandleHpChanged;
            _pulseTween?.Kill();
        }

        private void HandleHpChanged(float current, float previous, float max)
        {
            bool canSlice = max > 0f && current > 0f && (current / max) <= sliceThreshold;

            if (canSlice && !_isShowing)
                ShowIndicator();
            else if (!canSlice && _isShowing)
                HideIndicator();
        }

        private void ShowIndicator()
        {
            _isShowing = true;

            if (rectTransform != null) rectTransform.localScale = Vector3.one * 0.5f;

            // FadeIn + Scale Punch 등장
            DOTween.Sequence()
                .Append(canvasGroup != null
                    ? canvasGroup.DOFade(1f, 0.15f).SetEase(Ease.OutQuart)
                    : null)
                .Join(rectTransform != null
                    ? rectTransform.DOScale(1f, 0.2f).SetEase(Ease.OutBack)
                    : null)
                .OnComplete(() =>
                {
                    rectTransform?.DOPunchScale(Vector3.one * 0.2f, 0.25f, 5, 0.4f)
                        .OnComplete(StartPulse);
                });
        }

        private void HideIndicator()
        {
            _isShowing = false;
            _pulseTween?.Kill();
            canvasGroup?.DOFade(0f, 0.25f).SetEase(Ease.InQuart)
                .OnComplete(() => rectTransform?.DOScale(Vector3.one, 0f));
        }

        private void StartPulse()
        {
            _pulseTween?.Kill();
            _pulseTween = rectTransform?.DOScale(Vector3.one * 1.08f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }
}
