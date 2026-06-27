using System.Collections.Generic;
using System.Linq;
using Agents.Modules;
using UnityEngine;

namespace System.UpgradeSystem
{
    // 기본 선택 전략: CanApply == true인 업그레이드 중 등급별 가중치 랜덤으로 중복 없이 count개 선택
    [Serializable]
    public class RandomUpgradeSelector : IUpgradeSelector
    {
        [SerializeField] private float commonWeight = 100f;
        [SerializeField] private float rareWeight = 50f;
        [SerializeField] private float epicWeight = 20f;
        [SerializeField] private float legendaryWeight = 5f;

        public UpgradeDataSO[] SelectChoices(IReadOnlyList<UpgradeDataSO> pool, ModuleOwner owner, IUpgradeProgress progress, int count)
        {
            List<UpgradeDataSO> candidates = pool
                .Where(upgrade => upgrade != null && upgrade.CanApply(owner, progress))
                .ToList();
            List<UpgradeDataSO> result = new();

            for (int i = 0; i < count && candidates.Count > 0; i++)
            {
                float totalWeight = candidates.Sum(GetWeight);
                if (totalWeight <= 0f) break;

                float roll = UnityEngine.Random.Range(0f, totalWeight);
                float accumulated = 0f;
                int pickedIndex = candidates.Count - 1;

                for (int c = 0; c < candidates.Count; c++)
                {
                    accumulated += GetWeight(candidates[c]);
                    if (roll <= accumulated)
                    {
                        pickedIndex = c;
                        break;
                    }
                }
                
                result.Add(candidates[pickedIndex]);
                candidates.RemoveAt(pickedIndex);
            }

            return result.ToArray();
        }

        private float GetWeight(UpgradeDataSO upgrade) => upgrade.Grade switch
        {
            UpgradeGrade.Rare => rareWeight,
            UpgradeGrade.Epic => epicWeight,
            UpgradeGrade.Legendary => legendaryWeight,
            _ => commonWeight
        };
    }
}
