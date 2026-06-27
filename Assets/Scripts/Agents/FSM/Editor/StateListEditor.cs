using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Agents.FSM.Editor
{
    [CustomEditor(typeof(StateListSO))]
    public class StateListEditor : UnityEditor.Editor
    {
        public static class CodeFormat
        {
            public static string EnumFormat =
@"
namespace Agents.FSM
{{
    public enum {0}
    {{
        {1}
    }}
}}";
        }
        
        [SerializeField] private VisualTreeAsset treeAsset = default;
        
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            InspectorElement.FillDefaultInspector( root, serializedObject,this);
            treeAsset.CloneTree(root);

            root.Q<Button>("GenerateButton").clicked += HandleGenerateEnumClick;
            return root;
        }

        private void HandleGenerateEnumClick()
        {
            StateListSO listData = target as StateListSO;;

            int index = 0;
            string enumString = string.Join(",", listData.states.Select(so =>
            {
                so.stateIndex = index;
                EditorUtility.SetDirty(so);
                return $"{so.stateName} = {index++}";
            }));

            string code = string.Format(CodeFormat.EnumFormat, listData.enumName, enumString);

            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            string directoryName = Path.GetDirectoryName(scriptPath);
            DirectoryInfo parentDirectory = Directory.GetParent(directoryName);

            string path = parentDirectory.FullName;
            File.WriteAllText($"{path}/{listData.enumName}.cs", code);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}