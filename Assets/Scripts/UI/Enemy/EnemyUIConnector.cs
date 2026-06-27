using Agents.Modules;
using UnityEngine;

namespace UI.Enemy
{
    // 적 프리팹에 부착. Awake에서 같은 프리팹의 HealthModule을 찾아 EnemyHpBarView·EnemySliceIndicator에 연결한다.
    // Enemy 프리팹 구조: AbstractEnemy → (하위) World Space Canvas → EnemyHpBarView, EnemySliceIndicator
    [DefaultExecutionOrder(10)]
    public class EnemyUIConnector : MonoBehaviour
    {
        [SerializeField] private EnemyHpBarView hpBarView;
        [SerializeField] private EnemySliceIndicator sliceIndicator;

        private void Awake()
        {
            var healthModule = GetComponentInParent<HealthModule>()
                               ?? GetComponentInChildren<HealthModule>();

            if (healthModule == null)
            {
                Debug.LogWarning($"[EnemyUIConnector] HealthModule을 찾지 못했습니다: {name}");
                return;
            }

            if (hpBarView != null) hpBarView.Setup(healthModule);
            if (sliceIndicator != null) sliceIndicator.Setup(healthModule);
        }
    }
}
