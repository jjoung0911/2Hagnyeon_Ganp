using UnityEngine;

namespace System.UpgradeSystem
{
    // 업그레이드 선택지 풀 전체를 묶는 테이블 에셋
    [CreateAssetMenu(fileName = "New Upgrade Table", menuName = "SO/Upgrade/UpgradeTableSO", order = 0)]
    public class UpgradeTableSO : ScriptableObject
    {
        [field: SerializeField] public UpgradeDataSO[] Upgrades { get; private set; }
    }
}
