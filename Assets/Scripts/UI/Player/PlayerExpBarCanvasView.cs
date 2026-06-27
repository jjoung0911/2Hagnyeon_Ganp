using DG.Tweening;
using JWLib.EventChannelSystem;
using Player.LevelSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Player
{
    // 플레이어 경험치 Canvas UI(EXP Bar). PlayerLevelModule이 발행하는 ExpChangedEvent만 구독해 갱신한다.
    // 앞 바는 즉시, 뒤 바는 0.2초 후 천천히 채운다. 텍스트는 "Lv.{level}  exp/required" 형식.
    // 구독 시점에 이미 발행된 값이 있으면 EventChannelSO가 즉시 전달해주므로
    // 최초 1회는 애니메이션 없이 ApplyImmediate로 적용한다.
    public class PlayerExpBarCanvasView : MonoBehaviour
    {
        [SerializeField] private EventChannelSO eventChannel;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image delayImage;
        [SerializeField] private TextMeshProUGUI expText;

        private Tween _delayTween;
        private bool _initialized;

        private void Start()
        {
            eventChannel.AddListener<ExpChangedEvent>(HandleExpChanged);
        }

        private void OnDestroy()
        {
            eventChannel.RemoveListener<ExpChangedEvent>(HandleExpChanged);
            _delayTween?.Kill();
        }

        private void HandleExpChanged(ExpChangedEvent evt)
        {
            if (!_initialized)
            {
                _initialized = true;
                ApplyImmediate(evt.CurrentExp, evt.RequiredExp, evt.Level);
                return;
            }

            float ratio = evt.RequiredExp > 0 ? (float)evt.CurrentExp / evt.RequiredExp : 0f;

            fillImage.DOKill();
            fillImage.DOFillAmount(ratio, 0.1f).SetEase(Ease.OutQuart);

            _delayTween?.Kill();
            _delayTween = DOVirtual.DelayedCall(0.2f, () =>
                delayImage.DOFillAmount(ratio, 0.45f).SetEase(Ease.OutCubic));

            UpdateText(evt.CurrentExp, evt.RequiredExp, evt.Level);
        }

        private void ApplyImmediate(int currentExp, int requiredExp, int level)
        {
            float ratio = requiredExp > 0 ? (float)currentExp / requiredExp : 0f;
            fillImage.fillAmount = ratio;
            delayImage.fillAmount = ratio;
            UpdateText(currentExp, requiredExp, level);
        }

        private void UpdateText(int currentExp, int requiredExp, int level)
        {
            if (expText != null)
                expText.text = $"Lv.{level}  {currentExp} / {requiredExp}";
        }
    }
}
