using UnityEngine;

namespace System
{
    public abstract class IndexedAsset : ScriptableObject
    {
        [field:SerializeField]public int Index { get; set; }
    }
}