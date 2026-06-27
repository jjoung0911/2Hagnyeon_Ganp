namespace System.UpgradeSystem
{
    // 업그레이드별 누적 적용 횟수를 조회/기록하는 인터페이스
    public interface IUpgradeProgress
    {
        int GetStack(int upgradeIndex);
        void AddStack(int upgradeIndex);
    }
}
