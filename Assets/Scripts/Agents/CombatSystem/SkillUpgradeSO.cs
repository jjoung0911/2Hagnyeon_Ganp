using System;
using System.Reflection;
using Player.Skills;
using UnityEngine;

namespace Agents.CombatSystem
{
    // 업그레이드가 덮어쓸 값의 종류. 필드 타입에 따라 에디터가 자동 지정한다.
    public enum UpgradeValueKind
    {
        Float,
        Int,
        Bool
    }

    [Serializable]
    public struct UpgradeData
    {
        public string targetField;

        public UpgradeValueKind kind;
        public float floatValue;
        public int intValue;
        public bool boolValue;

        public object BoxedValue => kind switch
        {
            UpgradeValueKind.Int => intValue,
            UpgradeValueKind.Bool => boolValue,
            _ => floatValue
        };
    }
    
    [CreateAssetMenu(fileName = "new Skill Upgrade", menuName = "SO/Skill/SkillUpgradeSO")]
    public class SkillUpgradeSO : ScriptableObject
    {
        public string targetSkill;
        public UpgradeData[] targetFields;
        
        public string tierName;
        [TextArea] public string description;
        public int upgradeCost = 1;

        [Header("공통 배율 (1 = 변화 없음)")]
        [Range(0f, 5f)] public float damageMultiplier = 1f;
        [Range(0f, 5f)] public float cooldownMultiplier = 1f;

        public void Upgrade(AbstractPlayerSkill skillInstance)
        {
            if (skillInstance == null) return;

            Type targetType = skillInstance.GetType();
            if (targetType.FullName != targetSkill && targetType.Name != targetSkill) return;

            foreach (var data in targetFields)
            {
                FieldInfo targetField = targetType.GetField(data.targetField,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (targetField != null)
                {
                    object wrapData = Convert.ChangeType(data.BoxedValue, targetField.FieldType);
                    targetField.SetValue(skillInstance, wrapData);
                }
            }
        }
    }
}
