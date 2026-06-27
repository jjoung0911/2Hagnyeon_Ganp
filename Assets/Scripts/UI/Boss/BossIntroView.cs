using DG.Tweening;
using Enemy.Boss;
using JWLib.EventChannelSystem;
using TMPro;
using UnityEngine;

namespace UI.Boss
{
    // 보스 등장 연출 레터박스 + 이름 표시 View (UGUI).
    // BossIntroStartedEvent → 상하 레터박스 슬라이드인 + 이름 페이드인
    // BossIntroEndedEvent   → 이름 페이드아웃 → 레터박스 슬라이드아웃
    //
    // Inspector 계층 구조 예시:
    //   Canvas (Screen Space - Overlay)
    //     ├─ TopBar      (Image, Anchors: top-stretch,    Pivot: 0.5,1)
    //     ├─ BottomBar   (Image, Anchors: bottom-stretch, Pivot: 0.5,0)
    //     └─ NameGroup   (CanvasGroup)
    //          └─ BossNameText (TextMeshProUGUI)
    public class BossIntroView : MonoBehaviour
    {
        [SerializeField] private EventChannelSO bossEventChannel;
        [SerializeField] private string bossDisplayName = "FALLEN KNIGHT";
        [SerializeField] private int canvasSortOrder = 100;

        [Header("레터박스")]
        [SerializeField] private RectTransform topBar;
        [SerializeField] private RectTransform bottomBar;
        [SerializeField] private float barHeightPx = 120f;
        [SerializeField] private float barSlideInDuration = 0.4f;
        [SerializeField] private CanvasGroup canvasGroup;    
        
        [Header("보스 이름")]
        [SerializeField] private CanvasGroup nameGroup;
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private float nameDelay = 0.5f;
        [SerializeField] private float nameFadeDuration = 0.4f;

        [Header("아웃 타이밍")]
        [SerializeField] private float outDuration = 0.35f;

        private Tween _topTween;
        private Tween _bottomTween;
        private Tween _nameTween;

        private void Awake()
        {
            var canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
            if (canvas != null) canvas.sortingOrder = canvasSortOrder;
        }

        private void OnEnable()
        {
            bossEventChannel?.AddListener<BossIntroStartedEvent>(HandleIntroStarted);
            bossEventChannel?.AddListener<BossIntroEndedEvent>(HandleIntroEnded);
            ResetToHidden();
        }

        private void OnDisable()
        {
            bossEventChannel?.RemoveListener<BossIntroStartedEvent>(HandleIntroStarted);
            bossEventChannel?.RemoveListener<BossIntroEndedEvent>(HandleIntroEnded);
            KillAll();
        }

        private void ResetToHidden()
        {
            KillAll();
            SetBarY(topBar,    barHeightPx);
            SetBarY(bottomBar, -barHeightPx);
            if (nameGroup != null) nameGroup.alpha = 0f;
        }

        private void HandleIntroStarted(BossIntroStartedEvent _)
        {
            if (bossNameText != null) bossNameText.text = bossDisplayName;
            canvasGroup.alpha = 1;
            SlideIn();
        }

        private void HandleIntroEnded(BossIntroEndedEvent _)
        {
            canvasGroup.alpha = 0;
            SlideOut();
        }

        private void SlideIn()
        {
            KillAll();
            _topTween    = topBar.DOAnchorPosY(0f, barSlideInDuration).SetEase(Ease.OutCubic);
            _bottomTween = bottomBar.DOAnchorPosY(0f, barSlideInDuration).SetEase(Ease.OutCubic);
            _nameTween   = nameGroup.DOFade(1f, nameFadeDuration).SetDelay(nameDelay);
        }

        private void SlideOut()
        {
            KillAll();
            _nameTween   = nameGroup.DOFade(0f, outDuration);
            _topTween    = topBar.DOAnchorPosY(barHeightPx, outDuration)
                                 .SetEase(Ease.InCubic).SetDelay(outDuration * 0.5f);
            _bottomTween = bottomBar.DOAnchorPosY(-barHeightPx, outDuration)
                                    .SetEase(Ease.InCubic).SetDelay(outDuration * 0.5f);
        }

        private static void SetBarY(RectTransform rt, float y)
        {
            if (rt == null) return;
            var pos = rt.anchoredPosition;
            pos.y = y;
            rt.anchoredPosition = pos;
        }

        private void KillAll()
        {
            _topTween?.Kill();
            _bottomTween?.Kill();
            _nameTween?.Kill();
        }
    }
}
