using DG.Tweening;
using JWLib.EventChannelSystem;
using TMPro;
using UI.Events;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Player
{
    // 플레이어 체력 Canvas UI(HP Bar). playerChannel의 PlayerHpChangedEvent만 구독해 갱신한다.
    // 앞 바(fillImage)는 즉시, 뒤 바(delayImage)는 0.2초 후 천천히 따라오도록 갱신한다.
    // 구독 시점에 이미 발행된 값이 있으면 EventChannelSO가 즉시 전달해주므로
    // 최초 1회는 애니메이션 없이 ApplyImmediate로 적용한다.
    public class PlayerHpBarCanvasView : MonoBehaviour
    {
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image delayImage;
        [SerializeField] private TextMeshProUGUI hpText;

        private Tween _delayTween;
        private bool _initialized;

        private void Start()
        {
            playerChannel.AddListener<PlayerHpChangedEvent>(HandleHpChanged);
        }

        private void OnDestroy()
        {
            playerChannel.RemoveListener<PlayerHpChangedEvent>(HandleHpChanged);
            _delayTween?.Kill();
        }

        private void HandleHpChanged(PlayerHpChangedEvent evt)
        {
            if (!_initialized)
            {
                _initialized = true;
                ApplyImmediate(evt.Current, evt.Max);
                return;
            }

            float ratio = evt.Max > 0f ? evt.Current / evt.Max : 0f;

            fillImage.DOKill();
            fillImage.DOFillAmount(ratio, 0.06f).SetEase(Ease.OutQuart);

            _delayTween?.Kill();
            _delayTween = DOVirtual.DelayedCall(0.2f, () =>
                delayImage.DOFillAmount(ratio, 0.45f).SetEase(Ease.OutCubic));

            UpdateText(evt.Current, evt.Max);
        }

        private void ApplyImmediate(float current, float max)
        {
            float ratio = max > 0f ? current / max : 0f;
            fillImage.fillAmount = ratio;
            delayImage.fillAmount = ratio;
            UpdateText(current, max);
        }

        private void UpdateText(float current, float max)
        {
            if (hpText != null)
                hpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }
}
