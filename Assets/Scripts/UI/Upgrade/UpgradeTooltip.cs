using Agents.Modules;
using DG.Tweening;
using System.UpgradeSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Upgrade
{
    // 업그레이드 아이콘에 마우스를 올렸을 때 이름/설명/등급 정보를 보여주는 툴팁.
    // UpgradeIconSlot이 호버 시 Show(), 호버 해제 시 Hide()를 호출한다.
    public class UpgradeTooltip : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descText;
        [SerializeField] private TextMeshProUGUI gradeText;
        [SerializeField] private TextMeshProUGUI detailText;
        [SerializeField] private Vector2 offset = new(0f, 24f);
        [SerializeField] private float fadeDuration = 0.12f;

        private Tween _fadeTween;

        private void Awake()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        public void Show(UpgradeDataSO data, Vector3 worldPosition, ModuleOwner owner = null)
        {
            nameText.text = data.UpgradeName;
            descText.text = data.Description;
            gradeText.text = data.Grade.ToString();
            gradeText.color = UpgradeGradeUtility.GetColor(data.Grade);

            if (detailText != null)
            {
                string detail = data.GetEffectDetail(owner);
                detailText.gameObject.SetActive(!string.IsNullOrEmpty(detail));
                detailText.text = detail;
            }

            // 텍스트 변경 직후 레이아웃을 즉시 재계산해 사이즈가 텍스트 양에 맞춰지도록 한다
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

            rectTransform.position = worldPosition + (Vector3)offset;

            _fadeTween?.Kill();
            _fadeTween = canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);
        }

        public void Hide()
        {
            _fadeTween?.Kill();
            _fadeTween = canvasGroup.DOFade(0f, fadeDuration).SetUpdate(true);
        }
    }
}
