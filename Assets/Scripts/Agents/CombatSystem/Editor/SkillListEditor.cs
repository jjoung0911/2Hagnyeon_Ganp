using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Path = System.IO.Path;

namespace Agents.CombatSystem.Editor
{
    public static class CodeFormat
    {
        public static string EnumFormat =
            @"
namespace Agents.CombatSystem
{{
    public enum SkillEnum
    {{
        {0}
    }}
}}";
    }
    [CustomEditor(typeof(SkillDataTable))]
    public class SkillListEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset visual = default;
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            visual.CloneTree(root);
            
            Button generateBtn = root.Q<Button>("GenerateButton");
            generateBtn.clicked += HandleGenerateBtn;
            return root;
        }

        private void HandleGenerateBtn()
        {
            SkillDataTable table = target as SkillDataTable;

            int index = 0;
            string enumString = string.Join(",", table.skills.Select(so =>
            {
                so.skillIndex = index;
                EditorUtility.SetDirty(so);
                return $"{so.skillName} = {index++}";
            }));
            string code = string.Format(CodeFormat.EnumFormat, enumString);

            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            string directoryName = Path.GetDirectoryName(scriptPath);
            DirectoryInfo parentDirectory = Directory.GetParent(directoryName);

            string path = parentDirectory.FullName;
            File.WriteAllText($"{path}/SkillEnum.cs", code);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}