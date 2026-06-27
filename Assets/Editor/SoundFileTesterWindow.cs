using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GameEditor.Sound
{
    // 프로젝트의 오디오 파일(AudioClip)을 목록으로 보고 에디터에서 바로 미리듣는 창.
    // - 화살표 키(↑/↓)로 목록 이동, 이동 시 자동 재생(토글)
    // - Enter/Space 재생, Esc 정지
    // - 폴더 경로/이름 필터로 범위 좁히기
    public class SoundFileTesterWindow : EditorWindow
    {
        [MenuItem("Tools/사운드 파일 테스터")]
        private static void Open()
        {
            var win = GetWindow<SoundFileTesterWindow>("사운드 파일 테스터");
            win.minSize = new Vector2(380, 300);
            win.Refresh();
        }

        // 검색 범위 폴더 (비우면 전체 Assets)
        [SerializeField] private string _folder = "Assets/_Sounds";
        [SerializeField] private string _filter = "";
        [SerializeField] private bool _autoPlay = true;

        private readonly List<AudioClip> _clips = new();
        private readonly List<string> _paths = new();
        private int _selected = -1;
        private Vector2 _scroll;

        private const float RowHeight = 20f;

        private void OnEnable() => Refresh();

        private void Refresh()
        {
            _clips.Clear();
            _paths.Clear();

            string[] searchFolders = AssetDatabase.IsValidFolder(_folder)
                ? new[] { _folder }
                : null; // null = 전체 검색

            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", searchFolders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) continue;
                if (!string.IsNullOrEmpty(_filter) &&
                    clip.name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                _clips.Add(clip);
                _paths.Add(path);
            }

            // 이름순 정렬 (paths도 같이)
            var order = Enumerable.Range(0, _clips.Count)
                .OrderBy(i => _clips[i].name, StringComparer.OrdinalIgnoreCase).ToList();
            var c = order.Select(i => _clips[i]).ToList();
            var p = order.Select(i => _paths[i]).ToList();
            _clips.Clear(); _clips.AddRange(c);
            _paths.Clear(); _paths.AddRange(p);

            _selected = _clips.Count > 0 ? Mathf.Clamp(_selected, 0, _clips.Count - 1) : -1;
        }

        private void OnGUI()
        {
            HandleKeyboard();
            DrawToolbar();
            DrawList();
            DrawDetail();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                _folder = EditorGUILayout.TextField(_folder, EditorStyles.toolbarTextField, GUILayout.Width(160));
                GUILayout.Label("필터", GUILayout.Width(28));
                _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarSearchField, GUILayout.MinWidth(80));
                if (EditorGUI.EndChangeCheck()) Refresh();

                if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    Refresh();

                _autoPlay = GUILayout.Toggle(_autoPlay, "자동재생", EditorStyles.toolbarButton, GUILayout.Width(60));

                if (GUILayout.Button("■ 정지", EditorStyles.toolbarButton, GUILayout.Width(50)))
                    AudioPreview.StopAll();
            }
        }

        private void DrawList()
        {
            if (_clips.Count == 0)
            {
                EditorGUILayout.HelpBox("오디오 파일이 없습니다. 폴더 경로/필터를 확인하세요.", MessageType.Info);
                GUILayout.FlexibleSpace();
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _clips.Count; i++)
            {
                var rect = EditorGUILayout.GetControlRect(false, RowHeight);

                if (i == _selected)
                    EditorGUI.DrawRect(rect, new Color(0.24f, 0.48f, 0.90f, 0.35f));

                // 클릭 선택 + 재생
                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    Select(i, play: true);
                    Event.current.Use();
                }

                var numRect = new Rect(rect.x + 4, rect.y, 30, rect.height);
                var nameRect = new Rect(rect.x + 34, rect.y, rect.width - 120, rect.height);
                var lenRect = new Rect(rect.xMax - 80, rect.y, 76, rect.height);

                GUI.Label(numRect, (i + 1).ToString(), EditorStyles.miniLabel);
                GUI.Label(nameRect, _clips[i].name);
                GUI.Label(lenRect, $"{_clips[i].length * 1000f:0}ms", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawDetail()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_selected < 0 || _selected >= _clips.Count)
                {
                    GUILayout.Label("선택된 파일 없음", EditorStyles.miniLabel);
                    return;
                }

                var clip = _clips[_selected];
                EditorGUILayout.LabelField(clip.name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(_paths[_selected], EditorStyles.miniLabel);
                EditorGUILayout.LabelField(
                    $"{clip.length * 1000f:0}ms · {clip.frequency}Hz · {clip.channels}ch · {clip.samples} samples",
                    EditorStyles.miniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("▶ 재생 (Enter)")) PlaySelected();
                    if (GUILayout.Button("■ 정지 (Esc)")) AudioPreview.StopAll();
                    if (GUILayout.Button("프로젝트에서 선택"))
                        EditorGUIUtility.PingObject(clip);
                }
                GUILayout.Label("↑/↓ 이동 · Enter 재생 · Esc 정지", EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void HandleKeyboard()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown || _clips.Count == 0) return;

            switch (e.keyCode)
            {
                case KeyCode.DownArrow:
                    Select(Mathf.Min(_selected + 1, _clips.Count - 1), _autoPlay);
                    e.Use();
                    break;
                case KeyCode.UpArrow:
                    Select(Mathf.Max(_selected - 1, 0), _autoPlay);
                    e.Use();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                case KeyCode.Space:
                    PlaySelected();
                    e.Use();
                    break;
                case KeyCode.Escape:
                    AudioPreview.StopAll();
                    e.Use();
                    break;
            }
        }

        private void Select(int index, bool play)
        {
            if (index < 0 || index >= _clips.Count) return;
            _selected = index;
            EnsureVisible(index);
            if (play) PlaySelected();
            Repaint();
        }

        private void PlaySelected()
        {
            if (_selected < 0 || _selected >= _clips.Count) return;
            AudioPreview.StopAll();
            AudioPreview.Play(_clips[_selected]);
        }

        // 선택 항목이 스크롤 영역 안에 보이도록 조정
        private void EnsureVisible(int index)
        {
            float y = index * RowHeight;
            float viewH = position.height - 120f; // 툴바/디테일 여백 대략치
            if (y < _scroll.y) _scroll.y = y;
            else if (y + RowHeight > _scroll.y + viewH) _scroll.y = y + RowHeight - viewH;
        }
    }

    // UnityEditor.AudioUtil 리플렉션 래퍼 — 에디터에서 AudioClip 미리듣기 (버전별 메서드명 대응)
    internal static class AudioPreview
    {
        private static readonly MethodInfo _play;
        private static readonly MethodInfo _stopAll;

        static AudioPreview()
        {
            var asm = typeof(AudioImporter).Assembly;
            var t = asm.GetType("UnityEditor.AudioUtil");
            if (t == null) return;

            _play = t.GetMethod("PlayPreviewClip",
                        new[] { typeof(AudioClip), typeof(int), typeof(bool) })
                 ?? t.GetMethod("PlayClip",
                        new[] { typeof(AudioClip), typeof(int), typeof(bool) })
                 ?? t.GetMethod("PlayPreviewClip", new[] { typeof(AudioClip) })
                 ?? t.GetMethod("PlayClip", new[] { typeof(AudioClip) });

            _stopAll = t.GetMethod("StopAllPreviewClips")
                    ?? t.GetMethod("StopAllClips");
        }

        public static void Play(AudioClip clip, bool loop = false)
        {
            if (clip == null || _play == null) return;
            if (_play.GetParameters().Length == 3)
                _play.Invoke(null, new object[] { clip, 0, loop });
            else
                _play.Invoke(null, new object[] { clip });
        }

        public static void StopAll() => _stopAll?.Invoke(null, null);
    }
}
