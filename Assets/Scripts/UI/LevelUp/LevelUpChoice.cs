using System.UpgradeSystem;
using csiimnida.CSILib.SoundManager.RunTime;
using DG.Tweening;
using JWLib.EventChannelSystem;
using Player.Upgrade;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.LevelUp
{
    public class LevelUpChoice : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI descText;

        [Header("Hover")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float hoverDuration = 0.15f;

        [Header("Select")]
        [SerializeField] private float selectedScale = 1.25f;

        [Header("사운드")]
        [SerializeField] private SoundSo hoverSfx;
        [SerializeField] private SoundSo clickSfx;

        private UpgradeDataSO _targetChoice;
        private Vector2 _originAnchoredPos;

        private bool _isSelected;

        private void Awake()
        {
            _originAnchoredPos = rectTransform.anchoredPosition;
            ResetVisual();
        }

        private void ResetVisual()
        {
            _isSelected = false;

            canvasGroup.DOKill();
            rectTransform.DOKill();

            canvasGroup.alpha = 0f;
            rectTransform.localScale = Vector3.zero;
            rectTransform.anchoredPosition = _originAnchoredPos;
        }

        public void Show(UpgradeDataSO targetChoice, float delay)
        {
            _targetChoice = targetChoice;

            nameText.text = targetChoice.UpgradeName;
            icon.sprite = targetChoice.Icon;
            descText.text = targetChoice.Description;

            ResetVisual();
            PlayShowAnimation(delay);
        }

        private void PlayShowAnimation(float delay)
        {
            Sequence seq = DOTween.Sequence()
                .SetUpdate(true);

            seq.AppendInterval(delay);

            seq.Append(
                canvasGroup.DOFade(1f, 0.25f)
            );

            seq.Join(
                rectTransform
                    .DOScale(1f, 0.4f)
                    .SetEase(Ease.OutBack)
            );
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isSelected)
                return;

            rectTransform.DOKill();

            rectTransform
                .DOScale(hoverScale, hoverDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);

            if (hoverSfx != null) SoundManager.Instance.PlaySound(hoverSfx);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isSelected)
                return;

            rectTransform.DOKill();

            rectTransform
                .DOScale(1f, hoverDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isSelected)
                return;

            _isSelected = true;

            if (clickSfx != null) SoundManager.Instance.PlaySound(clickSfx);

            rectTransform.DOKill();

            Sequence seq = DOTween.Sequence()
                .SetUpdate(true);

            seq.Append(
                rectTransform
                    .DOScale(selectedScale, 0.3f)
                    .SetEase(Ease.OutBack)
            );

            seq.Join(
                canvasGroup
                    .DOFade(1f, 0.3f)
            );

            seq.OnComplete(() =>
            {
                Debug.Log("COMPLETE");
                playerChannel.RaiseEvent(
                    UpgradeEvents.UpgradeSelectedEvent.Init(_targetChoice));
            });
        }
    }
}