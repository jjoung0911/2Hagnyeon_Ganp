using System.Collections.Generic;
using JWLib.EventChannelSystem;
using UI.Events;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Player
{
    // 대쉬 충전 상태를 체력바처럼 fillAmount를 조절하는 바(슬롯)들로 표시한다.
    // barTemplate(첫 번째 슬롯)을 기준으로 MaxCharges가 늘어날 때마다 부족한 슬롯을 동적으로 생성한다.
    // playerChannel의 DashChargeChangedEvent만 구독해 갱신한다.
    public class DashChargeView : MonoBehaviour
    {
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private RectTransform barTemplate;

        private readonly List<Image> _chargeFillBars = new();

        private void Start()
        {
            _chargeFillBars.Add(GetFillImage(barTemplate));
            playerChannel.AddListener<DashChargeChangedEvent>(HandleDashChargeChanged);
        }

        private void OnDestroy()
        {
            playerChannel.RemoveListener<DashChargeChangedEvent>(HandleDashChargeChanged);
        }

        private void HandleDashChargeChanged(DashChargeChangedEvent evt)
        {
            while (_chargeFillBars.Count < evt.MaxCharges)
            {
                RectTransform bar = Instantiate(barTemplate, barTemplate.parent);
                bar.SetAsLastSibling();
                _chargeFillBars.Add(GetFillImage(bar));
            }

            for (int i = 0; i < _chargeFillBars.Count; i++)
            {
                if (i < evt.CurrentCharges)
                    _chargeFillBars[i].fillAmount = 1f;
                else if (i == evt.CurrentCharges)
                    _chargeFillBars[i].fillAmount = evt.NormalizedCooldown;
                else
                    _chargeFillBars[i].fillAmount = 0f;
            }
        }

        private static Image GetFillImage(RectTransform bar) => bar.Find("Fill").GetComponent<Image>();
    }
}
