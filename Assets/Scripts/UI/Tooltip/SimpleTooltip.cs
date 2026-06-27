using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Tooltip
{
    // 임의의 설명 문자열을 받아 페이드로 표시/숨김하는 범용 툴팁 (UGUI).
    // 데이터 타입에 의존하지 않으므로 어떤 버튼/아이콘에도 재사용할 수 있다.
    // TooltipTrigger가 호버 시 Show(), 호버 해제 시 Hide()를 호출한다.
    // (UpgradeTooltip과 동일한 패턴이되 UpgradeDataSO 비의존)
    public class SimpleTooltip : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TextMeshProUGUI descText;
        [Tooltip("호버한 대상의 월드 위치로부터의 표시 오프셋")]
        [SerializeField] private Vector2 offset = new(0f, 0f);
        [SerializeField] private float fadeDuration = 0.12f;

        private Tween _fadeTween;

        private void Awake()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false; // 툴팁이 버튼 클릭/호버를 가로채지 않도록
        }

        public void Show(string description, Vector3 worldPosition)
        {
            descText.text = description;

            // 텍스트 변경 직후 레이아웃을 즉시 재계산해 배경 크기가 텍스트 양에 맞춰지도록 한다
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

            // 다른 UI 위에 그려지도록 최상단으로
            transform.SetAsLastSibling();
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
