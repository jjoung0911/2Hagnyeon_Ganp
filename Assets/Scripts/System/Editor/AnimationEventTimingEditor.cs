using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ganp.EditorTools
{
    // 애니메이션 이벤트(AttackStart, AttackEnd, DrawEnd 등)의 타이밍을
    // 클립을 프레임 단위로 스크럽하며 직접 보고 추가/수정/삭제하는 에디터 창.
    // Animation 창에서 일일이 찾아 조정하는 대신 미리보기와 이벤트 편집을 한 곳에서 처리한다.
    public class AnimationEventTimingEditor : EditorWindow
    {
        // IAnimationTrigger(AgentRenderer)에 정의된 함수 — 새 이벤트 추가 시 드롭다운으로 제시
        private static readonly string[] KnownTriggerFunctions =
        {
            "EndTrigger", "Attack", "AttackStart", "AttackEnd", "FootStep"
        };

        private GameObject previewRoot;
        private Animator previewAnimator;
        private RuntimeAnimatorController animatorController;
        private readonly List<AnimationClip> controllerClips = new List<AnimationClip>();
        private int selectedClipIndex = -1;
        private AnimationClip animationClip;

        private int frame;
        private bool sampleAutomatically = true;

        private List<AnimationEvent> events = new List<AnimationEvent>();
        private bool eventsDirty;

        private Vector2 eventListScroll;
        private Vector2 clipListScroll;
        private int newEventFunctionIndex;
        private bool useCustomFunctionName;
        private string customFunctionName = "";

        [MenuItem("Tools/애니메이션 이벤트 타이밍 에디터")]
        private static void Open()
        {
            var window = GetWindow<AnimationEventTimingEditor>();
            window.titleContent = new GUIContent("Anim Event Timing");
            window.minSize = new Vector2(440f, 520f);
            window.Show();

            if (window.previewRoot == null && Selection.activeGameObject != null)
            {
                window.previewRoot = Selection.activeGameObject;
                window.LoadAnimatorClipsFromPreviewRoot();
            }
        }

        private void OnDisable() => StopSampling();

        private void OnGUI()
        {
            EditorGUILayout.LabelField("대상 오브젝트", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            previewRoot = (GameObject)EditorGUILayout.ObjectField("Preview Root", previewRoot, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                StopSampling();
                LoadAnimatorClipsFromPreviewRoot();
            }

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Animator", previewAnimator, typeof(Animator), true);

            if (GUILayout.Button("클립 목록 새로고침"))
                LoadAnimatorClipsFromPreviewRoot();

            if (previewRoot != null && previewAnimator == null)
                EditorGUILayout.HelpBox("선택한 오브젝트(혹은 자식)에서 Animator를 찾지 못했습니다.", MessageType.Warning);

            EditorGUILayout.Space(6f);
            DrawClipList();

            EditorGUILayout.Space(4f);
            EditorGUI.BeginChangeCheck();
            var manualClip = (AnimationClip)EditorGUILayout.ObjectField("클립 직접 지정", animationClip, typeof(AnimationClip), false);
            if (EditorGUI.EndChangeCheck())
                SelectClip(manualClip);

            if (animationClip == null)
            {
                EditorGUILayout.HelpBox("타이밍을 잡을 애니메이션 클립을 선택하세요.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8f);
            DrawScrubControls();

            EditorGUILayout.Space(8f);
            DrawReadOnlyWarning();

            EditorGUILayout.Space(4f);
            DrawEventList();

            EditorGUILayout.Space(6f);
            DrawAddEventControls();

            EditorGUILayout.Space(6f);
            DrawSaveControls();
        }

        private void DrawClipList()
        {
            EditorGUILayout.LabelField($"컨트롤러 클립 ({controllerClips.Count})", EditorStyles.boldLabel);
            clipListScroll = EditorGUILayout.BeginScrollView(clipListScroll, GUILayout.MaxHeight(140f));
            for (int i = 0; i < controllerClips.Count; i++)
            {
                AnimationClip clip = controllerClips[i];
                bool isSelected = i == selectedClipIndex;
                string label = $"{clip.name}  ({clip.length:0.00}s)";
                if (GUILayout.Toggle(isSelected, label, "Button") && !isSelected)
                    SelectClip(clip);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawScrubControls()
        {
            EditorGUILayout.LabelField("프레임 스크럽 / 미리보기", EditorStyles.boldLabel);
            sampleAutomatically = EditorGUILayout.Toggle("스크럽 시 자동 샘플링", sampleAutomatically);

            int maxFrame = GetMaxFrame();
            float frameRate = GetFrameRate();

            EditorGUI.BeginChangeCheck();
            frame = EditorGUILayout.IntSlider("프레임", frame, 0, maxFrame);
            if (EditorGUI.EndChangeCheck())
            {
                ClampFrame();
                if (sampleAutomatically) SampleCurrentFrame();
            }

            EditorGUILayout.LabelField("시간",
                $"{GetCurrentTime():0.000}s / {animationClip.length:0.000}s   ({frameRate:0.#} fps)");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("◀◀ 처음")) SetFrame(0);
                if (GUILayout.Button("◀ 이전 프레임")) SetFrame(frame - 1);
                if (GUILayout.Button("샘플링")) SampleCurrentFrame();
                if (GUILayout.Button("다음 프레임 ▶")) SetFrame(frame + 1);
                if (GUILayout.Button("끝 ▶▶")) SetFrame(maxFrame);
            }

            using (new EditorGUI.DisabledScope(!AnimationMode.InAnimationMode()))
            {
                if (GUILayout.Button("미리보기 모드 종료"))
                    StopSampling();
            }
        }

        private void DrawReadOnlyWarning()
        {
            if (!IsClipReadOnly(animationClip)) return;

            EditorGUILayout.HelpBox(
                "이 클립은 임포트된 모델의 하위 에셋이라 이벤트를 직접 저장할 수 없습니다. " +
                "편집 가능한 복제본을 만들어 사용하세요.",
                MessageType.Warning);

            if (GUILayout.Button("편집 가능한 복제본 생성"))
                CreateEditableCopy();
        }

        private void DrawEventList()
        {
            EditorGUILayout.LabelField($"애니메이션 이벤트 ({events.Count})", EditorStyles.boldLabel);

            if (events.Count == 0)
            {
                EditorGUILayout.HelpBox("등록된 이벤트가 없습니다.", MessageType.Info);
                return;
            }

            eventListScroll = EditorGUILayout.BeginScrollView(eventListScroll, GUILayout.MaxHeight(260f));

            for (int i = 0; i < events.Count; i++)
            {
                AnimationEvent evt = events[i];
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        float normalized = animationClip.length > 0f ? evt.time / animationClip.length : 0f;
                        normalized = EditorGUILayout.Slider($"#{i}  {evt.functionName}", normalized, 0f, 1f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            evt.time = Mathf.Clamp(normalized * animationClip.length, 0f, animationClip.length);
                            eventsDirty = true;
                        }

                        if (GUILayout.Button("이동", GUILayout.Width(48f)))
                            JumpToTime(evt.time);

                        if (GUILayout.Button("삭제", GUILayout.Width(48f)))
                        {
                            events.RemoveAt(i);
                            eventsDirty = true;
                            GUIUtility.ExitGUI();
                        }
                    }

                    EditorGUI.BeginChangeCheck();
                    evt.time = EditorGUILayout.FloatField("시간(초)", evt.time);
                    evt.functionName = EditorGUILayout.TextField("함수명", evt.functionName);
                    evt.floatParameter = EditorGUILayout.FloatField("Float 매개변수", evt.floatParameter);
                    evt.intParameter = EditorGUILayout.IntField("Int 매개변수", evt.intParameter);
                    evt.stringParameter = EditorGUILayout.TextField("String 매개변수", evt.stringParameter);
                    if (EditorGUI.EndChangeCheck())
                    {
                        evt.time = Mathf.Clamp(evt.time, 0f, animationClip.length);
                        eventsDirty = true;
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawAddEventControls()
        {
            EditorGUILayout.LabelField("현재 프레임 위치에 이벤트 추가", EditorStyles.boldLabel);

            useCustomFunctionName = EditorGUILayout.Toggle("직접 함수명 입력", useCustomFunctionName);
            string functionName;
            if (useCustomFunctionName)
            {
                customFunctionName = EditorGUILayout.TextField("함수명", customFunctionName);
                functionName = customFunctionName;
            }
            else
            {
                newEventFunctionIndex = EditorGUILayout.Popup("함수 (IAnimationTrigger)", newEventFunctionIndex, KnownTriggerFunctions);
                functionName = KnownTriggerFunctions[newEventFunctionIndex];
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(functionName)))
            {
                if (GUILayout.Button($"프레임 {frame} ({GetCurrentTime():0.000}s) 위치에 \"{functionName}\" 추가", GUILayout.Height(28f)))
                {
                    var newEvent = new AnimationEvent
                    {
                        time = GetCurrentTime(),
                        functionName = functionName
                    };
                    events.Add(newEvent);
                    events = events.OrderBy(e => e.time).ToList();
                    eventsDirty = true;
                }
            }
        }

        private void DrawSaveControls()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!eventsDirty || IsClipReadOnly(animationClip)))
                {
                    if (GUILayout.Button("클립에 저장", GUILayout.Height(30f)))
                        SaveEvents();
                }

                if (GUILayout.Button("클립에서 다시 불러오기", GUILayout.Height(30f)))
                    LoadEventsFromClip();
            }

            if (eventsDirty)
                EditorGUILayout.HelpBox("저장하지 않은 변경사항이 있습니다.", MessageType.Warning);
        }

        // ----- 데이터 로딩/선택 -----

        private void LoadAnimatorClipsFromPreviewRoot()
        {
            previewAnimator = previewRoot != null ? previewRoot.GetComponentInChildren<Animator>(true) : null;
            animatorController = previewAnimator != null ? previewAnimator.runtimeAnimatorController : null;

            controllerClips.Clear();
            if (animatorController != null)
            {
                controllerClips.AddRange(animatorController.animationClips
                    .Where(c => c != null)
                    .GroupBy(c => c.GetInstanceID())
                    .Select(g => g.First())
                    .OrderBy(c => c.name));
            }

            if (controllerClips.Count == 0)
            {
                selectedClipIndex = -1;
                SelectClip(null);
                return;
            }

            int existingIndex = animationClip != null ? controllerClips.IndexOf(animationClip) : -1;
            SelectClip(controllerClips[existingIndex >= 0 ? existingIndex : 0]);
        }

        private void SelectClip(AnimationClip clip)
        {
            StopSampling();
            animationClip = clip;
            selectedClipIndex = clip != null ? controllerClips.IndexOf(clip) : -1;
            frame = 0;
            ClampFrame();
            LoadEventsFromClip();
            if (clip != null && sampleAutomatically) SampleCurrentFrame();
            Repaint();
        }

        private void LoadEventsFromClip()
        {
            events = animationClip != null
                ? AnimationUtility.GetAnimationEvents(animationClip).Select(CloneEvent).ToList()
                : new List<AnimationEvent>();
            eventsDirty = false;
        }

        private static AnimationEvent CloneEvent(AnimationEvent source) => new AnimationEvent
        {
            time = source.time,
            functionName = source.functionName,
            floatParameter = source.floatParameter,
            intParameter = source.intParameter,
            stringParameter = source.stringParameter,
            objectReferenceParameter = source.objectReferenceParameter,
            messageOptions = source.messageOptions
        };

        private void SaveEvents()
        {
            if (animationClip == null || IsClipReadOnly(animationClip)) return;

            events = events.OrderBy(e => e.time).ToList();
            Undo.RecordObject(animationClip, "Edit Animation Events");
            AnimationUtility.SetAnimationEvents(animationClip, events.ToArray());
            EditorUtility.SetDirty(animationClip);
            AssetDatabase.SaveAssets();
            eventsDirty = false;
        }

        private void CreateEditableCopy()
        {
            if (animationClip == null) return;

            string sourcePath = AssetDatabase.GetAssetPath(animationClip);
            string folder = string.IsNullOrEmpty(sourcePath) ? "Assets" : System.IO.Path.GetDirectoryName(sourcePath);
            string targetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{animationClip.name}_Copy.anim");

            var copy = Instantiate(animationClip);
            AssetDatabase.CreateAsset(copy, targetPath);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("복제본 생성 완료",
                $"편집 가능한 복제본을 생성했습니다:\n{targetPath}\n\n" +
                "Animator Controller에서 원본 클립을 이 복제본으로 교체한 뒤 다시 선택해 주세요.",
                "확인");

            Selection.activeObject = copy;
            EditorGUIUtility.PingObject(copy);
        }

        // ----- 프레임/시간 -----

        private void SetFrame(int newFrame)
        {
            frame = Mathf.Clamp(newFrame, 0, GetMaxFrame());
            if (sampleAutomatically) SampleCurrentFrame();
            Repaint();
        }

        private void ClampFrame() => frame = Mathf.Clamp(frame, 0, GetMaxFrame());

        private int GetMaxFrame() =>
            animationClip == null ? 0 : Mathf.Max(0, Mathf.RoundToInt(animationClip.length * GetFrameRate()));

        private float GetFrameRate() =>
            animationClip == null || animationClip.frameRate <= 0f ? 60f : animationClip.frameRate;

        private float GetCurrentTime() =>
            animationClip == null ? 0f : Mathf.Clamp(frame / GetFrameRate(), 0f, animationClip.length);

        private void JumpToTime(float time)
        {
            frame = Mathf.RoundToInt(Mathf.Clamp(time, 0f, animationClip.length) * GetFrameRate());
            ClampFrame();
            if (sampleAutomatically) SampleCurrentFrame();
            Repaint();
        }

        // ----- 샘플링/미리보기 -----

        private void SampleCurrentFrame()
        {
            if (animationClip == null || previewRoot == null) return;

            if (!AnimationMode.InAnimationMode())
                AnimationMode.StartAnimationMode();

            AnimationMode.BeginSampling();
            GameObject animationRoot = previewAnimator != null ? previewAnimator.gameObject : previewRoot;
            AnimationMode.SampleAnimationClip(animationRoot, animationClip, GetCurrentTime());
            AnimationMode.EndSampling();
            SceneView.RepaintAll();
        }

        private void StopSampling()
        {
            if (AnimationMode.InAnimationMode())
            {
                AnimationMode.StopAnimationMode();
                SceneView.RepaintAll();
            }
        }

        private static bool IsClipReadOnly(AnimationClip clip) =>
            clip != null && (clip.hideFlags & HideFlags.NotEditable) != 0;
    }
}
