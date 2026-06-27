using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Player
{
    // 스킬 슬롯 1개 uGUI 컴포넌트.
    // SkillCooldownHUDView가 생성하고 SetData()로 매 프레임 상태를 주입한다.
    public class SkillSlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        // fillMethod = Radial360, fillOrigin = Top, fillClockwise = false
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private Image lockOverlay;
        [SerializeField] private TextMeshProUGUI cooldownText;

        public void SetData(Sprite icon, bool isUnlocked, float normalizedCooldown, float remainingSeconds)
        {
            iconImage.sprite = icon;

            lockOverlay.gameObject.SetActive(!isUnlocked);

            if (isUnlocked)
            {
                bool onCooldown = normalizedCooldown < 1f;
                cooldownOverlay.gameObject.SetActive(onCooldown);
                if (onCooldown)
                    cooldownOverlay.fillAmount = 1f - normalizedCooldown;

                if (cooldownText != null)
                {
                    bool showText = onCooldown && remainingSeconds > 0.1f;
                    cooldownText.gameObject.SetActive(showText);
                    if (showText)
                        cooldownText.text = remainingSeconds.ToString("F1");
                }
            }
            else
            {
                cooldownOverlay.gameObject.SetActive(false);
                if (cooldownText != null) cooldownText.gameObject.SetActive(false);
            }
        }
    }
}
