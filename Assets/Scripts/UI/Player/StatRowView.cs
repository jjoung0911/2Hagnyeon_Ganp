using System.StatSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Player
{
    // 스탯 패널의 한 줄 — 아이콘 + 이름 + 값. PlayerStatPanelView가 풀링해 재사용한다.
    public class StatRowView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image iconImage;

        public void Bind(StatSO stat)
        {
            if (stat == null) return;

            if (nameText != null)
                nameText.text = string.IsNullOrEmpty(stat.DisplayName) ? stat.StatName : stat.DisplayName;

            if (valueText != null)
                valueText.text = Format(stat);

            if (iconImage != null)
            {
                iconImage.sprite = stat.StatIcon;
                iconImage.enabled = stat.StatIcon != null;
            }
        }

        // IsPercent면 백분율(%), 아니면 소수 둘째 자리까지
        private static string Format(StatSO stat)
            => stat.IsPercent ? $"{stat.Value * 100f:0.#}%" : $"{stat.Value:0.##}";
    }
}
