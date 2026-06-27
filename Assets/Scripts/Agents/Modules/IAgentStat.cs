using System.StatSystem;

namespace Agents.Modules
{
    public interface IAgentStat
    {
        StatSO GetStat(int assetIndex);
        bool TryGetStat(int index, out StatSO outStat);
        void AddModifier(int assetIndex, object key, float val);
        void RemoveModifier(int assetIndex, object key);
        float SubscribeStat(int assetIndex, StatSO.ValueChangeHandler handler, float defaultVal);
        void UnSubscribeStat(int assetIndex, StatSO.ValueChangeHandler handler);
    }
}