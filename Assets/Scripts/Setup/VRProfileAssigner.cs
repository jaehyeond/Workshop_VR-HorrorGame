#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// VRPostProcessingManager에 Volume Profile들을 자동으로 할당하는 도구
/// </summary>
public class VRProfileAssigner : EditorWindow
{
    [MenuItem("Window/VR Horror Game/Assign VR Profiles")]
    public static void ShowWindow()
    {
        GetWindow<VRProfileAssigner>("VR Profile Assigner");
    }

    void OnGUI()
    {
        GUILayout.Label("VR Volume Profile Auto Assigner", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "VRPostProcessingManager의 Volume Profile들을 자동으로 할당합니다.\n" +
            "빨간 화면 피격 효과를 위해 필요합니다.", 
            MessageType.Info);

        GUILayout.Space(15);

        if (GUILayout.Button("Auto Assign All Profiles", GUILayout.Height(40)))
        {
            AssignAllProfiles();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Create Missing Profiles", GUILayout.Height(30)))
        {
            CreateMissingProfiles();
        }

        GUILayout.Space(15);

        // 현재 상태 표시
        DisplayCurrentStatus();
    }

    private void AssignAllProfiles()
    {
        VRPostProcessingManager manager = FindFirstObjectByType<VRPostProcessingManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Error", 
                "VRPostProcessingManager not found in scene!", "OK");
            return;
        }

        // SerializedObject를 사용하여 private 필드에 접근
        SerializedObject serializedManager = new SerializedObject(manager);

        // Global Volume 할당
        Volume globalVolume = FindFirstObjectByType<Volume>();
        if (globalVolume != null)
        {
            SerializedProperty globalVolumeProp = serializedManager.FindProperty("globalVolume");
            globalVolumeProp.objectReferenceValue = globalVolume;

            // Global Volume에 Profile 할당
            if (globalVolume.profile == null)
            {
                VolumeProfile defaultProfile = FindOrCreateProfile("VR_Horror_PostProcessProfile");
                globalVolume.profile = defaultProfile;
                EditorUtility.SetDirty(globalVolume);
            }
        }

        // 호러 효과 프리셋들 할당
        AssignProfile(serializedManager, "normalProfile", "VR_Normal_Profile");
        AssignProfile(serializedManager, "scareProfile", "VR_Scare_Profile");
        AssignProfile(serializedManager, "deathProfile", "VR_Death_Profile");
        AssignProfile(serializedManager, "lowHealthProfile", "VR_LowHealth_Profile");

        // 변경사항 적용
        serializedManager.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);

        Debug.Log("[VRProfileAssigner] All profiles assigned successfully!");
        
        EditorUtility.DisplayDialog("Success", 
            "All Volume Profiles have been assigned!\n\n" +
            "Now test the VR damage effect with [T] key.", "OK");
    }

    private void AssignProfile(SerializedObject serializedManager, string propertyName, string profileName)
    {
        SerializedProperty property = serializedManager.FindProperty(propertyName);
        if (property != null)
        {
            VolumeProfile profile = FindOrCreateProfile(profileName);
            property.objectReferenceValue = profile;
        }
    }

    private VolumeProfile FindOrCreateProfile(string profileName)
    {
        // 기존 프로필 찾기
        string[] guids = AssetDatabase.FindAssets($"{profileName} t:VolumeProfile");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        }

        // 기본 프로필 사용
        guids = AssetDatabase.FindAssets("VR_Horror_PostProcessProfile t:VolumeProfile");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        }

        // 새 프로필 생성
        return CreateNewProfile(profileName);
    }

    private VolumeProfile CreateNewProfile(string profileName)
    {
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        
        // 기본 호러 효과 설정
        SetupHorrorEffects(profile, profileName);

        // 저장
        string path = $"Assets/Settings/{profileName}.asset";
        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"[VRProfileAssigner] Created new profile: {profileName}");
        return profile;
    }

    private void SetupHorrorEffects(VolumeProfile profile, string profileName)
    {
        // Vignette 추가
        var vignette = profile.Add<Vignette>();
        vignette.intensity.overrideState = true;
        vignette.color.overrideState = true;
        vignette.color.value = Color.black;

        // Color Adjustments 추가
        var colorAdjustments = profile.Add<ColorAdjustments>();
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.contrast.overrideState = true;

        // 프로필별 특화 설정
        switch (profileName)
        {
            case "VR_Normal_Profile":
                vignette.intensity.value = 0.3f;
                colorAdjustments.saturation.value = -5f;
                colorAdjustments.contrast.value = 10f;
                break;

            case "VR_Scare_Profile":
                vignette.intensity.value = 0.6f;
                colorAdjustments.saturation.value = -15f;
                colorAdjustments.contrast.value = 25f;
                break;

            case "VR_Death_Profile":
                vignette.intensity.value = 1.0f;
                colorAdjustments.saturation.value = -100f; // 흑백
                colorAdjustments.contrast.value = -20f;
                break;

            case "VR_LowHealth_Profile":
                vignette.intensity.value = 0.7f;
                vignette.color.value = Color.red; // 빨간 비네팅
                colorAdjustments.saturation.value = -20f;
                break;

            default:
                vignette.intensity.value = 0.4f;
                colorAdjustments.saturation.value = -10f;
                colorAdjustments.contrast.value = 15f;
                break;
        }
    }

    private void CreateMissingProfiles()
    {
        string[] profileNames = {
            "VR_Horror_PostProcessProfile",
            "VR_Normal_Profile", 
            "VR_Scare_Profile", 
            "VR_Death_Profile", 
            "VR_LowHealth_Profile"
        };

        int createdCount = 0;
        foreach (string profileName in profileNames)
        {
            string[] guids = AssetDatabase.FindAssets($"{profileName} t:VolumeProfile");
            if (guids.Length == 0)
            {
                CreateNewProfile(profileName);
                createdCount++;
            }
        }

        EditorUtility.DisplayDialog("Profiles Created", 
            $"Created {createdCount} missing Volume Profiles!", "OK");
    }

    private void DisplayCurrentStatus()
    {
        EditorGUILayout.LabelField("Current Status:", EditorStyles.boldLabel);
        
        VRPostProcessingManager manager = FindFirstObjectByType<VRPostProcessingManager>();
        if (manager == null)
        {
            EditorGUILayout.LabelField("VRPostProcessingManager: Missing");
            return;
        }

        EditorGUILayout.LabelField("VRPostProcessingManager: Found");

        // Volume 상태 확인
        Volume globalVolume = FindFirstObjectByType<Volume>();
        string volumeStatus = globalVolume != null ? "Found" : "Missing";
        EditorGUILayout.LabelField($"Global Volume: {volumeStatus}");

        if (globalVolume != null)
        {
            string profileStatus = globalVolume.profile != null ? "Assigned" : "Missing";
            EditorGUILayout.LabelField($"Volume Profile: {profileStatus}");
        }
    }
}
#endif 