#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// VRPostProcessingManager 자동 설정 도구
/// 씬에 필요한 Post Processing 컴포넌트들을 자동으로 생성합니다.
/// </summary>
public class VRPostProcessingManagerSetup : EditorWindow
{
    [MenuItem("Window/VR Horror Game/Setup VR Post Processing Manager")]
    public static void ShowWindow()
    {
        GetWindow<VRPostProcessingManagerSetup>("VR Post Processing Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("VR Post Processing Manager Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "VR 피격 효과를 위해 VRPostProcessingManager가 필요합니다.\n" +
            "아래 버튼을 클릭하여 자동으로 설정하세요.", 
            MessageType.Info);

        GUILayout.Space(15);

        if (GUILayout.Button("Create VR Post Processing Manager", GUILayout.Height(40)))
        {
            SetupPostProcessing();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Setup Global Volume", GUILayout.Height(30)))
        {
            SetupGlobalVolume();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Enable Camera Post Processing", GUILayout.Height(30)))
        {
            EnableCameraPostProcessing();
        }

        GUILayout.Space(15);

        EditorGUILayout.LabelField("Current Status:", EditorStyles.boldLabel);
        
        // VRPostProcessingManager 확인
        VRPostProcessingManager manager = FindFirstObjectByType<VRPostProcessingManager>();
        string managerStatus = manager != null ? "Found" : "Missing";
        EditorGUILayout.LabelField($"VRPostProcessingManager: {managerStatus}");
        
        // Global Volume 확인
        Volume globalVolume = FindFirstObjectByType<Volume>();
        string volumeStatus = globalVolume != null ? "Found" : "Missing";
        EditorGUILayout.LabelField($"Global Volume: {volumeStatus}");
        
        // Camera Post Processing 확인
        Camera mainCamera = Camera.main;
        bool hasPostProcessing = false;
        if (mainCamera != null)
        {
            var cameraData = mainCamera.GetComponent<UniversalAdditionalCameraData>();
            hasPostProcessing = cameraData != null && cameraData.renderPostProcessing;
        }
        string cameraStatus = hasPostProcessing ? "Enabled" : "Disabled";
        EditorGUILayout.LabelField($"Camera Post Processing: {cameraStatus}");
    }

    private void SetupPostProcessing()
    {
        // 1. Global Volume 생성
        GameObject volumeObject = new GameObject("Global Volume");
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0;

        // 2. 깨끗한 새 VolumeProfile 생성 (DefaultVolumeProfile 사용 안함)
        VolumeProfile newProfile = CreateCleanVRProfile();
        volume.profile = newProfile;

        // 3. VRPostProcessingManager 생성
        GameObject managerObject = new GameObject("VR Post Processing Manager");
        VRPostProcessingManager manager = managerObject.AddComponent<VRPostProcessingManager>();

        // 4. Manager에 Volume 할당 (Reflection 사용)
        SetPrivateField(manager, "globalVolume", volume);

        // 5. 호러 효과 프리셋들 생성 및 할당
        SetPrivateField(manager, "normalProfile", CreateHorrorProfile("VR_Normal_Profile", 0.3f, -5f, Color.black));
        SetPrivateField(manager, "scareProfile", CreateHorrorProfile("VR_Scare_Profile", 0.6f, -15f, Color.black));
        SetPrivateField(manager, "deathProfile", CreateHorrorProfile("VR_Death_Profile", 1.0f, -100f, Color.black));
        SetPrivateField(manager, "lowHealthProfile", CreateHorrorProfile("VR_LowHealth_Profile", 0.7f, -20f, Color.red));

        // 6. Camera Post Processing 자동 활성화
        EnableCameraPostProcessingAuto();

        Debug.Log("[VRPostProcessingManagerSetup] VRPostProcessingManager created successfully!");
        Debug.Log("[VRPostProcessingManagerSetup] Camera Post Processing enabled");
        Debug.Log("[VRPostProcessingManagerSetup] Test VR damage effect with [T] key!");
        
        EditorUtility.DisplayDialog("Setup Complete!", 
            "VR Post Processing Manager 설정 완료!\n\n" +
            "Global Volume 생성\n" +
            "Volume Profiles 할당\n" +
            "Camera Post Processing 활성화\n\n" +
            "[T] 키로 VR 피격 효과를 테스트하세요!", "확인");
    }

    /// <summary>
    /// 깨끗한 VR 전용 Volume Profile 생성 (테스트 컴포넌트 없음)
    /// </summary>
    private VolumeProfile CreateCleanVRProfile()
    {
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        
        // 기본 VR 최적화 설정만 추가
        var vignette = profile.Add<Vignette>();
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.2f;
        vignette.color.overrideState = true;
        vignette.color.value = Color.black;

        var colorAdjustments = profile.Add<ColorAdjustments>();
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.saturation.value = 0f;
        colorAdjustments.contrast.overrideState = true;
        colorAdjustments.contrast.value = 5f;

        // 저장
        string path = "Assets/Settings/VR_Clean_PostProcessProfile.asset";
        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"[VRPostProcessingManagerSetup] Created clean VR profile: {path}");
        return profile;
    }

    /// <summary>
    /// 호러 효과 전용 Volume Profile 생성
    /// </summary>
    private VolumeProfile CreateHorrorProfile(string profileName, float vignetteIntensity, float saturation, Color vignetteColor)
    {
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        
        // Vignette 설정
        var vignette = profile.Add<Vignette>();
        vignette.intensity.overrideState = true;
        vignette.intensity.value = vignetteIntensity;
        vignette.color.overrideState = true;
        vignette.color.value = vignetteColor;
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = 0.4f;

        // Color Adjustments 설정
        var colorAdjustments = profile.Add<ColorAdjustments>();
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.saturation.value = saturation;
        colorAdjustments.contrast.overrideState = true;
        colorAdjustments.contrast.value = Mathf.Clamp(20f - Mathf.Abs(saturation) * 0.3f, -30f, 30f);

        // 저장
        string path = $"Assets/Settings/{profileName}.asset";
        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"[VRPostProcessingManagerSetup] Created horror profile: {profileName}");
        return profile;
    }

    /// <summary>
    /// Reflection을 사용하여 private 필드에 값 할당
    /// </summary>
    private void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(target, value);
            Debug.Log($"[VRPostProcessingManagerSetup] Set {fieldName} = {value?.GetType().Name}");
        }
        else
        {
            Debug.LogWarning($"[VRPostProcessingManagerSetup] Field '{fieldName}' not found!");
        }
    }

    /// <summary>
    /// Camera Post Processing 자동 활성화
    /// </summary>
    private void EnableCameraPostProcessingAuto()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            // OVRCameraRig에서 Center Eye Camera 찾기
            OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
            if (cameraRig != null)
            {
                mainCamera = cameraRig.centerEyeAnchor?.GetComponent<Camera>();
            }
        }

        if (mainCamera != null)
        {
            // Universal Additional Camera Data 추가/설정
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
            Debug.Log($"[VRPostProcessingManagerSetup] Camera Post Processing 활성화: {mainCamera.name}");
        }
        else
        {
            Debug.LogWarning("[VRPostProcessingManagerSetup] Main Camera를 찾을 수 없습니다!");
        }
    }

    private void SetupGlobalVolume()
    {
        Volume existingVolume = FindFirstObjectByType<Volume>();
        if (existingVolume != null)
        {
            EditorUtility.DisplayDialog("Already Exists", 
                "Global Volume already exists in the scene!", "OK");
            return;
        }

        // Global Volume 생성
        GameObject volumeObj = new GameObject("Global Volume");
        Volume volume = volumeObj.AddComponent<Volume>();
        volume.isGlobal = true;

        // VR Horror Profile 할당
        string profilePath = "Assets/Settings/VR_Horror_PostProcessProfile.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        
        if (profile != null)
        {
            volume.profile = profile;
            Debug.Log("[VRPostProcessingManagerSetup] VR Horror Profile assigned to Global Volume");
        }
        else
        {
            Debug.LogWarning("[VRPostProcessingManagerSetup] VR Horror Profile not found, creating default profile");
            volume.profile = CreateDefaultProfile();
        }

        EditorUtility.SetDirty(volumeObj);
        Selection.activeGameObject = volumeObj;

        EditorUtility.DisplayDialog("Success", 
            "Global Volume has been created and configured!", "OK");
    }

    private void EnableCameraPostProcessing()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            EditorUtility.DisplayDialog("Error", 
                "Main Camera not found!", "OK");
            return;
        }

        // Universal Additional Camera Data 추가/설정
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

        Debug.Log("[VRPostProcessingManagerSetup] Camera Post Processing enabled");
        
        EditorUtility.DisplayDialog("Success", 
            "Camera Post Processing has been enabled!", "OK");
    }

    private VolumeProfile CreateDefaultProfile()
    {
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        
        // Vignette 추가
        var vignette = profile.Add<Vignette>();
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.3f;
        vignette.color.overrideState = true;
        vignette.color.value = Color.black;

        // Color Adjustments 추가
        var colorAdjustments = profile.Add<ColorAdjustments>();
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.saturation.value = -10f;
        colorAdjustments.contrast.overrideState = true;
        colorAdjustments.contrast.value = 10f;

        // 프로필 저장
        string path = "Assets/Settings/Default_VR_PostProcessProfile.asset";
        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();

        return profile;
    }
}
#endif 