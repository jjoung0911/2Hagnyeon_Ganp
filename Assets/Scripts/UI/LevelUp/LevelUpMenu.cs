using System.Collections.Generic;
using System.UpgradeSystem;
using csiimnida.CSILib.SoundManager.RunTime;
using DG.Tweening;
using JWLib.EventChannelSystem;
using Player.Upgrade;
using UnityEngine;

namespace UI.LevelUp
{
    public class LevelUpMenu : MonoBehaviour
    {
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private List<LevelUpChoice> levelChoices;
        [SerializeField] private LevelUpChoice levelUpTemplate;
        [SerializeField] private Transform spawnTrm;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Slide In")]
        [SerializeField] private float slideDistance = 200f;
        [SerializeField] private float slideDuration = 0.4f;

        [Header("사운드")]
        [SerializeField] private SoundSo levelUpSfx;

        private RectTransform _rectTransform;
        private Vector2 _originAnchoredPos;

        private void Awake()
        {
            playerChannel.AddListener<UpgradeChoicesReadyEvent>(HandleShowChoices);
            playerChannel.AddListener<UpgradeAppliedEvent>(HandleHideChoices);

            _rectTransform = canvasGroup.GetComponent<RectTransform>();
            _originAnchoredPos = _rectTransform.anchoredPosition;

            canvasGroup.alpha = 0;
            canvasGroup.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            playerChannel.RemoveListener<UpgradeChoicesReadyEvent>(HandleShowChoices);
            playerChannel.RemoveListener<UpgradeAppliedEvent>(HandleHideChoices);
        }

        private void HandleHideChoices(UpgradeAppliedEvent evt)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            canvasGroup.DOKill();
            _rectTransform.DOKill();

            canvasGroup
                .DOFade(0f, 0.4f)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    canvasGroup.gameObject.SetActive(false);
                });
        }

        private void HandleShowChoices(UpgradeChoicesReadyEvent evt)
        {
            if (evt.Choices == null || evt.Choices.Length == 0)
                return;

            if (levelUpSfx != null) SoundManager.Instance.PlaySound(levelUpSfx);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            MatchCount(evt.Choices.Length);

            canvasGroup.gameObject.SetActive(true);

            canvasGroup.DOKill();
            canvasGroup.alpha = 0;

            canvasGroup
                .DOFade(1f, 0.4f)
                .SetUpdate(true);

            _rectTransform.DOKill();
            _rectTransform.anchoredPosition = _originAnchoredPos + new Vector2(0f, slideDistance);

            _rectTransform
                .DOAnchorPos(_originAnchoredPos, slideDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);

            for (int i = 0; i < evt.Choices.Length; i++)
            {
                levelChoices[i].Show(evt.Choices[i], i * 0.15f);
            }
        }

        private void MatchCount(int count)
        {
            while (levelChoices.Count < count)
            {
                LevelUpChoice choice =
                    Instantiate(levelUpTemplate, spawnTrm);

                levelChoices.Add(choice);
            }

            for (int i = 0; i < levelChoices.Count; i++)
            {
                levelChoices[i].gameObject.SetActive(i < count);
            }
        }
    }
}