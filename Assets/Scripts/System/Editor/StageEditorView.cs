using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace System.Editor
{
    public class StageEditorView : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset editorView = default;

        private ObjectField _rootObjectField; // 프리팹이 자식으로 생성될 Transform이 담기는 field
        private ObjectField _prefabListField; // StagePrefabListSO를 담을 field
        private DropdownField _itemDropdownField; // 아이템 이름들을 담고있는 DropdownField
        private VisualElement _itemSelectContainer; // 아이템 드랍다운필드, 텍스쳐 담고 있는 VisualElement
        private VisualElement _previewImage; // 선택된 프리팹의 미리보기 이미지를 띄울 곳
        private FloatField _cellSizeField; // 셀 사이즈 입력 공간란
        private Vector3Field _pivotOffsetField;
        private Vector3Field _rotateField;

        private bool _isReadyToPlacement; // 설치가 가능한 상태인지 판단
        private static GameObject _rootObject;
        private static StagePrefabListSO _prefabList;
        private static float _cellSize;
        private static Vector3 _pivotOffset;
        private static Vector3 _rotate;
        private GameObject _selectedPrefab;
    
        [MenuItem("Tools/StageEditorView")]
        public static void ShowExample()
        {
            StageEditorView wnd = GetWindow<StageEditorView>();
            wnd.titleContent = new GUIContent("StageEditorView");
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            editorView.CloneTree(root);

            _rootObjectField = root.Q<ObjectField>("RootObjectField");
            _prefabListField = root.Q<ObjectField>("PrefabListObjectField");
            _itemDropdownField = root.Q<DropdownField>("ItemDropdownField");
            _itemSelectContainer = root.Q<VisualElement>("ItemSelectContainer");
            _cellSizeField = root.Q<FloatField>("CellSizeFloatField");
            _previewImage = root.Q<VisualElement>("PreviewImage");
            _pivotOffsetField = root.Q<Vector3Field>("PivotOffsetField");
            _rotateField = root.Q<Vector3Field>("RotateField");
        
            _prefabListField.RegisterValueChangedCallback(HandlePrefabListChange);
            _rootObjectField.RegisterValueChangedCallback(HandleRootObjectChange);
            _itemDropdownField.RegisterValueChangedCallback(HandleItemSelect);
            _cellSizeField.RegisterValueChangedCallback(evt => { _cellSize = evt.newValue; });
            _pivotOffsetField.RegisterValueChangedCallback(evt => { _pivotOffset = evt.newValue; });
            _rotateField.RegisterValueChangedCallback(evt => { _rotate = evt.newValue; });

            _pivotOffset = _pivotOffsetField.value;
        
            if(_rootObject != null)
                _rootObjectField.SetValueWithoutNotify(_rootObject);
            if(_prefabList != null)
                _prefabListField.SetValueWithoutNotify(_prefabList);
        
            CheckSelectContainerActive();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += HandleSceneGui;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= HandleSceneGui;
        }

        private void HandleSceneGui(SceneView sceneView)
        {
            if (!_isReadyToPlacement) return;
            Event evt = Event.current; // 현재 이벤트 받아옴

            Ray ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition); // 씬의 마우스 위치를 월드 포인트로 변환
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out var distance))
            {
                Vector3 worldPoint = ray.GetPoint(distance);

                Vector3 snappedPoint = new Vector3(
                    Mathf.Floor(worldPoint.x / _cellSize) * _cellSize + _cellSize * 0.5f,
                    0,
                    Mathf.Floor(worldPoint.z / _cellSize) * _cellSize + _cellSize * 0.5f);

                if (_selectedPrefab != null)
                {
                    DrawPrefabGizmo(snappedPoint);
                    if (evt.type == EventType.MouseDown && evt.button == 0)
                    {
                        PlacePrefab(snappedPoint);
                        evt.Use();
                    }
                }
            }
        }

        private void PlacePrefab(Vector3 snappedPoint)
        {
            if(_selectedPrefab == null || _isReadyToPlacement == false) return;

            // Vector3 pivotOffset = new Vector3(_cellSize * 0.5f, 0, -_cellSize * 0.5f);
            Vector3 placementPosition = snappedPoint + _pivotOffset;

            GameObject newInstance = PrefabUtility.InstantiatePrefab(_selectedPrefab, _rootObject.transform) as GameObject;
            newInstance.transform.position = placementPosition;
            newInstance.transform.rotation = Quaternion.Euler(_rotate);
        
            Undo.RegisterCompleteObjectUndo(newInstance, $"Placed Prefab{newInstance.name}");
        }

        private void DrawPrefabGizmo(Vector3 pos)
        {
            Handles.color = Color.magenta;
            // Handles.DrawWireCube(pos, new Vector3(_cellSize, 0.1f, _cellSize));
            Matrix4x4 rotateMatrix = Matrix4x4.TRS(pos, Quaternion.Euler(_rotate),Vector3.one );
            using (new Handles.DrawingScope(rotateMatrix))
            {
                Handles.DrawWireCube(Vector3.zero, new Vector3(_cellSize, 0.1f, _cellSize));
            }
        }
    
        private void HandleItemSelect(ChangeEvent<string> evt)
        {
            if (String.IsNullOrEmpty(evt.newValue))
            {
                _previewImage.style.backgroundImage = null;
                _isReadyToPlacement = false;
                _selectedPrefab = null;
                return;
            }

            _selectedPrefab = _prefabList.prefabs[_itemDropdownField.index];
            Texture2D preview = AssetPreview.GetAssetPreview(_selectedPrefab);
            if (preview != null)
            {
                _previewImage.style.backgroundImage = preview;
            }
            else
            {
                if (AssetPreview.IsLoadingAssetPreview(_selectedPrefab.GetInstanceID()))
                {
                    _previewImage.schedule.Execute(() =>
                    {
                        Texture2D loadedPreview = AssetPreview.GetAssetPreview(_selectedPrefab);
                        if (loadedPreview != null)
                            _previewImage.style.backgroundImage = loadedPreview;
                    }).Until(() => !AssetPreview.IsLoadingAssetPreview(_selectedPrefab.GetInstanceID()));
                }
            }

            _isReadyToPlacement = true;
        }

        private void CheckSelectContainerActive()
        {
            bool isReadyToView = _rootObject != null && _prefabList != null;
            _itemSelectContainer.style.display = isReadyToView ? DisplayStyle.Flex : DisplayStyle.None;
            if (isReadyToView)
            {
                _itemDropdownField.choices.Clear();
                _itemDropdownField.choices.AddRange(_prefabList.prefabs.Select(item => item.name));
            }
            else
            {
                _isReadyToPlacement = false;
            }
        }
    
        private void HandlePrefabListChange(ChangeEvent<UnityEngine.Object> evt)
        {
            _prefabList = evt.newValue as StagePrefabListSO;
            CheckSelectContainerActive();
        }

        private void HandleRootObjectChange(ChangeEvent<UnityEngine.Object> evt)
        {
            GameObject newRootObj = evt.newValue as GameObject;

            if (PrefabUtility.IsPartOfPrefabAsset(newRootObj))
            {
                _rootObject = evt.previousValue as GameObject;
                _rootObjectField.SetValueWithoutNotify(null);
                EditorUtility.DisplayDialog("Error", "루트 오브젝트는 프리팹이 아닌 하이라키의 오브젝트여야 합니다", "OK");
            }

            _rootObject = newRootObj;
            CheckSelectContainerActive();
        }
    }
}
