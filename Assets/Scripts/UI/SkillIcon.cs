using System;
using System.Collections;
using Agents.CombatSystem;
using JWLib.EventChannelSystem;
using Player.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SkillIcon : MonoBehaviour
    {
        [SerializeField] private SkillDataSO skillData;
        [SerializeField] private EventChannelSO playerChannel;
        private Image _iconImage;
        private void Awake()
        {
            _iconImage = GetComponent<Image>();
            playerChannel.AddListener<SkillUseEvent>(HandleCooldown);
        }

        private void OnDestroy()
        {
            playerChannel.RemoveListener<SkillUseEvent>(HandleCooldown);
        }

        private void HandleCooldown(SkillUseEvent evt)
        {
            if (evt.UseSkill != skillData) return;
            StartCoroutine(CooldownCoroutine(evt.CoolTime));
        }

        private IEnumerator CooldownCoroutine(float cooldown)
        {
            _iconImage.fillAmount = 0;
            float elapsed = 0f;
            while (elapsed < cooldown)
            {
                elapsed += Time.deltaTime;
                Debug.Log(elapsed);
                _iconImage.fillAmount = Mathf.Clamp01(elapsed / cooldown);
                yield return null;
            }
        }
    }
}