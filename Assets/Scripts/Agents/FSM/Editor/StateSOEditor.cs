using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Agents.FSM.Editor
{
    [CustomEditor(typeof(StateSO))]
    public class StateSOEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset visualTree = default;
        
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            visualTree.CloneTree(root);

            DropdownField dropdownField = root.Q<DropdownField>("ClassDropdownField");
            FillDropdownField(dropdownField);
            return root;
        }

        private void FillDropdownField(DropdownField dropdownField)
        {
            dropdownField.choices.Clear();

            Assembly assembly = Assembly.GetAssembly(typeof(AgentState));
            List<Type> derivedTypes = assembly.GetTypes().Where(type =>
                type.IsClass && type.IsSubclassOf(typeof(AgentState)) && type.IsAbstract == false).ToList();
            
            dropdownField.choices.AddRange(derivedTypes.Select(type => type.FullName));
            
            if(derivedTypes.Count > 0 && !string.IsNullOrEmpty(dropdownField.value))
                dropdownField.SetValueWithoutNotify(derivedTypes[0].FullName);
        }
    }
}