using csiimnida.CSILib.SoundManager.RunTime;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace UI.Menu
{
    // 메뉴 버튼 호버/클릭 스케일 연출 공통 컴포넌트.
    // 메뉴 버튼 GameObject에 부착하면 별도 코드 없이 동작한다.
    [RequireComponent(typeof(RectTransform))]
    public class MenuButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float pressScale = 0.95f;
        [SerializeField] private float duration = 0.12f;

        [Header("사운드")]
        [SerializeField] private SoundSo hoverSfx;

        private RectTransform _rect;
        private Tween _scaleTween;
        private bool _isHovering;

        private void Awake() => _rect = (RectTransform)transform;

        private void OnDisable()
        {
            _scaleTween?.Kill();
            _rect.localScale = Vector3.one;
            _isHovering = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovering = true;
            AnimateTo(hoverScale);
            if (hoverSfx != null) SoundManager.Instance.PlaySound(hoverSfx);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
            AnimateTo(1f);
        }

        public void StartGame()
        {
            SceneManager.LoadScene("GameScene");
        }
        
        private void AnimateTo(float scale)
        {
            _scaleTween?.Kill();
            _scaleTween = _rect.DOScale(scale, duration).SetEase(Ease.OutQuad);
        }
    }
}
