using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ganp.EditorTools
{
    public class AnimationFrameVFXEditor : EditorWindow
    {
        private GameObject previewRoot;
        private Animator previewAnimator;
        private RuntimeAnimatorController animatorController;
        private AnimationClip animationClip;
        private readonly List<AnimationClip> controllerClips = new List<AnimationClip>();
        private const string TempPreviewFolder = "Assets/__AnimationVFXPreview";
        private Scene workScene;
        private GameObject workSceneRoot;
        private Animator workSceneAnimator;
        private bool workSceneAnimatorWasEnabled;
        private Camera workSceneCamera;
        private RenderTexture scenePreviewTexture;
        private GameObject vfxPrefab;
        private Transform spawnParent;
        private Transform spawnAnchor;
        private GameObject selectedWorkObject;
        private Vector3 positionOffset;
        private Vector3 rotationOffset;
        private int frame;
        private int selectedClipIndex = -1;
        private int scenePreviewTool;
        private bool sampleAutomatically = true;
        private bool keepSamplingWhenClosed;
        private bool closeWorkSceneWithWindow = true;
        private Vector2 clipListScroll;
        private Vector2 previewPanelScroll;
        private Vector2 workHierarchyScroll;
        private Vector2 scenePreviewOrbit = new Vector2(30f, -35f);
        private float scenePreviewDistance = 6f;
        private Vector3 scenePreviewPan;
        private bool drawPreviewGrid = true;

        [MenuItem("Tools/Animation Frame VFX Editor")]
        private static void Open()
        {
            AnimationFrameVFXEditor window = GetWindow<AnimationFrameVFXEditor>();
            window.titleContent = new GUIContent("Animation VFX");
            window.minSize = new Vector2(1040f, 520f);
            window.Show();

            if (window.previewRoot == null && Selection.activeGameObject != null)
            {
                window.previewRoot = Selection.activeGameObject;
                window.LoadAnimatorClipsFromPreviewRoot();
            }
        }

        private void OnDisable()
        {
            if (!keepSamplingWhenClosed)
            {
                StopSampling();
            }

            if (closeWorkSceneWithWindow)
            {
                CloseWorkScene();
            }

            ReleaseScenePreviewTexture();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(260f)))
                {
                    DrawAnimatorAndClipList();
                }

                GUILayout.Space(8f);

                previewPanelScroll = EditorGUILayout.BeginScrollView(previewPanelScroll);
                DrawPreviewAndPlacementPanel();
                EditorGUILayout.EndScrollView();

                GUILayout.Space(8f);

                using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(640f)))
                {
                    DrawScenePreviewPanel();
                }
            }
        }

        private void DrawAnimatorAndClipList()
        {
            EditorGUILayout.LabelField("Animator Source", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            previewRoot = (GameObject)EditorGUILayout.ObjectField("Preview Root", previewRoot, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                CloseWorkScene();
                LoadAnimatorClipsFromPreviewRoot();
            }

            DrawWorkSceneControls();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Animator", previewAnimator, typeof(Animator), true);
                EditorGUILayout.ObjectField("Controller", animatorController, typeof(RuntimeAnimatorController), false);
            }

            if (GUILayout.Button("Refresh Clips"))
            {
                LoadAnimatorClipsFromPreviewRoot();
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"Controller Clips ({controllerClips.Count})", EditorStyles.boldLabel);

            if (previewRoot != null && previewAnimator == null)
            {
                EditorGUILayout.HelpBox("No Animator was found on this object or its children.", MessageType.Warning);
            }
            else if (previewAnimator != null && animatorController == null)
            {
                EditorGUILayout.HelpBox("The Animator has no Runtime Animator Controller.", MessageType.Warning);
            }

            clipListScroll = EditorGUILayout.BeginScrollView(clipListScroll);
            for (int i = 0; i < controllerClips.Count; i++)
            {
                AnimationClip clip = controllerClips[i];
                bool isSelected = i == selectedClipIndex;
                string label = $"{clip.name}  ({clip.length:0.00}s, {clip.frameRate:0.#}fps)";

                if (GUILayout.Toggle(isSelected, label, "Button"))
                {
                    if (!isSelected)
                    {
                        SelectClip(i);
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Manual Clip", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            AnimationClip manualClip = (AnimationClip)EditorGUILayout.ObjectField(animationClip, typeof(AnimationClip), false);
            if (EditorGUI.EndChangeCheck())
            {
                selectedClipIndex = controllerClips.IndexOf(manualClip);
                animationClip = manualClip;
                frame = 0;
                ClampFrame();
                SampleCurrentFrame();
            }
        }

        private void DrawPreviewAndPlacementPanel()
        {
            EditorGUILayout.LabelField("Frame Preview", EditorStyles.boldLabel);

            sampleAutomatically = EditorGUILayout.Toggle("Auto Sample Frame", sampleAutomatically);
            keepSamplingWhenClosed = EditorGUILayout.Toggle("Keep Pose On Close", keepSamplingWhenClosed);

            using (new EditorGUI.DisabledScope(animationClip == null))
            {
                DrawFrameControls();
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("VFX Placement", EditorStyles.boldLabel);
            vfxPrefab = (GameObject)EditorGUILayout.ObjectField("VFX Prefab", vfxPrefab, typeof(GameObject), false);
            spawnParent = (Transform)EditorGUILayout.ObjectField("Spawn Parent", spawnParent, typeof(Transform), true);
            spawnAnchor = (Transform)EditorGUILayout.ObjectField("Spawn Anchor", spawnAnchor, typeof(Transform), true);
            positionOffset = EditorGUILayout.Vector3Field("Position Offset", positionOffset);
            rotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", rotationOffset);

            EditorGUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(previewRoot == null || animationClip == null))
            {
                if (GUILayout.Button(vfxPrefab == null ? "Create Empty VFX Marker At Frame" : "Place VFX Prefab At Frame", GUILayout.Height(32f)))
                {
                    PlaceVFXObject();
                }
            }

            EditorGUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(!AnimationMode.InAnimationMode()))
            {
                if (GUILayout.Button("Stop Animation Sampling"))
                {
                    StopSampling();
                }
            }

            EditorGUILayout.HelpBox(
                "Assign a scene object to Preview Root. Create a work scene to edit a clone without touching the original scene, then save the clone as a prefab when the VFX timing is set.",
                MessageType.Info);
        }

        private void DrawScenePreviewPanel()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(420f)))
                {
                    DrawSceneViewportPanel();
                }

                GUILayout.Space(6f);

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(Mathf.Clamp(position.width * 0.24f, 280f, 420f))))
                {
                    DrawWorkHierarchyPanel();
                }
            }
        }

        private void DrawSceneViewportPanel()
        {
            EditorGUILayout.LabelField("Work Scene Preview", EditorStyles.boldLabel);
            scenePreviewTool = GUILayout.Toolbar(scenePreviewTool, new[] { "Q View", "Select", "W Move", "E Rotate", "R Scale" });
            drawPreviewGrid = EditorGUILayout.Toggle("Grid", drawPreviewGrid);

            float previewHeight = Mathf.Max(280f, position.height - 90f);
            Rect previewRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                GUIStyle.none,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(previewHeight));

            if (!IsWorkSceneOpen() || workSceneRoot == null)
            {
                EditorGUI.DrawRect(previewRect, new Color(0.12f, 0.12f, 0.12f));
                GUI.Label(previewRect, "Create a Work Scene to preview it here.", EditorStyles.centeredGreyMiniLabel);

                using (new EditorGUI.DisabledScope(previewRoot == null))
                {
                    if (GUILayout.Button("Create Work Scene Preview"))
                    {
                        CreateWorkScene();
                    }
                }

                return;
            }

            HandleScenePreviewShortcuts(previewRect);

            if (Event.current.type == EventType.Repaint)
            {
                RenderScenePreview(previewRect);
            }

            if (scenePreviewTexture != null)
            {
                GUI.DrawTexture(previewRect, scenePreviewTexture, ScaleMode.StretchToFill, false);
            }

            DrawPreviewGrid(previewRect);
            DrawTransformHandle(previewRect);
            HandleScenePreviewInput(previewRect);
            DrawScenePreviewSelectionOverlay(previewRect);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Frame"))
                {
                    FrameWorkScenePreview();
                }
            }

            DrawSelectedObjectControls();
            EditorGUILayout.LabelField("Q View, W Move, E Rotate, R Scale. LMB selects, Alt+LMB or Q+LMB orbits, MMB/RMB pans, wheel zooms.", EditorStyles.miniLabel);
        }

        private void DrawWorkHierarchyPanel()
        {
            EditorGUILayout.LabelField("Work Hierarchy", EditorStyles.boldLabel);

            if (!IsWorkSceneOpen() || workSceneRoot == null)
            {
                EditorGUILayout.HelpBox("Create a Work Scene to edit the copied Preview Root hierarchy.", MessageType.Info);
                return;
            }

            workHierarchyScroll = EditorGUILayout.BeginScrollView(workHierarchyScroll);
            DrawHierarchyNode(workSceneRoot.transform, 0);
            EditorGUILayout.EndScrollView();
        }

        private void DrawHierarchyNode(Transform target, int depth)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(depth * 12f);
                bool isSelected = selectedWorkObject == target.gameObject;
                string label = target.childCount > 0 ? $"{target.name} ({target.childCount})" : target.name;
                if (GUILayout.Toggle(isSelected, label, "Button", GUILayout.MinWidth(120f)))
                {
                    selectedWorkObject = target.gameObject;
                    Repaint();
                }
            }

            for (int i = 0; i < target.childCount; i++)
            {
                DrawHierarchyNode(target.GetChild(i), depth + 1);
            }
        }

        private void DrawWorkSceneControls()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Work Scene", EditorStyles.boldLabel);
            closeWorkSceneWithWindow = EditorGUILayout.Toggle("Close With Window", closeWorkSceneWithWindow);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Scene Root", workSceneRoot, typeof(GameObject), true);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(previewRoot == null))
                {
                    if (GUILayout.Button(IsWorkSceneOpen() ? "Rebuild" : "Create"))
                    {
                        CreateWorkScene();
                    }
                }

                using (new EditorGUI.DisabledScope(!IsWorkSceneOpen()))
                {
                    if (GUILayout.Button("Close"))
                    {
                        CloseWorkScene();
                    }
                }
            }

            using (new EditorGUI.DisabledScope(previewRoot == null))
            {
                if (GUILayout.Button("Open Copy In Prefab Mode"))
                {
                    OpenPreviewRootCopyInPrefabMode();
                }
            }

            using (new EditorGUI.DisabledScope(!IsWorkSceneOpen() || workSceneRoot == null))
            {
                if (GUILayout.Button("Save Root As Prefab"))
                {
                    SaveWorkSceneRootAsPrefab();
                }
            }
        }

        private void LoadAnimatorClipsFromPreviewRoot()
        {
            previewAnimator = previewRoot != null ? previewRoot.GetComponentInChildren<Animator>(true) : null;
            animatorController = previewAnimator != null ? previewAnimator.runtimeAnimatorController : null;

            controllerClips.Clear();
            if (animatorController != null)
            {
                controllerClips.AddRange(animatorController.animationClips
                    .Where(clip => clip != null)
                    .GroupBy(clip => clip.GetInstanceID())
                    .Select(group => group.First())
                    .OrderBy(clip => clip.name));
            }

            if (controllerClips.Count == 0)
            {
                selectedClipIndex = -1;
                animationClip = null;
            }
            else
            {
                int existingIndex = animationClip != null ? controllerClips.IndexOf(animationClip) : -1;
                SelectClip(existingIndex >= 0 ? existingIndex : 0);
                return;
            }

            frame = 0;
            ClampFrame();
            SampleCurrentFrame();
            Repaint();
        }

        private void SelectClip(int index)
        {
            selectedClipIndex = Mathf.Clamp(index, 0, controllerClips.Count - 1);
            animationClip = controllerClips[selectedClipIndex];
            frame = 0;
            ClampFrame();
            SampleCurrentFrame();
            Repaint();
        }

        private void DrawFrameControls()
        {
            AnimationClip clip = animationClip;
            if (clip == null)
            {
                EditorGUILayout.HelpBox("Select an animation clip to preview frames.", MessageType.Info);
                return;
            }

            int maxFrame = GetMaxFrame();
            float frameRate = GetFrameRate();
            float currentTime = GetCurrentTime();

            EditorGUI.BeginChangeCheck();
            frame = EditorGUILayout.IntSlider("Frame", frame, 0, maxFrame);
            if (EditorGUI.EndChangeCheck())
            {
                SampleCurrentFrame();
            }

            EditorGUILayout.LabelField("Time", $"{currentTime:0.000}s / {clip.length:0.000}s");
            EditorGUILayout.LabelField("Frame Rate", $"{frameRate:0.##} fps");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("First"))
                {
                    SetFrame(0);
                }

                if (GUILayout.Button("Prev"))
                {
                    SetFrame(frame - 1);
                }

                if (GUILayout.Button("Sample"))
                {
                    SampleCurrentFrame(true);
                }

                if (GUILayout.Button("Next"))
                {
                    SetFrame(frame + 1);
                }

                if (GUILayout.Button("Last"))
                {
                    SetFrame(maxFrame);
                }
            }
        }

        private void SetFrame(int newFrame)
        {
            frame = Mathf.Clamp(newFrame, 0, GetMaxFrame());
            SampleCurrentFrame();
            Repaint();
        }

        private void ClampFrame()
        {
            frame = Mathf.Clamp(frame, 0, GetMaxFrame());
        }

        private int GetMaxFrame()
        {
            if (animationClip == null)
            {
                return 0;
            }

            return Mathf.Max(0, Mathf.RoundToInt(animationClip.length * GetFrameRate()));
        }

        private float GetFrameRate()
        {
            if (animationClip == null || animationClip.frameRate <= 0f)
            {
                return 60f;
            }

            return animationClip.frameRate;
        }

        private float GetCurrentTime()
        {
            if (animationClip == null)
            {
                return 0f;
            }

            return Mathf.Clamp(frame / GetFrameRate(), 0f, animationClip.length);
        }

        private void SampleCurrentFrame(bool force = false)
        {
            if (!force && !sampleAutomatically)
            {
                return;
            }

            AnimationClip clip = animationClip;
            GameObject samplingRoot = GetSamplingRoot();
            if (samplingRoot == null || clip == null)
            {
                return;
            }

            if (IsWorkSceneOpen())
            {
                StopSampling();
                SampleWorkSceneClip(clip);
                return;
            }

            if (!AnimationMode.InAnimationMode())
            {
                AnimationMode.StartAnimationMode();
            }

            AnimationMode.BeginSampling();
            GameObject animationRoot = previewAnimator != null ? previewAnimator.gameObject : samplingRoot;
            AnimationMode.SampleAnimationClip(animationRoot, clip, GetCurrentTime());
            AnimationMode.EndSampling();
            SceneView.RepaintAll();
        }

        private void SampleWorkSceneClip(AnimationClip clip)
        {
            if (workSceneRoot == null)
            {
                return;
            }

            float currentTime = GetCurrentTime();
            clip.SampleAnimation(workSceneRoot, currentTime);

            if (workSceneAnimator != null && workSceneAnimator.gameObject != workSceneRoot)
            {
                clip.SampleAnimation(workSceneAnimator.gameObject, currentTime);
            }

            Repaint();
        }

        private void StopSampling()
        {
            if (AnimationMode.InAnimationMode())
            {
                AnimationMode.StopAnimationMode();
                SceneView.RepaintAll();
            }
        }

        private void PlaceVFXObject()
        {
            SampleCurrentFrame(true);

            GameObject placementRoot = GetSamplingRoot();
            if (placementRoot == null)
            {
                return;
            }

            Transform anchor = spawnAnchor != null && spawnAnchor.gameObject.scene == placementRoot.scene
                ? spawnAnchor
                : placementRoot.transform;
            Transform parent = spawnParent != null && spawnParent.gameObject.scene == placementRoot.scene
                ? spawnParent
                : placementRoot.transform;
            Vector3 position = anchor.TransformPoint(positionOffset);
            Quaternion rotation = anchor.rotation * Quaternion.Euler(rotationOffset);

            GameObject instance;
            if (vfxPrefab != null)
            {
                instance = PrefabUtility.InstantiatePrefab(vfxPrefab, parent) as GameObject;
                if (instance == null)
                {
                    instance = Instantiate(vfxPrefab, parent);
                }
            }
            else
            {
                instance = new GameObject("VFX Marker");
                instance.transform.SetParent(parent);
            }

            instance.name = $"{(vfxPrefab != null ? vfxPrefab.name : "VFX Marker")}_{animationClip.name}_F{frame}";
            instance.transform.SetPositionAndRotation(position, rotation);
            Undo.RegisterCreatedObjectUndo(instance, $"Place VFX at frame {frame}");

            if (!IsWorkSceneOpen())
            {
                Selection.activeGameObject = instance;
                EditorGUIUtility.PingObject(instance);
            }
        }

        private GameObject GetSamplingRoot()
        {
            return IsWorkSceneOpen() && workSceneRoot != null ? workSceneRoot : previewRoot;
        }

        private bool IsWorkSceneOpen()
        {
            return workScene.IsValid() && workScene.isLoaded;
        }

        private void CreateWorkScene()
        {
            if (previewRoot == null)
            {
                return;
            }

            StopSampling();
            CloseWorkScene();
            workScene = EditorSceneManager.NewPreviewScene();

            workSceneRoot = Instantiate(previewRoot);
            workSceneRoot.name = $"{previewRoot.name}_VFX_Work";
            SceneManager.MoveGameObjectToScene(workSceneRoot, workScene);
            workSceneAnimator = workSceneRoot.GetComponentInChildren<Animator>(true);
            if (workSceneAnimator != null)
            {
                workSceneAnimatorWasEnabled = workSceneAnimator.enabled;
                workSceneAnimator.enabled = false;
            }

            GameObject lightObject = new GameObject("Preview Directional Light");
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            SceneManager.MoveGameObjectToScene(lightObject, workScene);

            GameObject cameraObject = new GameObject("Animation VFX Preview Camera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            workSceneCamera = cameraObject.AddComponent<Camera>();
            workSceneCamera.clearFlags = CameraClearFlags.SolidColor;
            workSceneCamera.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            workSceneCamera.fieldOfView = 35f;
            workSceneCamera.nearClipPlane = 0.01f;
            workSceneCamera.farClipPlane = 1000f;
            workSceneCamera.enabled = false;
            SceneManager.MoveGameObjectToScene(cameraObject, workScene);

            SampleCurrentFrame(true);
            FrameWorkScenePreview();
            Repaint();
        }

        private void CloseWorkScene()
        {
            if (!IsWorkSceneOpen())
            {
                workSceneRoot = null;
                workSceneAnimator = null;
                workSceneAnimatorWasEnabled = false;
                selectedWorkObject = null;
                return;
            }

            EditorSceneManager.ClosePreviewScene(workScene);
            workSceneRoot = null;
            workSceneAnimator = null;
            workSceneAnimatorWasEnabled = false;
            workSceneCamera = null;
            selectedWorkObject = null;
            workScene = default;
            ReleaseScenePreviewTexture();
        }

        private void SaveWorkSceneRootAsPrefab()
        {
            if (!IsWorkSceneOpen() || workSceneRoot == null)
            {
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "Save VFX Work Prefab",
                workSceneRoot.name,
                "prefab",
                "Choose where to save the edited VFX timing prefab.");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (workSceneAnimator != null)
            {
                workSceneAnimator.enabled = workSceneAnimatorWasEnabled;
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(workSceneRoot, path);

            if (workSceneAnimator != null)
            {
                workSceneAnimator.enabled = false;
            }

            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
        }

        private void OpenPreviewRootCopyInPrefabMode()
        {
            if (previewRoot == null)
            {
                return;
            }

            StopSampling();
            CloseWorkScene();
            EnsureTempPreviewFolder();

            GameObject copy = Instantiate(previewRoot);
            copy.name = $"{previewRoot.name}_VFX_Work";

            string path = AssetDatabase.GenerateUniqueAssetPath(
                $"{TempPreviewFolder}/{SanitizeFileName(copy.name)}.prefab");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(copy, path);
            DestroyImmediate(copy);

            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Prefab Mode", "Failed to create the temporary preview prefab.", "OK");
                return;
            }

            AssetDatabase.OpenAsset(prefab);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        private static void EnsureTempPreviewFolder()
        {
            if (AssetDatabase.IsValidFolder(TempPreviewFolder))
            {
                return;
            }

            AssetDatabase.CreateFolder("Assets", "__AnimationVFXPreview");
        }

        private static string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return new string(fileName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        }

        private void HandleScenePreviewInput(Rect previewRect)
        {
            Event current = Event.current;
            if (!previewRect.Contains(current.mousePosition))
            {
                return;
            }

            if (current.type == EventType.MouseDrag && (current.alt || scenePreviewTool == 0))
            {
                HandleScenePreviewOrbitInput(current);
                return;
            }

            if (current.type == EventType.MouseDrag && (current.button == 1 || current.button == 2))
            {
                scenePreviewPan += GetPreviewPanDelta(current.delta);
                current.Use();
                Repaint();
                return;
            }

            if (current.type == EventType.ScrollWheel)
            {
                scenePreviewDistance = Mathf.Max(0.25f, scenePreviewDistance + current.delta.y * 0.15f);
                current.Use();
                Repaint();
                return;
            }

            HandleScenePreviewSelectionInput(previewRect, current);
        }

        private void HandleScenePreviewOrbitInput(Event current)
        {
            if (current.type != EventType.MouseDrag)
            {
                return;
            }

            if (current.button == 0)
            {
                scenePreviewOrbit.x = Mathf.Clamp(scenePreviewOrbit.x - current.delta.y * 0.4f, -85f, 85f);
                scenePreviewOrbit.y += current.delta.x * 0.4f;
                current.Use();
                Repaint();
                return;
            }
        }

        private void HandleScenePreviewSelectionInput(Rect previewRect, Event current)
        {
            if (current.type != EventType.MouseDown || current.button != 0 || GUIUtility.hotControl != 0)
            {
                return;
            }

            Ray ray = GetScenePreviewRay(previewRect, current.mousePosition);
            selectedWorkObject = PickWorkSceneObject(ray);
            current.Use();
            Repaint();
        }

        private void HandleScenePreviewShortcuts(Rect previewRect)
        {
            Event current = Event.current;
            if (current.type != EventType.KeyDown || !previewRect.Contains(current.mousePosition))
            {
                return;
            }

            switch (current.keyCode)
            {
                case KeyCode.Q:
                    scenePreviewTool = 0;
                    break;
                case KeyCode.W:
                    scenePreviewTool = 2;
                    break;
                case KeyCode.E:
                    scenePreviewTool = 3;
                    break;
                case KeyCode.R:
                    scenePreviewTool = 4;
                    break;
                case KeyCode.F:
                    FrameWorkScenePreview();
                    break;
                default:
                    return;
            }

            current.Use();
            Repaint();
        }

        private void RenderScenePreview(Rect previewRect)
        {
            if (workSceneCamera == null || workSceneRoot == null)
            {
                return;
            }

            int width = Mathf.Max(1, Mathf.RoundToInt(previewRect.width));
            int height = Mathf.Max(1, Mathf.RoundToInt(previewRect.height));
            EnsureScenePreviewTexture(width, height);
            UpdateWorkScenePreviewCamera();

            workSceneCamera.aspect = width / (float)height;
            workSceneCamera.targetTexture = scenePreviewTexture;
            workSceneCamera.Render();
            workSceneCamera.targetTexture = null;
        }

        private void DrawTransformHandle(Rect previewRect)
        {
            if (scenePreviewTool < 2 || selectedWorkObject == null || workSceneCamera == null)
            {
                return;
            }

            Handles.SetCamera(previewRect, workSceneCamera);
            Transform selectedTransform = selectedWorkObject.transform;
            EditorGUI.BeginChangeCheck();

            if (scenePreviewTool == 2)
            {
                Vector3 newPosition = Handles.PositionHandle(selectedTransform.position, selectedTransform.rotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(selectedTransform, "Move Preview Object");
                    selectedTransform.position = newPosition;
                    Repaint();
                }

                return;
            }

            if (scenePreviewTool == 3)
            {
                Quaternion newRotation = Handles.RotationHandle(selectedTransform.rotation, selectedTransform.position);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(selectedTransform, "Rotate Preview Object");
                    selectedTransform.rotation = newRotation;
                    Repaint();
                }

                return;
            }

            float handleSize = HandleUtility.GetHandleSize(selectedTransform.position);
            Vector3 newScale = Handles.ScaleHandle(
                selectedTransform.localScale,
                selectedTransform.position,
                selectedTransform.rotation,
                handleSize);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(selectedTransform, "Scale Preview Object");
                selectedTransform.localScale = newScale;
                Repaint();
            }
        }

        private void DrawPreviewGrid(Rect previewRect)
        {
            if (!drawPreviewGrid || workSceneCamera == null)
            {
                return;
            }

            Handles.SetCamera(previewRect, workSceneCamera);
            using (new Handles.DrawingScope(new Color(0.35f, 0.35f, 0.35f, 0.45f)))
            {
                const int halfLineCount = 10;
                for (int i = -halfLineCount; i <= halfLineCount; i++)
                {
                    Handles.DrawLine(new Vector3(i, 0f, -halfLineCount), new Vector3(i, 0f, halfLineCount));
                    Handles.DrawLine(new Vector3(-halfLineCount, 0f, i), new Vector3(halfLineCount, 0f, i));
                }
            }

            Handles.color = Color.red;
            Handles.DrawLine(Vector3.zero, Vector3.right * 2f);
            Handles.color = Color.green;
            Handles.DrawLine(Vector3.zero, Vector3.up * 2f);
            Handles.color = Color.blue;
            Handles.DrawLine(Vector3.zero, Vector3.forward * 2f);
        }

        private Ray GetScenePreviewRay(Rect previewRect, Vector2 guiPosition)
        {
            float textureWidth = scenePreviewTexture != null ? scenePreviewTexture.width : previewRect.width;
            float textureHeight = scenePreviewTexture != null ? scenePreviewTexture.height : previewRect.height;
            float x = Mathf.InverseLerp(previewRect.xMin, previewRect.xMax, guiPosition.x) * textureWidth;
            float y = Mathf.InverseLerp(previewRect.yMax, previewRect.yMin, guiPosition.y) * textureHeight;
            return workSceneCamera.ScreenPointToRay(new Vector3(x, y, 0f));
        }

        private Vector3 GetPreviewPanDelta(Vector2 mouseDelta)
        {
            if (workSceneCamera == null)
            {
                return Vector3.zero;
            }

            float scale = Mathf.Max(scenePreviewDistance, 0.1f) * 0.0025f;
            Vector3 right = workSceneCamera.transform.right;
            Vector3 up = workSceneCamera.transform.up;
            return (-right * mouseDelta.x + up * mouseDelta.y) * scale;
        }

        private GameObject PickWorkSceneObject(Ray ray)
        {
            Renderer[] renderers = workSceneRoot.GetComponentsInChildren<Renderer>(true);
            GameObject closestObject = null;
            float closestDistance = float.PositiveInfinity;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (renderer.bounds.IntersectRay(ray, out float distance) && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = renderer.gameObject;
                }
            }

            if (closestObject != null)
            {
                return closestObject;
            }

            Transform[] transforms = workSceneRoot.GetComponentsInChildren<Transform>(true);
            foreach (Transform target in transforms)
            {
                if (target == workSceneRoot.transform && transforms.Length > 1)
                {
                    continue;
                }

                Bounds pickBounds = new Bounds(target.position, Vector3.one * 0.35f);
                if (pickBounds.IntersectRay(ray, out float distance) && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = target.gameObject;
                }
            }

            return closestObject;
        }

        private void DrawScenePreviewSelectionOverlay(Rect previewRect)
        {
            if (selectedWorkObject == null || workSceneCamera == null)
            {
                return;
            }

            Vector3 viewportPoint = workSceneCamera.WorldToViewportPoint(selectedWorkObject.transform.position);
            if (viewportPoint.z <= 0f)
            {
                return;
            }

            float x = previewRect.xMin + viewportPoint.x * previewRect.width;
            float y = previewRect.yMax - viewportPoint.y * previewRect.height;
            Rect labelRect = new Rect(x - 70f, y - 28f, 140f, 22f);

            EditorGUI.DrawRect(labelRect, new Color(0.05f, 0.05f, 0.05f, 0.75f));
            GUI.Label(labelRect, selectedWorkObject.name, EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawSelectedObjectControls()
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Selected", selectedWorkObject, typeof(GameObject), true);
            }

            if (selectedWorkObject == null)
            {
                return;
            }

            if (GUILayout.Button("Clear Selection"))
            {
                selectedWorkObject = null;
                return;
            }

            EditorGUI.BeginChangeCheck();
            Vector3 positionValue = EditorGUILayout.Vector3Field("Position", selectedWorkObject.transform.position);
            Vector3 rotationValue = EditorGUILayout.Vector3Field("Rotation", selectedWorkObject.transform.eulerAngles);
            Vector3 scaleValue = EditorGUILayout.Vector3Field("Scale", selectedWorkObject.transform.localScale);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(selectedWorkObject.transform, "Edit Preview Object Transform");
                selectedWorkObject.transform.position = positionValue;
                selectedWorkObject.transform.rotation = Quaternion.Euler(rotationValue);
                selectedWorkObject.transform.localScale = scaleValue;
                Repaint();
            }
        }

        private void EnsureScenePreviewTexture(int width, int height)
        {
            if (scenePreviewTexture != null &&
                scenePreviewTexture.width == width &&
                scenePreviewTexture.height == height)
            {
                return;
            }

            ReleaseScenePreviewTexture();
            scenePreviewTexture = new RenderTexture(width, height, 24)
            {
                name = "Animation VFX Scene Preview",
                hideFlags = HideFlags.HideAndDontSave
            };
            scenePreviewTexture.Create();
        }

        private void ReleaseScenePreviewTexture()
        {
            if (scenePreviewTexture == null)
            {
                return;
            }

            scenePreviewTexture.Release();
            DestroyImmediate(scenePreviewTexture);
            scenePreviewTexture = null;
        }

        private void FrameWorkScenePreview()
        {
            Bounds bounds = CalculateWorkSceneBounds();
            float radius = Mathf.Max(bounds.extents.magnitude, 0.5f);
            scenePreviewPan = Vector3.zero;
            scenePreviewDistance = Mathf.Max(radius * 2.4f, 1f);
            UpdateWorkScenePreviewCamera();
            Repaint();
        }

        private void UpdateWorkScenePreviewCamera()
        {
            if (workSceneCamera == null || workSceneRoot == null)
            {
                return;
            }

            Bounds bounds = CalculateWorkSceneBounds();
            Quaternion rotation = Quaternion.Euler(scenePreviewOrbit.x, scenePreviewOrbit.y, 0f);
            Vector3 focusPoint = bounds.center + scenePreviewPan;
            Vector3 cameraPosition = focusPoint + rotation * new Vector3(0f, 0f, -scenePreviewDistance);

            workSceneCamera.transform.SetPositionAndRotation(cameraPosition, rotation);
            workSceneCamera.transform.LookAt(focusPoint);
        }

        private Bounds CalculateWorkSceneBounds()
        {
            if (workSceneRoot == null)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            Renderer[] renderers = workSceneRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(workSceneRoot.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }
    }
}
