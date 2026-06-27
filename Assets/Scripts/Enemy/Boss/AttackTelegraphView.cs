using System.Collections;
using UnityEngine;

namespace Enemy.Boss
{
    // 바닥 공격 범위 경고(텔레그래프) 표시. 스킬마다 Instantiate 후 Show()로 표시하고 자동 소멸한다.
    // radius는 메쉬 스케일, angle은 머티리얼 파라미터로 전달되며, 실제 형태는 프리팹의 메쉬·셰이더에서 결정한다.
    public class AttackTelegraphView : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private string alphaProperty = "_Alpha";
        [SerializeField] private string angleProperty = "_AngleDeg";
        // 데칼 투영 볼륨(큐브)의 높이 — 바닥 굴곡/높이차를 충분히 덮도록 설정. radius와 무관한 Y 두께.
        [SerializeField] private float projectionHeight = 8f;

        [Header("지면 한정 (GroundLayer 위에만 투영)")]
        // 이 레이어(지면)로 레이캐스트해 실제 바닥 Y를 구한다. 기본값 = Ground(6)
        [SerializeField] private LayerMask groundMask = 1 << 6;
        // 바닥 위로 이 높이까지는 허용(완만한 굴곡 대비). 이보다 높은 표면(에너미 몸체)엔 안 그려짐
        [SerializeField] private float heightTolerance = 0.5f;
        // 바닥 탐지용 위쪽 레이캐스트 시작 높이
        [SerializeField] private float groundProbeHeight = 5f;

        private static readonly int GroundYId = Shader.PropertyToID("_GroundY");
        private static readonly int HeightToleranceId = Shader.PropertyToID("_HeightTolerance");

        private MaterialPropertyBlock _propertyBlock;

        // 부채꼴 각도 설정. 같은 프리팹을 원형(360°)·콘(120°) 등 다양한 모양으로 표시할 수 있게 한다.
        public void SetAngle(float angleDeg)
        {
            if (targetRenderer == null) return;

            _propertyBlock ??= new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(angleProperty, angleDeg);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }

        // radius: 표시 반경, duration: 표시 시간. 페이드 후 자동 소멸한다. 각도는 SetAngle()로 별도 설정.
        public void Show(float radius, float duration)
        {
            _propertyBlock ??= new MaterialPropertyBlock();

            // XZ는 지름(=radius*2)로 투영 범위를, Y는 투영 볼륨 높이로 설정 (깊이 데칼 박스)
            transform.localScale = new Vector3(radius * 2f, projectionHeight, radius * 2f);

            StartCoroutine(PlayRoutine(duration));
        }

        private IEnumerator PlayRoutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                SetAlpha(alphaCurve.Evaluate(elapsed / duration));
                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        private void SetAlpha(float alpha)
        {
            if (targetRenderer == null) return;
            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(alphaProperty, alpha);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
