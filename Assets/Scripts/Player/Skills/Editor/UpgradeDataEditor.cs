using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Agents.CombatSystem;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Player.Skills.Editor
{
    // SkillUpgradeSO 전용 커스텀 인스펙터.
    // 대상 스킬 클래스를 드롭다운으로 고르고, 그 스킬의 업그레이드 가능 필드를
    // 필드별 행(필드 선택 + 타입에 맞는 값 입력)으로 편집한다.
    // 같은 필드를 여러 행에서 중복 선택하지 못하도록 막는다.
    [CustomEditor(typeof(SkillUpgradeSO))]
    public class UpgradeDataEditor : UnityEditor.Editor
    {
        // 업그레이드로 덮어쓸 수 있는 필드 타입
        private static readonly HashSet<Type> SupportedTypes = new()
        {
            typeof(float), typeof(int), typeof(bool)
        };

        [SerializeField] private VisualTreeAsset visualTree;

        private DropdownField _targetSkillDropdown;
        private ListView _upgradeDataListView;
        private HelpBox _validationBox;

        // 현재 선택된 스킬의 업그레이드 가능 필드 이름 → 타입
        private readonly Dictionary<string, Type> _fieldTypes = new();
        private readonly List<string> _fieldNames = new();

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            if (visualTree != null)
                visualTree.CloneTree(root);

            _targetSkillDropdown = root.Q<DropdownField>("TargetSkillDropdownField");
            _upgradeDataListView = root.Q<ListView>("UpgradeDataListView");

            // 중복/유효하지 않은 필드 경고 — 대상 필드 리스트 바로 위에 삽입
            _validationBox = new HelpBox(string.Empty, HelpBoxMessageType.Warning) { style = { display = DisplayStyle.None } };
            _upgradeDataListView.parent.Insert(_upgradeDataListView.parent.IndexOf(_upgradeDataListView), _validationBox);

            FillSkillChoices(_targetSkillDropdown);
            RefreshFieldCache();

            var targetSkillProp = serializedObject.FindProperty("targetSkill");
            _targetSkillDropdown.SetValueWithoutNotify(targetSkillProp.stringValue);
            _targetSkillDropdown.RegisterValueChangedCallback(HandleSkillChanged);

            SetupListView();
            UpdateValidation();

            // PropertyField 및 ListView 자동 바인딩
            root.Bind(serializedObject);
            return root;
        }

        // 대상 스킬 드롭다운: AbstractPlayerSkill 파생 구현체의 FullName 목록
        private void FillSkillChoices(DropdownField dropdown)
        {
            dropdown.choices.Clear();

            Assembly assembly = Assembly.GetAssembly(typeof(AbstractPlayerSkill));
            IEnumerable<Type> skills = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(AbstractPlayerSkill)))
                .OrderBy(t => t.Name);

            dropdown.choices.AddRange(skills.Select(t => t.FullName));
        }

        private void SetupListView()
        {
            _upgradeDataListView.bindingPath = "targetFields";
            _upgradeDataListView.reorderable = true;
            _upgradeDataListView.showAddRemoveFooter = true;
            _upgradeDataListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            _upgradeDataListView.makeItem = MakeRow;
            _upgradeDataListView.bindItem = BindRow;
            _upgradeDataListView.unbindItem = UnbindRow;

            // 새 행은 마지막 행을 복제하므로(=중복) 빈 상태로 초기화
            _upgradeDataListView.itemsAdded += HandleItemsAdded;
            // 행 삭제 시 다른 행의 선택 가능 필드가 늘어나므로 갱신
            _upgradeDataListView.itemsRemoved += _ => RefreshRowsAndValidation();
        }

        private VisualElement MakeRow()
        {
            VisualElement row = new() { name = "Row" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 1;
            row.style.marginBottom = 1;

            DropdownField fieldDropdown = new() { name = "FieldDropdown" };
            fieldDropdown.style.flexGrow = 1;
            fieldDropdown.style.marginRight = 4;
            // 행 재사용에 안전하도록 콜백은 1회만 등록하고 index는 userData로 추적
            fieldDropdown.RegisterValueChangedCallback(HandleFieldChanged);

            VisualElement valueHolder = new() { name = "ValueHolder" };
            valueHolder.style.flexGrow = 1;
            valueHolder.style.flexDirection = FlexDirection.Row;

            row.Add(fieldDropdown);
            row.Add(valueHolder);
            return row;
        }

        private void BindRow(VisualElement element, int index)
        {
            element.userData = index;

            SerializedProperty elementProp = ElementAt(index);
            if (elementProp == null) return;

            string current = elementProp.FindPropertyRelative("targetField").stringValue;

            var fieldDropdown = element.Q<DropdownField>("FieldDropdown");
            // 다른 행이 이미 쓰는 필드는 선택지에서 제외(중복 차단). 단 자기 자신의 현재 값은 유지.
            fieldDropdown.choices = AvailableFieldsFor(index, current);
            fieldDropdown.SetValueWithoutNotify(current);

            RebuildValueWidget(element, elementProp);
        }

        private void UnbindRow(VisualElement element, int index)
        {
            element.Q<VisualElement>("ValueHolder")?.Clear();
        }

        // 선택된 필드 종류(kind)에 맞는 값 입력 위젯을 생성하고 SerializedProperty에 바인딩
        private void RebuildValueWidget(VisualElement element, SerializedProperty elementProp)
        {
            var holder = element.Q<VisualElement>("ValueHolder");
            holder.Clear();

            var kind = (UpgradeValueKind)elementProp.FindPropertyRelative("kind").enumValueIndex;

            BindableElement widget = kind switch
            {
                UpgradeValueKind.Int => new IntegerField(),
                UpgradeValueKind.Bool => new Toggle(),
                _ => new FloatField()
            };
            widget.style.flexGrow = 1;
            widget.BindProperty(elementProp.FindPropertyRelative(ValuePropName(kind)));
            holder.Add(widget);
        }

        // 필드 드롭다운 변경: 대상 필드명 저장 + 필드 타입으로 kind 자동 지정 + 값 위젯 갱신
        private void HandleFieldChanged(ChangeEvent<string> evt)
        {
            if (evt.currentTarget is not DropdownField dropdown) return;
            if (dropdown.parent?.userData is not int index) return;

            SerializedProperty elementProp = ElementAt(index);
            if (elementProp == null) return;

            SerializedProperty fieldProp = elementProp.FindPropertyRelative("targetField");
            string newValue = evt.newValue;

            // 방어적 예외처리 — 선택지에서 제외했더라도 외부 경로로 중복이 들어오면 되돌린다
            if (!string.IsNullOrEmpty(newValue) && IsFieldUsedByOther(index, newValue))
            {
                Debug.LogWarning($"[SkillUpgradeSO] 필드 '{newValue}' 는 이미 다른 행에서 사용 중입니다. 중복 선택할 수 없습니다.");
                dropdown.SetValueWithoutNotify(fieldProp.stringValue);
                return;
            }

            fieldProp.stringValue = newValue;

            if (_fieldTypes.TryGetValue(newValue, out Type fieldType))
                elementProp.FindPropertyRelative("kind").enumValueIndex = (int)KindOf(fieldType);

            serializedObject.ApplyModifiedProperties();

            // 이 행이 필드를 점유했으므로 다른 행의 선택지도 갱신해야 한다
            RefreshRowsAndValidation();
        }

        private void HandleSkillChanged(ChangeEvent<string> evt)
        {
            serializedObject.FindProperty("targetSkill").stringValue = evt.newValue;
            serializedObject.ApplyModifiedProperties();

            RefreshFieldCache();
            RefreshRowsAndValidation();
        }

        // 새로 추가된 행을 빈 상태로 초기화 — 마지막 행 복제로 인한 중복 방지
        private void HandleItemsAdded(IEnumerable<int> indices)
        {
            foreach (int index in indices)
            {
                SerializedProperty elementProp = ElementAt(index);
                if (elementProp == null) continue;

                elementProp.FindPropertyRelative("targetField").stringValue = string.Empty;
                elementProp.FindPropertyRelative("kind").enumValueIndex = (int)UpgradeValueKind.Float;
                elementProp.FindPropertyRelative("floatValue").floatValue = 0f;
                elementProp.FindPropertyRelative("intValue").intValue = 0;
                elementProp.FindPropertyRelative("boolValue").boolValue = false;
            }

            serializedObject.ApplyModifiedProperties();
            RefreshRowsAndValidation();
        }

        private void RefreshRowsAndValidation()
        {
            _upgradeDataListView.RefreshItems();
            UpdateValidation();
        }

        // 중복 필드 / 현재 스킬에 없는 필드를 모아 경고 박스에 표시
        private void UpdateValidation()
        {
            if (_validationBox == null) return;

            SerializedProperty array = serializedObject.FindProperty("targetFields");
            if (array == null) { _validationBox.style.display = DisplayStyle.None; return; }

            var seen = new HashSet<string>();
            var duplicates = new HashSet<string>();
            var unknown = new HashSet<string>();

            for (int i = 0; i < array.arraySize; i++)
            {
                string name = array.GetArrayElementAtIndex(i).FindPropertyRelative("targetField").stringValue;
                if (string.IsNullOrEmpty(name)) continue;

                if (!seen.Add(name)) duplicates.Add(name);
                if (_fieldNames.Count > 0 && !_fieldTypes.ContainsKey(name)) unknown.Add(name);
            }

            var messages = new List<string>();
            if (duplicates.Count > 0)
                messages.Add($"중복 선택된 필드: {string.Join(", ", duplicates)}");
            if (unknown.Count > 0)
                messages.Add($"현재 스킬에 존재하지 않는 필드: {string.Join(", ", unknown)}");

            if (messages.Count == 0)
            {
                _validationBox.style.display = DisplayStyle.None;
            }
            else
            {
                _validationBox.text = string.Join("\n", messages);
                _validationBox.style.display = DisplayStyle.Flex;
            }
        }

        // index 행에서 고를 수 있는 필드 = 전체 필드 - 다른 행이 점유한 필드. 단 current는 항상 포함.
        private List<string> AvailableFieldsFor(int index, string current)
        {
            var used = UsedFieldsExcept(index);
            var list = _fieldNames.Where(f => !used.Contains(f)).ToList();
            if (!string.IsNullOrEmpty(current) && !list.Contains(current))
                list.Insert(0, current);
            return list;
        }

        private bool IsFieldUsedByOther(int index, string fieldName)
            => UsedFieldsExcept(index).Contains(fieldName);

        // index를 제외한 모든 행이 점유 중인 필드 이름 집합
        private HashSet<string> UsedFieldsExcept(int index)
        {
            var set = new HashSet<string>();
            SerializedProperty array = serializedObject.FindProperty("targetFields");
            if (array == null) return set;

            for (int i = 0; i < array.arraySize; i++)
            {
                if (i == index) continue;
                string name = array.GetArrayElementAtIndex(i).FindPropertyRelative("targetField").stringValue;
                if (!string.IsNullOrEmpty(name)) set.Add(name);
            }
            return set;
        }

        // 현재 targetSkill 기준으로 업그레이드 가능 필드 목록/타입 캐시 갱신
        private void RefreshFieldCache()
        {
            _fieldNames.Clear();
            _fieldTypes.Clear();

            Type skillType = GetSelectedSkillType();
            if (skillType == null) return;

            FieldInfo[] fields = skillType.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                // 컴파일러 생성 backing field(<Prop>k__BackingField) 등은 제외
                if (field.Name.Contains('<')) continue;
                if (!SupportedTypes.Contains(field.FieldType)) continue;

                _fieldNames.Add(field.Name);
                _fieldTypes[field.Name] = field.FieldType;
            }
        }

        private Type GetSelectedSkillType()
        {
            string fullName = serializedObject.FindProperty("targetSkill").stringValue;
            if (string.IsNullOrEmpty(fullName)) return null;
            return Assembly.GetAssembly(typeof(AbstractPlayerSkill)).GetType(fullName);
        }

        private SerializedProperty ElementAt(int index)
        {
            SerializedProperty array = serializedObject.FindProperty("targetFields");
            return index >= 0 && index < array.arraySize ? array.GetArrayElementAtIndex(index) : null;
        }

        private static UpgradeValueKind KindOf(Type type)
        {
            if (type == typeof(int)) return UpgradeValueKind.Int;
            if (type == typeof(bool)) return UpgradeValueKind.Bool;
            return UpgradeValueKind.Float;
        }

        private static string ValuePropName(UpgradeValueKind kind) => kind switch
        {
            UpgradeValueKind.Int => "intValue",
            UpgradeValueKind.Bool => "boolValue",
            _ => "floatValue"
        };
    }
}
