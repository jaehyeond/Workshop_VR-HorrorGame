using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// VR Post Processing 환경을 자동으로 설정하는 에디터 스크립트
/// </summary>
public class VRPostProcessingSetup : EditorWindow
{
    private VolumeProfile horrorProfile;
    private bool setupComplete = false;
    
    [MenuItem("Window/VR Post Processing Setup")]
    public static void ShowWindow()
    {
        VRPostProcessingSetup window = GetWindow<VRPostProcessingSetup>("VR Post Processing Setup");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }
    
    private void OnGUI()
    {
        GUILayout.Space(10);
        
        EditorGUILayout.LabelField("VR 호러 게임 Post Processing 설정", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "이 도구는 VR 호러 게임에 최적화된 Post Processing 환경을 자동으로 설정합니다.\n" +
            "• URP Volume 생성\n" +
            "• 호러 분위기 효과 적용\n" +
            "• VR 성능 최적화\n" +
            "• 카메라 설정 자동화", 
            MessageType.Info);
        
        GUILayout.Space(20);
        
        // 현재 상태 표시
        DisplayCurrentStatus();
        
        GUILayout.Space(20);
        
        // 설정 버튼들
        if (GUILayout.Button("1. URP 렌더 파이프라인 확인", GUILayout.Height(30)))
        {
            CheckURPSetup();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("2. Global Volume 생성/설정", GUILayout.Height(30)))
        {
            SetupGlobalVolume();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("3. 카메라 Post Processing 활성화", GUILayout.Height(30)))
        {
            SetupCameraPostProcessing();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("4. 호러 Profile 적용", GUILayout.Height(30)))
        {
            ApplyHorrorProfile();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("5. VR 최적화 적용", GUILayout.Height(30)))
        {
            ApplyVROptimizations();
        }
        
        GUILayout.Space(20);
        
        // 전체 자동 설정
        if (GUILayout.Button("🎮 모든 설정 자동 실행", GUILayout.Height(40)))
        {
            RunFullSetup();
        }
        
        GUILayout.Space(10);
        
        if (setupComplete)
        {
            EditorGUILayout.HelpBox(
                "✅ VR Post Processing 설정이 완료되었습니다!\n" +
                "게임을 실행하여 호러 분위기 효과를 확인해보세요.", 
                MessageType.Info);
        }
        
        GUILayout.Space(20);
        
        // 추가 도구들
        EditorGUILayout.LabelField("추가 도구", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Post Processing Manager 추가"))
        {
            AddPostProcessingManager();
        }
        
        if (GUILayout.Button("성능 모니터링 컴포넌트 추가"))
        {
            AddPerformanceMonitor();
        }
    }
    
    private void DisplayCurrentStatus()
    {
        EditorGUILayout.LabelField("현재 상태", EditorStyles.boldLabel);
        
        // URP 확인
        var renderPipeline = GraphicsSettings.currentRenderPipeline;
        bool isURP = renderPipeline != null && renderPipeline.GetType().Name.Contains("Universal");
        
        string urpStatus = isURP ? "✅ URP 활성화됨" : "❌ URP가 설정되지 않음";
        EditorGUILayout.LabelField("렌더 파이프라인:", urpStatus);
        
        // 글로벌 볼륨 확인
        Volume globalVolume = FindFirstObjectByType<Volume>();
        bool hasGlobalVolume = globalVolume != null && globalVolume.isGlobal;
        
        string volumeStatus = hasGlobalVolume ? "✅ Global Volume 존재함" : "❌ Global Volume 없음";
        EditorGUILayout.LabelField("Post Processing Volume:", volumeStatus);
        
        // 메인 카메라 확인
        Camera mainCamera = Camera.main;
        bool hasMainCamera = mainCamera != null;
        
        string cameraStatus = hasMainCamera ? "✅ 메인 카메라 발견" : "❌ 메인 카메라 없음";
        EditorGUILayout.LabelField("메인 카메라:", cameraStatus);
        
        if (hasMainCamera)
        {
            var cameraData = mainCamera.GetComponent<UniversalAdditionalCameraData>();
            bool hasPostProcessing = cameraData != null && cameraData.renderPostProcessing;
            
            string ppStatus = hasPostProcessing ? "✅ Post Processing 활성화됨" : "❌ Post Processing 비활성화됨";
            EditorGUILayout.LabelField("카메라 Post Processing:", ppStatus);
        }
    }
    
    private void CheckURPSetup()
    {
        var renderPipeline = GraphicsSettings.currentRenderPipeline;
        bool isURP = renderPipeline != null && renderPipeline.GetType().Name.Contains("Universal");
        
        if (!isURP)
        {
            EditorUtility.DisplayDialog("URP 설정 필요", 
                "현재 프로젝트가 URP(Universal Render Pipeline)를 사용하지 않습니다.\n" +
                "Window > Rendering > Render Pipeline Converter를 사용하여 URP로 변환하거나,\n" +
                "새로운 URP Asset을 생성하여 Graphics Settings에서 설정해주세요.", 
                "확인");
        }
        else
        {
            EditorUtility.DisplayDialog("URP 확인 완료", 
                "URP가 올바르게 설정되어 있습니다!", 
                "확인");
        }
    }
    
    private void SetupGlobalVolume()
    {
        // 기존 Global Volume 찾기
                    Volume existingVolume = FindFirstObjectByType<Volume>();
        
        if (existingVolume != null && existingVolume.isGlobal)
        {
            Debug.Log("기존 Global Volume을 사용합니다: " + existingVolume.name);
            return;
        }
        
        // 새 Global Volume 생성
        GameObject volumeObj = new GameObject("Global Volume");
        Volume volume = volumeObj.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0;
        
        // Volume Profile 생성
        VolumeProfile profile = CreateHorrorProfile();
        volume.profile = profile;
        
        // 씬에 등록
        EditorUtility.SetDirty(volumeObj);
        
        Debug.Log("Global Volume이 생성되었습니다: " + volumeObj.name);
        
        EditorUtility.DisplayDialog("Global Volume 생성 완료", 
            "Global Volume이 성공적으로 생성되었습니다!", 
            "확인");
    }
    
    private VolumeProfile CreateHorrorProfile()
    {
        // 기존 프로필이 있는지 확인
        string profilePath = "Assets/Settings/URP_HorrorProfile.asset";
        VolumeProfile existingProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        
        if (existingProfile != null)
        {
            return existingProfile;
        }
        
        // 새 프로필 생성
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        
        // 디렉토리 생성
        string directory = "Assets/Settings";
        if (!AssetDatabase.IsValidFolder(directory))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
        }
        
        AssetDatabase.CreateAsset(profile, profilePath);
        
        // 호러 효과들 추가
        AddHorrorEffectsToProfile(profile);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        return profile;
    }
    
    private void AddHorrorEffectsToProfile(VolumeProfile profile)
    {
        // Bloom 효과
        if (!profile.TryGet<Bloom>(out var bloom))
        {
            bloom = profile.Add<Bloom>();
        }
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 1.2f;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 0.25f;
        bloom.tint.overrideState = true;
        bloom.tint.value = new Color(1f, 0.9f, 0.8f);
        
        // Color Adjustments
        if (!profile.TryGet<ColorAdjustments>(out var colorAdjustments))
        {
            colorAdjustments = profile.Add<ColorAdjustments>();
        }
        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.postExposure.value = -0.2f;
        colorAdjustments.contrast.overrideState = true;
        colorAdjustments.contrast.value = 15f;
        colorAdjustments.colorFilter.overrideState = true;
        colorAdjustments.colorFilter.value = new Color(0.8f, 0.85f, 1f);
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.saturation.value = -15f;
        
        // Vignette
        if (!profile.TryGet<Vignette>(out var vignette))
        {
            vignette = profile.Add<Vignette>();
        }
        vignette.color.overrideState = true;
        vignette.color.value = Color.black;
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.35f;
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = 0.25f;
        
        // Film Grain
        if (!profile.TryGet<FilmGrain>(out var filmGrain))
        {
            filmGrain = profile.Add<FilmGrain>();
        }
        filmGrain.intensity.overrideState = true;
        filmGrain.intensity.value = 0.12f;
        filmGrain.response.overrideState = true;
        filmGrain.response.value = 0.8f;
        
        // Chromatic Aberration
        if (!profile.TryGet<ChromaticAberration>(out var chromaticAberration))
        {
            chromaticAberration = profile.Add<ChromaticAberration>();
        }
        chromaticAberration.intensity.overrideState = true;
        chromaticAberration.intensity.value = 0.08f;
        
        EditorUtility.SetDirty(profile);
    }
    
    private void SetupCameraPostProcessing()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            EditorUtility.DisplayDialog("오류", 
                "메인 카메라를 찾을 수 없습니다!", 
                "확인");
            return;
        }
        
        // Universal Additional Camera Data 추가
        UniversalAdditionalCameraData cameraData = mainCamera.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData == null)
        {
            cameraData = mainCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        }
        
        // Post Processing 활성화
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        cameraData.antialiasingQuality = AntialiasingQuality.Medium;
        
        EditorUtility.SetDirty(mainCamera.gameObject);
        
        Debug.Log("카메라 Post Processing이 활성화되었습니다: " + mainCamera.name);
        
        EditorUtility.DisplayDialog("카메라 설정 완료", 
            "메인 카메라의 Post Processing이 활성화되었습니다!", 
            "확인");
    }
    
    private void ApplyHorrorProfile()
    {
        Volume globalVolume = FindFirstObjectByType<Volume>();
        if (globalVolume == null)
        {
            EditorUtility.DisplayDialog("오류", 
                "Global Volume을 먼저 생성해주세요!", 
                "확인");
            return;
        }
        
        VolumeProfile profile = CreateHorrorProfile();
        globalVolume.profile = profile;
        
        EditorUtility.SetDirty(globalVolume.gameObject);
        
        EditorUtility.DisplayDialog("호러 Profile 적용 완료", 
            "호러 분위기 Post Processing Profile이 적용되었습니다!", 
            "확인");
    }
    
    private void ApplyVROptimizations()
    {
        // Quality Settings 최적화
        QualitySettings.antiAliasing = 4; // 4x MSAA
        QualitySettings.vSyncCount = 0; // VR에서는 VSync 비활성화
        QualitySettings.shadows = UnityEngine.ShadowQuality.All;
        QualitySettings.shadowResolution = UnityEngine.ShadowResolution.Medium;
        
        // VR 텍스처 스케일 설정
        if (UnityEngine.XR.XRSettings.enabled)
        {
            UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.0f;
        }
        
        // Fixed Delta Time 설정 (90Hz VR)
        Time.fixedDeltaTime = 1f / 90f;
        
        EditorUtility.DisplayDialog("VR 최적화 적용 완료", 
            "VR 환경에 최적화된 설정이 적용되었습니다!", 
            "확인");
    }
    
    private void RunFullSetup()
    {
        EditorUtility.DisplayProgressBar("VR Post Processing 설정", "URP 확인 중...", 0.2f);
        CheckURPSetup();
        
        EditorUtility.DisplayProgressBar("VR Post Processing 설정", "Global Volume 생성 중...", 0.4f);
        SetupGlobalVolume();
        
        EditorUtility.DisplayProgressBar("VR Post Processing 설정", "카메라 설정 중...", 0.6f);
        SetupCameraPostProcessing();
        
        EditorUtility.DisplayProgressBar("VR Post Processing 설정", "호러 Profile 적용 중...", 0.8f);
        ApplyHorrorProfile();
        
        EditorUtility.DisplayProgressBar("VR Post Processing 설정", "VR 최적화 적용 중...", 1.0f);
        ApplyVROptimizations();
        
        EditorUtility.ClearProgressBar();
        
        setupComplete = true;
        
        EditorUtility.DisplayDialog("설정 완료", 
            "🎮 VR 호러 게임 Post Processing 환경 설정이 완료되었습니다!\n\n" +
            "✅ URP 확인 완료\n" +
            "✅ Global Volume 생성\n" +
            "✅ 카메라 Post Processing 활성화\n" +
            "✅ 호러 분위기 효과 적용\n" +
            "✅ VR 성능 최적화\n\n" +
            "이제 게임을 실행하여 효과를 확인해보세요!", 
            "확인");
    }
    
    private void AddPostProcessingManager()
    {
        GameObject managerObj = new GameObject("VR Post Processing Manager");
        managerObj.AddComponent<VRPostProcessingManager>();
        
        EditorUtility.SetDirty(managerObj);
        
        EditorUtility.DisplayDialog("Post Processing Manager 추가", 
            "VR Post Processing Manager가 씬에 추가되었습니다!", 
            "확인");
    }
    
    private void AddPerformanceMonitor()
    {
        // 간단한 성능 모니터링 오브젝트 생성
        GameObject monitorObj = new GameObject("VR Performance Monitor");
        
        // 커스텀 성능 모니터 컴포넌트가 있다면 추가
        // monitorObj.AddComponent<VRPerformanceMonitor>();
        
        EditorUtility.SetDirty(monitorObj);
        
        EditorUtility.DisplayDialog("성능 모니터 추가", 
            "VR 성능 모니터링 오브젝트가 추가되었습니다!", 
            "확인");
    }
} 