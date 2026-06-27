using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace System.Editor
{
    public class MapNightLightingEditor : EditorWindow
    {
        private const string MapScenePath   = "Assets/Scenes/Map.unity";
        private const string LightGroupName = "== Baked Lights ==";

        private bool  _highQualityBake     = true;
        private float _intensityMultiplier = 2.0f;
        private float _rangeMultiplier     = 2.0f;
        private bool  _adjustGroupOnly     = true;

        // 절대값 설정
        private float _targetIntensity = 10f;
        private float _targetRange     = 50f;
        private bool  _absGroupOnly    = true;

        [MenuItem("Tools/Map 야간 라이팅 설정")]
        public static void ShowWindow() =>
            GetWindow<MapNightLightingEditor>("Map 야간 라이팅");

        // ─────────────────────────────────────────────────────
        //  GUI
        // ─────────────────────────────────────────────────────
        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Map 씬 — 야간 베이킹 라이팅", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "배치 후 Window > Rendering > Lighting 에서 Generate Lighting 실행",
                MessageType.Info);

            EditorGUILayout.Space(6);
            _highQualityBake = EditorGUILayout.Toggle("고품질 베이킹 (느림)", _highQualityBake);
            EditorGUILayout.Space(6);

            // ── 단계별 실행 ──
            EditorGUILayout.LabelField("단계별 실행", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("① 달빛 + 환경 설정", GUILayout.Height(28)))
                ApplyMoonAndEnvironment();
            if (GUILayout.Button("② 베이킹용 라이트 전체 배치", GUILayout.Height(34)))
                PlaceAllBakedLights();
            if (GUILayout.Button("③ 베이킹 설정 + Global Volume", GUILayout.Height(28)))
                ApplyBakeSettingsAndVolume();

            EditorGUILayout.Space(8);

            // ── 라이트 일괄 조정 ──
            EditorGUILayout.LabelField("라이트 일괄 조정 — 배율", EditorStyles.miniBoldLabel);
            _intensityMultiplier = EditorGUILayout.Slider("Intensity 배율", _intensityMultiplier, 0.1f, 10f);
            _rangeMultiplier     = EditorGUILayout.Slider("Range 배율",     _rangeMultiplier,     0.1f, 10f);
            _adjustGroupOnly     = EditorGUILayout.Toggle("배치 그룹만 (OFF=씬 전체)", _adjustGroupOnly);
            EditorGUILayout.Space(3);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("배율 적용", GUILayout.Height(28)))
                    ScaleLights(_intensityMultiplier, _rangeMultiplier, _adjustGroupOnly);
                if (GUILayout.Button("역배율 복원", GUILayout.Height(28)))
                    ScaleLights(1f / _intensityMultiplier, 1f / _rangeMultiplier, _adjustGroupOnly);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("라이트 일괄 조정 — 절대값", EditorStyles.miniBoldLabel);
            _targetIntensity = EditorGUILayout.FloatField("목표 Intensity", _targetIntensity);
            _targetRange     = EditorGUILayout.FloatField("목표 Range",     _targetRange);
            _absGroupOnly    = EditorGUILayout.Toggle("배치 그룹만 (OFF=씬 전체)", _absGroupOnly);
            EditorGUILayout.Space(3);
            if (GUILayout.Button("절대값으로 설정", GUILayout.Height(28)))
                SetLightsAbsolute(_targetIntensity, _targetRange, _absGroupOnly);

            EditorGUILayout.Space(8);

            // ── 유틸리티 ──
            EditorGUILayout.LabelField("유틸리티", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("배치 라이트 제거"))
                    RemoveLightGroup();
                if (GUILayout.Button("①②③ 한번에"))
                    RunAll();
            }

            EditorGUILayout.Space(6);

            // ── 진단 / 베이킹 준비 ──
            EditorGUILayout.LabelField("진단 / 베이킹 준비", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(
                "④ 프리팹 자식이 Non-Static이면 베이킹이 안 됨. 반드시 실행.",
                MessageType.Warning);
            if (GUILayout.Button("④ 씬 전체 ContributeGI 설정 (필수)", GUILayout.Height(34)))
                SetAllRenderersForBaking();
            if (GUILayout.Button("진단: 베이킹 대상 수 확인"))
                DiagnoseBakingReadiness();
            if (GUILayout.Button("기존 라이트맵 초기화"))
                ClearLightmapData();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("④ → ③ → Ctrl+S → Generate Lighting",
                EditorStyles.centeredGreyMiniLabel);
        }

        private void RunAll()
        {
            ApplyMoonAndEnvironment();
            PlaceAllBakedLights();
            ApplyBakeSettingsAndVolume();
        }

        // ═════════════════════════════════════════════════════
        //  ① 달빛 + RenderSettings
        // ═════════════════════════════════════════════════════
        private static void ApplyMoonAndEnvironment()
        {
            if (!EnsureMapScene()) return;

            RenderSettings.skybox          = null;
            RenderSettings.ambientMode     = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = new Color(0.05f, 0.07f, 0.18f);
            RenderSettings.ambientEquatorColor = new Color(0.07f, 0.10f, 0.20f);
            RenderSettings.ambientGroundColor  = new Color(0.02f, 0.03f, 0.07f);
            RenderSettings.ambientIntensity    = 1f;
            RenderSettings.fog        = true;
            RenderSettings.fogMode    = FogMode.Exponential;
            RenderSettings.fogColor   = new Color(0.04f, 0.06f, 0.14f);
            RenderSettings.fogDensity = 0.008f;

            Light moon = FindOrCreateDirectionalLight();
            Undo.RecordObject(moon, "Setup Moonlight");
            Undo.RecordObject(moon.transform, "Setup Moonlight Transform");
            moon.gameObject.name    = "Moonlight";
            moon.color              = new Color(0.75f, 0.85f, 1.0f);
            moon.intensity          = 0.60f;
            moon.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            moon.shadows            = LightShadows.Soft;
            moon.shadowStrength     = 0.70f;
            moon.shadowBias         = 0.05f;
            moon.lightmapBakeType   = LightmapBakeType.Mixed;
            RenderSettings.sun      = moon;

            MarkDirty();
            Debug.Log("[MapNightLighting] ① 달빛·환경 설정 완료");
        }

        // ═════════════════════════════════════════════════════
        //  ② 베이킹 라이트 배치
        //  값 기준: 대형 도시 맵 (400×800 유닛), 야간 분위기
        // ═════════════════════════════════════════════════════
        private static void PlaceAllBakedLights()
        {
            if (!EnsureMapScene()) return;

            RemoveLightGroup();
            var root = new GameObject(LightGroupName);
            Undo.RegisterCreatedObjectUndo(root, "Create Light Group");

            PlaceGateLights(root);
            PlaceCitadelLights(root);
            PlacePortLights(root);
            PlaceCityCoreLights(root);
            PlaceMainStreetLights(root);
            PlaceWallTorches(root);

            MarkDirty();
            Debug.Log($"[MapNightLighting] ② 라이트 {root.transform.childCount}개 배치 완료");
        }

        // ── 성문 ─────────────────────────────────────────────
        // 북문 (-51, 0, 268) / 남문 (-147, 0, -111)
        private static void PlaceGateLights(GameObject root)
        {
            var amber = new Color(1.0f, 0.72f, 0.30f);
            var cool  = new Color(0.80f, 0.90f, 1.0f);

            // 북문
            CreateBakedPoint(root, "Gate_North_L",   new Vector3(-62f, 8f,  268f), amber, 12f, 40f);
            CreateBakedPoint(root, "Gate_North_R",   new Vector3(-41f, 8f,  268f), amber, 12f, 40f);
            CreateBakedSpot (root, "Gate_North_Top", new Vector3(-51f, 20f, 272f),
                Quaternion.Euler(80f, 0f, 0f), cool, 8f, 50f, 60f);

            // 남문
            CreateBakedPoint(root, "Gate_South_L",   new Vector3(-156f, 8f, -111f), amber, 12f, 40f);
            CreateBakedPoint(root, "Gate_South_R",   new Vector3(-139f, 8f, -111f), amber, 12f, 40f);
            CreateBakedSpot (root, "Gate_South_Top", new Vector3(-147f, 20f, -107f),
                Quaternion.Euler(80f, 0f, 0f), cool, 8f, 50f, 60f);
        }

        // ── 성채 ─────────────────────────────────────────────
        // citadel_gate (14.6, 0, -205)
        private static void PlaceCitadelLights(GameObject root)
        {
            var blue  = new Color(0.65f, 0.80f, 1.0f);
            var amber = new Color(1.0f,  0.68f, 0.25f);

            CreateBakedPoint(root, "Citadel_Gate_L",   new Vector3( 4f, 11f, -205f), amber, 14f, 45f);
            CreateBakedPoint(root, "Citadel_Gate_R",   new Vector3(25f, 11f, -205f), amber, 14f, 45f);
            CreateBakedSpot (root, "Citadel_Top",      new Vector3(14f, 35f, -198f),
                Quaternion.Euler(70f, 180f, 0f), blue, 15f, 60f, 50f);
            CreateBakedPoint(root, "Citadel_Inner",    new Vector3(14f,  8f, -222f), blue,  8f, 55f);
            CreateBakedPoint(root, "Citadel_Tower_L",  new Vector3(-7f, 16f, -217f), amber, 10f, 40f);
            CreateBakedPoint(root, "Citadel_Tower_R",  new Vector3(35f, 16f, -217f), amber, 10f, 40f);
        }

        // ── 항구 ─────────────────────────────────────────────
        // Port (192.8, 0, 186)
        private static void PlacePortLights(GameObject root)
        {
            var warm   = new Color(1.0f, 0.70f, 0.35f);
            var orange = new Color(1.0f, 0.55f, 0.20f);

            float[,] dock =
            {
                { 173f, 7f, 175f }, { 193f, 7f, 175f }, { 213f, 7f, 175f },
                { 173f, 7f, 197f }, { 193f, 7f, 197f }, { 213f, 7f, 197f },
            };
            for (int i = 0; i < 6; i++)
                CreateBakedPoint(root, $"Port_Dock_{i}",
                    new Vector3(dock[i, 0], dock[i, 1], dock[i, 2]),
                    i < 3 ? orange : warm, 12f, 55f);

            CreateBakedSpot(root, "Port_Entry_L", new Vector3(181f, 18f, 168f),
                Quaternion.Euler(65f,  25f, 0f), warm, 14f, 60f, 55f);
            CreateBakedSpot(root, "Port_Entry_R", new Vector3(205f, 18f, 168f),
                Quaternion.Euler(65f, -25f, 0f), warm, 14f, 60f, 55f);
            CreateBakedPoint(root, "Port_End",    new Vector3(193f,  5f, 218f),
                new Color(1.0f, 0.90f, 0.50f), 14f, 65f);
        }

        // ── 도시 중심 (Core, -51.3 / 128) ───────────────────
        private static void PlaceCityCoreLights(GameObject root)
        {
            var warm = new Color(1.0f, 0.78f, 0.42f);
            var cool = new Color(0.80f, 0.90f, 1.0f);

            CreateBakedPoint(root, "Core_NW",     new Vector3(-68f, 8f, 145f), warm, 10f, 50f);
            CreateBakedPoint(root, "Core_NE",     new Vector3(-34f, 8f, 145f), warm, 10f, 50f);
            CreateBakedPoint(root, "Core_SW",     new Vector3(-68f, 8f, 111f), warm, 10f, 50f);
            CreateBakedPoint(root, "Core_SE",     new Vector3(-34f, 8f, 111f), warm, 10f, 50f);
            CreateBakedSpot (root, "Core_Center", new Vector3(-51f, 22f, 128f),
                Quaternion.Euler(90f, 0f, 0f), cool, 12f, 60f, 65f);
            CreateBakedPoint(root, "Core_Fill_N", new Vector3(-51f, 6f, 158f), warm, 7f, 45f);
            CreateBakedPoint(root, "Core_Fill_S", new Vector3(-51f, 6f,  98f), warm, 7f, 45f);
        }

        // ── 주요 도로 ─────────────────────────────────────────
        private static void PlaceMainStreetLights(GameObject root)
        {
            var amber = new Color(1.0f, 0.74f, 0.38f);
            const float h  = 7f;
            const float r  = 50f;   // range
            const float lx = 9f;    // intensity

            // 남북 도로 북쪽 (Core → 북문)
            foreach (float z in new[] { 158f, 190f, 220f, 250f })
                CreateBakedPoint(root, $"NS_N_{(int)z}", new Vector3(-51f, h, z), amber, lx, r);

            // 남북 도로 남쪽 (Core → 남문/성채)
            foreach (float z in new[] { 98f, 62f, 25f, -12f, -48f, -85f })
                CreateBakedPoint(root, $"NS_S_{(int)z}", new Vector3(-51f, h, z), amber, lx, r);

            // 동서 도로 (시내 → 항구)
            foreach (float x in new[] { -22f, 25f, 72f, 118f, 158f })
                CreateBakedPoint(root, $"EW_{(int)x}", new Vector3(x, h, 128f), amber, lx, r);

            // 서쪽 블록 (Block_03 / Block_04)
            CreateBakedPoint(root, "B03_A", new Vector3(-142f, h, 132f), amber, lx, r);
            CreateBakedPoint(root, "B03_B", new Vector3(-163f, h, 112f), amber, lx, r);
            CreateBakedPoint(root, "B04_A", new Vector3(-167f, h,  78f), amber, lx, r);
            CreateBakedPoint(root, "B04_B", new Vector3(-188f, h,  58f), amber, lx, r);

            // 동쪽 블록 (Block_01 / Block_09)
            CreateBakedPoint(root, "B01_A", new Vector3( 57f, h, 180f), amber, lx, r);
            CreateBakedPoint(root, "B01_B", new Vector3( 80f, h, 157f), amber, lx, r);
            CreateBakedPoint(root, "B09_A", new Vector3(107f, h,  62f), amber, lx, r);
            CreateBakedPoint(root, "B09_B", new Vector3(132f, h,  37f), amber, lx, r);

            // 성채 진입로
            CreateBakedPoint(root, "Citadel_Rd_A", new Vector3(  0f, h, -132f), amber, 8f, 45f);
            CreateBakedPoint(root, "Citadel_Rd_B", new Vector3(  8f, h, -168f), amber, 8f, 45f);
        }

        // ── 성벽 횃불 ────────────────────────────────────────
        private static void PlaceWallTorches(GameObject root)
        {
            var torch = new Color(1.0f, 0.62f, 0.20f);
            const float wH = 12f;
            const float wR = 30f;
            const float wI = 8f;

            var positions = new List<Vector3>
            {
                // 북쪽
                new(-82f, wH, 266f), new(-21f, wH, 266f),
                // 서쪽
                new(-212f, wH, 157f), new(-212f, wH, 102f),
                new(-212f, wH,  52f), new(-212f, wH,   2f),
                new(-201f, wH, -48f),
                // 남서
                new(-172f, wH,-102f), new(-148f, wH,-152f),
                new(-102f, wH,-182f),
                // 남쪽
                new( -72f, wH,-252f), new( -28f, wH,-332f),
                new(  22f, wH,-332f), new(  72f, wH,-322f),
                new( 117f, wH,-202f),
                // 동쪽
                new( 138f, wH,  62f), new( 138f, wH, 112f),
                new( 138f, wH, 157f),
                // 북동 (항구 방향)
                new(  74f, wH, 157f), new(  74f, wH, 202f),
            };

            for (int i = 0; i < positions.Count; i++)
                CreateBakedPoint(root, $"Wall_{i:00}", positions[i], torch, wI, wR);
        }

        // ═════════════════════════════════════════════════════
        //  ③ 베이킹 설정 + Global Volume
        // ═════════════════════════════════════════════════════
        private void ApplyBakeSettingsAndVolume()
        {
            if (!EnsureMapScene()) return;

            const string settingsPath = "Assets/Scenes/Map Lighting Settings.lighting";
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(settingsPath)
                           ?? new LightingSettings();

            if (!AssetDatabase.Contains(settings))
                AssetDatabase.CreateAsset(settings, settingsPath);

            settings.bakedGI             = true;
            settings.realtimeGI          = false;
            settings.mixedBakeMode       = MixedLightingMode.Shadowmask;
            settings.lightmapper         = LightingSettings.Lightmapper.ProgressiveGPU;
            settings.lightmapResolution      = _highQualityBake ? 20f : 10f;
            settings.lightmapMaxSize     = 4096;
            settings.directSampleCount   = _highQualityBake ? 256  : 64;
            settings.indirectSampleCount = _highQualityBake ? 2048 : 512;
            settings.environmentSampleCount = _highQualityBake ? 512 : 128;
            settings.maxBounces          = 3;
            settings.filteringMode       = LightingSettings.FilterMode.Auto;
            settings.ao                  = true;
            settings.aoMaxDistance       = 2f;

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
            Lightmapping.lightingSettings = settings;

            ApplyGlobalVolume();
            MarkDirty();
            Debug.Log("[MapNightLighting] ③ 완료. Generate Lighting 실행하세요.");
        }

        private static void ApplyGlobalVolume()
        {
            var volume = FindOrCreateGlobalVolume("Night Global Volume");

            const string profilePath = "Assets/Scenes/Map Night Volume Profile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }
            else
            {
                var toRemove = new List<VolumeComponent>(profile.components);
                foreach (var c in toRemove)
                    if (c != null) DestroyImmediate(c, true);
                profile.components.Clear();
                EditorUtility.SetDirty(profile);
            }

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.75f);
            bloom.intensity.Override(1.2f);
            bloom.scatter.Override(0.60f);
            bloom.tint.Override(new Color(0.90f, 0.85f, 1.0f));

            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(-0.30f);
            ca.contrast.Override(12f);
            ca.colorFilter.Override(new Color(0.84f, 0.91f, 1.0f));
            ca.saturation.Override(-8f);

            var smh = profile.Add<ShadowsMidtonesHighlights>(true);
            smh.shadows.Override(new Vector4(0.88f, 0.92f, 1.06f, 0f));
            smh.midtones.Override(new Vector4(0.93f, 0.96f, 1.02f, 0f));
            smh.highlights.Override(new Vector4(0.96f, 0.98f, 1.0f, 0f));

            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.28f);
            vig.smoothness.Override(0.38f);
            vig.color.Override(Color.black);

            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Medium1);
            grain.intensity.Override(0.10f);
            grain.response.Override(0.6f);

            Undo.RecordObject(volume, "Night Volume");
            volume.sharedProfile = profile;
            AssetDatabase.SaveAssets();
        }

        // ═════════════════════════════════════════════════════
        //  라이트 절대값 설정
        // ═════════════════════════════════════════════════════
        private static void SetLightsAbsolute(float intensity, float range, bool groupOnly)
        {
            if (!EnsureMapScene()) return;

            Light[] lights;
            if (groupOnly)
            {
                var group = GameObject.Find(LightGroupName);
                if (group == null)
                {
                    Debug.LogWarning("[MapNightLighting] 배치 그룹 없음. ②번 먼저 실행하세요.");
                    return;
                }
                lights = group.GetComponentsInChildren<Light>(true);
            }
            else
            {
                lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            }

            foreach (var l in lights)
            {
                Undo.RecordObject(l, "Set Light Absolute");
                l.intensity = Mathf.Max(0f, intensity);
                if (l.type != LightType.Directional)
                    l.range = Mathf.Max(0.1f, range);
            }

            MarkDirty();
            Debug.Log($"[MapNightLighting] {lights.Length}개 — Intensity={intensity:F2}, Range={range:F2} 설정 완료");
        }

        // ═════════════════════════════════════════════════════
        //  라이트 일괄 배율 조정
        // ═════════════════════════════════════════════════════
        private static void ScaleLights(float intensityMul, float rangeMul, bool groupOnly)
        {
            if (!EnsureMapScene()) return;

            Light[] lights;
            if (groupOnly)
            {
                var group = GameObject.Find(LightGroupName);
                if (group == null)
                {
                    Debug.LogWarning("[MapNightLighting] 배치 그룹 없음. ②번 먼저 실행하세요.");
                    return;
                }
                lights = group.GetComponentsInChildren<Light>(true);
            }
            else
            {
                lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            }

            foreach (var l in lights)
            {
                Undo.RecordObject(l, "Scale Light");
                l.intensity = Mathf.Max(0f, l.intensity * intensityMul);
                if (l.type != LightType.Directional)
                    l.range = Mathf.Max(0.1f, l.range * rangeMul);
            }

            MarkDirty();
            Debug.Log($"[MapNightLighting] {lights.Length}개 조정 — I×{intensityMul:F2}, R×{rangeMul:F2}");
        }

        // ═════════════════════════════════════════════════════
        //  ④ 씬 전체 ContributeGI 설정
        // ═════════════════════════════════════════════════════
        private static void SetAllRenderersForBaking()
        {
            if (!EnsureMapScene()) return;

            var renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            int count = 0;
            foreach (var r in renderers)
            {
                var go    = r.gameObject;
                var flags = GameObjectUtility.GetStaticEditorFlags(go);
                if ((flags & StaticEditorFlags.ContributeGI) == 0)
                {
                    Undo.RecordObject(go, "Set ContributeGI");
                    GameObjectUtility.SetStaticEditorFlags(go, flags | StaticEditorFlags.ContributeGI);
                }
                if (r.receiveGI != ReceiveGI.Lightmaps)
                {
                    Undo.RecordObject(r, "Set ReceiveGI");
                    r.receiveGI = ReceiveGI.Lightmaps;
                }
                count++;
            }

            MarkDirty();
            Debug.Log($"[MapNightLighting] ④ {count}개 Renderer 설정 완료");
        }

        private static void DiagnoseBakingReadiness()
        {
            if (!EnsureMapScene()) return;

            var all  = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            int gi   = 0, noGI = 0, lm = 0;
            foreach (var r in all)
            {
                var f = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                if ((f & StaticEditorFlags.ContributeGI) != 0) gi++; else noGI++;
                if (r.receiveGI == ReceiveGI.Lightmaps) lm++;
            }
            var ls = Lightmapping.lightingSettings;
            string lsStr = ls != null
                ? $"bakeRes={ls.lightmapResolution}, bakedGI={ls.bakedGI}, maxSize={ls.lightmapMaxSize}"
                : "없음 (③ 실행 필요)";

            Debug.Log(
                $"[진단] Renderer 총 {all.Length}개\n" +
                $"  ContributeGI O: {gi} / X: {noGI}  (X가 크면 ④ 실행)\n" +
                $"  Lightmaps GI: {lm}\n" +
                $"  LightingSettings: {lsStr}");
        }

        private static void ClearLightmapData()
        {
            if (!EnsureMapScene()) return;
            Lightmapping.ClearLightingDataAsset();
            Lightmapping.Clear();
            MarkDirty();
            Debug.Log("[MapNightLighting] 라이트맵 초기화 완료");
        }

        // ═════════════════════════════════════════════════════
        //  헬퍼 — 라이트 생성
        // ═════════════════════════════════════════════════════
        private static void CreateBakedPoint(
            GameObject parent, string name,
            Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Baked Point");
            go.transform.SetParent(parent.transform);
            go.transform.position = pos;

            var l = go.AddComponent<Light>();
            l.type             = LightType.Point;
            l.color            = color;
            l.intensity        = intensity;
            l.range            = range;
            l.shadows          = LightShadows.Soft;
            l.shadowStrength   = 0.65f;
            l.lightmapBakeType = LightmapBakeType.Baked;

            var urp = go.GetComponent<UniversalAdditionalLightData>();
            if (urp != null) urp.usePipelineSettings = true;
        }

        private static void CreateBakedSpot(
            GameObject parent, string name,
            Vector3 pos, Quaternion rot,
            Color color, float intensity, float range, float angle)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Baked Spot");
            go.transform.SetParent(parent.transform);
            go.transform.position = pos;
            go.transform.rotation = rot;

            var l = go.AddComponent<Light>();
            l.type             = LightType.Spot;
            l.color            = color;
            l.intensity        = intensity;
            l.range            = range;
            l.spotAngle        = angle;
            l.innerSpotAngle   = angle * 0.4f;
            l.shadows          = LightShadows.Soft;
            l.shadowStrength   = 0.60f;
            l.lightmapBakeType = LightmapBakeType.Baked;

            var urp = go.GetComponent<UniversalAdditionalLightData>();
            if (urp != null) urp.usePipelineSettings = true;
        }

        // ═════════════════════════════════════════════════════
        //  헬퍼 — 씬 / 오브젝트
        // ═════════════════════════════════════════════════════
        private static void RemoveLightGroup()
        {
            var g = GameObject.Find(LightGroupName);
            if (g == null) return;
            Undo.DestroyObjectImmediate(g);
            MarkDirty();
        }

        private static bool EnsureMapScene()
        {
            var s = SceneManager.GetSceneByPath(MapScenePath);
            if (s.isLoaded) return true;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
            return true;
        }

        private static void MarkDirty()
        {
            var s = SceneManager.GetSceneByPath(MapScenePath);
            if (s.isLoaded) EditorSceneManager.MarkSceneDirty(s);
        }

        private static Light FindOrCreateDirectionalLight()
        {
            if (RenderSettings.sun != null) return RenderSettings.sun;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) return l;
            var go = new GameObject("Moonlight");
            var lt = go.AddComponent<Light>();
            lt.type = LightType.Directional;
            return lt;
        }

        private static Volume FindOrCreateGlobalVolume(string goName)
        {
            foreach (var v in FindObjectsByType<Volume>(FindObjectsSortMode.None))
            {
                if (!v.isGlobal) continue;
                v.gameObject.name = goName;
                v.priority = 10;
                return v;
            }
            var go  = new GameObject(goName);
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10;
            return vol;
        }
    }
}
