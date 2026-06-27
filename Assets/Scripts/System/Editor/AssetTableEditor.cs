using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace System.Editor
{
    [CustomEditor(typeof(AssetTableSO), true)]
    public class AssetTableEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset viewAsset;
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            viewAsset.CloneTree(root);

            root.Q<Button>("GenerateButton").clicked += HandleGenerateIndex;
            return root;
        }

        private void HandleGenerateIndex()
        {
            AssetTableSO targetSo = target as AssetTableSO;

            int index = 0;
            foreach (var asset in targetSo.Assets)
            {
                asset.Index = index++;
                EditorUtility.SetDirty(asset);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}