using UnityEngine;

namespace System
{
    [CreateAssetMenu(fileName = "AssetNameSO", menuName = "SO/AssetNameSO", order = 0)]
    public class AssetNameSO : ScriptableObject
    {
        [field: SerializeField] public string AssetName { get; private set; }
        [field: SerializeField] public int AssetHash { get; private set; }

        private void OnValidate()
        {
            if(string.IsNullOrEmpty(AssetName))
                AssetHash = 0;
            else
                AssetHash = AssetName.GetHashCode();
        }
    }
}