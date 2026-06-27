using Agents.Modules;
using System.UpgradeSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Upgrade
{
    // 적용된 업그레이드 1개를 아이콘으로 표시. 마우스 호버 시 UpgradeTooltip을 표시/숨김 처리한다.
    public class UpgradeIconSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image gradeBorder;

        public UpgradeDataSO Data { get; private set; }
        private UpgradeTooltip _tooltip;
        private ModuleOwner _owner;

        public void Setup(UpgradeDataSO data, UpgradeTooltip tooltip, ModuleOwner owner)
        {
            Data = data;
            _tooltip = tooltip;
            _owner = owner;
            
            iconImage.sprite = data.Icon;

            if (gradeBorder != null)
                gradeBorder.color = UpgradeGradeUtility.GetColor(data.Grade);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Data == null || _tooltip == null) return;
            _tooltip.Show(Data, transform.position, _owner);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltip == null) return;
            _tooltip.Hide();
        }
    }
}
