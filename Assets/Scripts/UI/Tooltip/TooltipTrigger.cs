using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Tooltip
{
    // 버튼 등 UI 요소에 부착해, 마우스 호버 시 지정한 설명을 SimpleTooltip으로 띄운다.
    // 설명 문구는 인스펙터(TextArea)에서 편집한다.
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private SimpleTooltip tooltip;
        [TextArea(2, 5)]
        [SerializeField] private string description;
        [Tooltip("툴팁 표시 기준 위치. 비우면 이 오브젝트의 위치를 사용한다.")]
        [SerializeField] private RectTransform anchor;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltip == null || string.IsNullOrEmpty(description)) return;
            Transform pivot = anchor != null ? anchor : transform;
            tooltip.Show(description, pivot.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltip?.Hide();
        }

        // 패널이 닫히는 등으로 호버 해제 이벤트가 누락돼도 툴팁이 남지 않도록 방어
        private void OnDisable()
        {
            tooltip?.Hide();
        }
    }
}
