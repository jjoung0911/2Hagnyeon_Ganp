using System.Collections.Generic;
using Agents.Modules;

namespace System.UpgradeSystem
{
    // 업그레이드 풀에서 제시할 선택지를 고르는 전략 인터페이스 (OCP — 새 선택 알고리즘은 새 구현체로 추가)
    public interface IUpgradeSelector
    {
        UpgradeDataSO[] SelectChoices(IReadOnlyList<UpgradeDataSO> pool, ModuleOwner owner, IUpgradeProgress progress, int count);
    }
}
