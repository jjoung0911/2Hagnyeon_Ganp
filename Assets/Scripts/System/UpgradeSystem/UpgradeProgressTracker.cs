using System.Collections.Generic;

namespace System.UpgradeSystem
{
    // IUpgradeProgress의 런타임 구현. PlayerUpgradeModule이 인스턴스 하나를 보유한다.
    public class UpgradeProgressTracker : IUpgradeProgress
    {
        private readonly Dictionary<int, int> _stackByIndex = new();

        public int GetStack(int upgradeIndex)
            => _stackByIndex.TryGetValue(upgradeIndex, out int count) ? count : 0;

        public void AddStack(int upgradeIndex)
            => _stackByIndex[upgradeIndex] = GetStack(upgradeIndex) + 1;
    }
}
