using DG.Tweening;
using Enemy.Boss;
using JWLib.EventChannelSystem;
using TMPro;
using UnityEngine;

namespace UI.Boss
{
    // 보스 격파 승리 타이틀 표시 View (UGUI).
    // BossDefeatedEvent → 타이틀 페이드인 → 일정 시간 대기 → 자동 페이드아웃
    //
    // Inspector 계층 구조 예시 (BossIntroView와 동일 Canvas 하위):
    //   Canvas (Screen Space - Overlay)
    //     └─ GameObject (BossDefeatedView)
    //          └─ TitleGroup    (CanvasGroup)
    //               └─ DefeatedText (TextMeshProUGUI)
    public class BossDefeatedView : MonoBehaviour
    {
        [SerializeField] private EventChannelSO bossEventChannel;
        [SerializeField] private string displayText = "BOSS DEFEATED";

        [Header("타이틀")]
        [SerializeField] private CanvasGroup titleGroup;
        [SerializeField] private TextMeshProUGUI defeatedText;

        [Header("타이밍")]
        [SerializeField] private float fadeInDuration = 0.4f;
        [SerializeField] private float holdDuration = 2.5f;
        [SerializeField] private float fadeOutDuration = 0.6f;

        private Sequence _sequence;

        private void OnEnable()
        {
            bossEventChannel?.AddListener<BossDefeatedEvent>(HandleBossDefeated);
            ResetToHidden();
        }

        private void OnDisable()
        {
            bossEventChannel?.RemoveListener<BossDefeatedEvent>(HandleBossDefeated);
            _sequence?.Kill();
        }

        private void ResetToHidden()
        {
            _sequence?.Kill();
            if (titleGroup != null) titleGroup.alpha = 0f;
        }

        private void HandleBossDefeated(BossDefeatedEvent _)
        {
            if (defeatedText != null) defeatedText.text = displayText;

            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Append(titleGroup.DOFade(1f, fadeInDuration))
                .AppendInterval(holdDuration)
                .Append(titleGroup.DOFade(0f, fadeOutDuration));
        }
    }
}
