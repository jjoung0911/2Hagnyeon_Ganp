
using UnityEngine;

namespace System
{
    [CreateAssetMenu(fileName = "AssetTableSO", menuName = "SO/AssetTableSO", order = 0)]
    public class AssetTableSO : ScriptableObject
    {
        [field: SerializeField] public string TableName { get; private set; }

        [field:SerializeField] public IndexedAsset[] Assets { get; private set; }
    }
}