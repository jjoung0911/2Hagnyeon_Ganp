using DG.Tweening;
using JWLib.EventChannelSystem;
using TMPro;
using UI.Events;
using UnityEngine;

namespace UI.Combat
{
    // 콤보 수 Canvas UI(ComboText). CombatUIBridge가 발행하는 ComboChangedEvent를 구독해
    // 콤보 수를 표시한다. 콤보가 늘어날 때마다 펀치 스케일 효과를 재생하고,
    // 콤보가 끊기면(0 또는 1 이하) 서서히 사라진다.
    public class ComboCanvasView : MonoBehaviour
    {
        [SerializeField] private EventChannelSO uiChannel;
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private float punchScale = 0.3f;
        [SerializeField] private float punchDuration = 0.2f;
        [SerializeField] private float fadeOutDuration = 0.6f;

        private Tween _punchTween;
        private Tween _fadeTween;

        private void Awake()
        {
            uiChannel.AddListener<ComboChangedEvent>(HandleComboChanged);
            comboText.text = string.Empty;
            SetAlpha(0f);
        }

        private void OnDestroy()
        {
            uiChannel.RemoveListener<ComboChangedEvent>(HandleComboChanged);
            _punchTween?.Kill();
            _fadeTween?.Kill();
        }

        private void HandleComboChanged(ComboChangedEvent evt)
        {
            if (evt.Count > 1)
                ShowCombo(evt.Count);
            else
                FadeOutCombo();
        }

        private void ShowCombo(int count)
        {
            comboText.text = $"x <b><size=130> {count} </size></b>";

            _fadeTween?.Kill();
            SetAlpha(1f);

            _punchTween?.Kill();
            comboText.transform.localScale = Vector3.one;
            _punchTween = comboText.transform.DOPunchScale(Vector3.one * punchScale, punchDuration, 8, 0.8f);
        }

        private void FadeOutCombo()
        {
            _punchTween?.Kill();
            comboText.transform.localScale = Vector3.one;

            _fadeTween?.Kill();
            _fadeTween = comboText.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuart)
                .OnComplete(() => comboText.text = string.Empty);
        }

        private void SetAlpha(float alpha)
        {
            Color color = comboText.color;
            color.a = alpha;
            comboText.color = color;
        }
    }
}
