using UnityEngine;

namespace System.Editor
{
    [CreateAssetMenu(fileName = "stage prefab", menuName = "StagePrefabListSO", order = 0)]
    public class StagePrefabListSO : ScriptableObject
    {
        public GameObject[] prefabs;
    }
}