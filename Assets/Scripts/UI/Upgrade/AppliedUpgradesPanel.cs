using System.Collections.Generic;
using JWLib.EventChannelSystem;
using Player.Skills;
using Player.Upgrade;
using System.UpgradeSystem;
using UnityEngine;

namespace UI.Upgrade
{
    // 플레이어가 적용한 업그레이드 목록을 아이콘으로 표시한다.
    // UpgradeAppliedEvent를 구독해 업그레이드가 적용될 때마다 아이콘을 하나씩 추가한다.
    public class AppliedUpgradesPanel : MonoBehaviour
    {
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private UpgradeIconSlot iconTemplate;
        [SerializeField] private Transform spawnTrm;
        [SerializeField] private UpgradeTooltip tooltip;

        private readonly List<UpgradeIconSlot> _slots = new();

        private void Awake()
        {
            iconTemplate.gameObject.SetActive(false);
            playerChannel.AddListener<UpgradeAppliedEvent>(HandleUpgradeApplied);
        }

        private void OnDestroy()
        {
            playerChannel.RemoveListener<UpgradeAppliedEvent>(HandleUpgradeApplied);
        }

        private void HandleUpgradeApplied(UpgradeAppliedEvent evt)
        {
            if (evt.Selected == null) return;

            // ShowInHUD = false인 스킬(대쉬·점프·콤보 등)의 업그레이드는 패널에 표시하지 않음
            if (evt.Selected is SkillLevelUpgradeSO skillUpgrade && evt.Owner != null)
            {
                var skill = evt.Owner.GetModule<PlayerSkillModule>()?.GetSkill((int)skillUpgrade.SkillIndex);
                if (skill != null && !skill.ShowInHUD) return;
            }
            
            foreach (var iconSlot in _slots)
            {
                if (iconSlot.Data.IsSameTarget(evt.Selected))
                {
                    iconSlot.Setup(evt.Selected, tooltip, evt.Owner);
                    return;
                }
            }
            
            UpgradeIconSlot slot = Instantiate(iconTemplate, spawnTrm);
            slot.gameObject.SetActive(true);
            
            slot.Setup(evt.Selected, tooltip, evt.Owner);
            _slots.Add(slot);
        }
    }
}
