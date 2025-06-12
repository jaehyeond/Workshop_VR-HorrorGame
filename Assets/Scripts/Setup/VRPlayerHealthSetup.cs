#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// VRPlayerHealth 자동 설정 도구
/// Enemy 공격이 제대로 작동하도록 VR 플레이어에 VRPlayerHealth 컴포넌트를 추가합니다.
/// </summary>
public class VRPlayerHealthSetup : EditorWindow
{
    [MenuItem("Window/VR Horror Game/Setup VR Player Health")]
    public static void ShowWindow()
    {
        GetWindow<VRPlayerHealthSetup>("VR Player Health Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("VR Player Health Auto Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Enemy 공격이 작동하려면 VR 플레이어에 VRPlayerHealth 컴포넌트가 필요합니다.\n" +
            "이 도구는 자동으로 올바른 위치에 VRPlayerHealth를 추가합니다.", 
            MessageType.Info);

        GUILayout.Space(15);

        if (GUILayout.Button("Auto Setup VR Player Health", GUILayout.Height(40)))
        {
            SetupVRPlayerHealth();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Check Current Setup", GUILayout.Height(30)))
        {
            CheckCurrentSetup();
        }

        GUILayout.Space(15);

        // 현재 상태 표시
        DisplayCurrentStatus();
    }

    private void SetupVRPlayerHealth()
    {
        // 1. OVRCameraRig 찾기
        OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
        if (cameraRig == null)
        {
            EditorUtility.DisplayDialog("Error", 
                "OVRCameraRig를 찾을 수 없습니다!\n씬에 VR 플레이어가 있는지 확인하세요.", "OK");
            return;
        }

        // 2. 이미 VRPlayerHealth가 있는지 확인
        VRPlayerHealth existingHealth = cameraRig.GetComponent<VRPlayerHealth>();
        if (existingHealth != null)
        {
            EditorUtility.DisplayDialog("Already Exists", 
                "VRPlayerHealth가 이미 존재합니다!", "OK");
            return;
        }

        // 3. VRPlayerHealth 추가
        VRPlayerHealth playerHealth = cameraRig.gameObject.AddComponent<VRPlayerHealth>();
        
        // 4. 기본 설정 적용
        playerHealth.maxHealth = 100f;
        playerHealth.damageEffectDuration = 1.5f;
        playerHealth.damageScreenIntensity = 0.8f;
        playerHealth.invincibilityDuration = 1f;

        // 5. 변경사항 저장
        EditorUtility.SetDirty(cameraRig.gameObject);

        Debug.Log("[VRPlayerHealthSetup] ✅ VRPlayerHealth 추가 완료!");
        
        EditorUtility.DisplayDialog("Success", 
            "VRPlayerHealth가 성공적으로 추가되었습니다!\n\n" +
            "이제 Enemy 공격이 VR 피격 효과를 트리거할 수 있습니다.", "OK");
    }

    private void CheckCurrentSetup()
    {
        string report = "🔍 VR Player Health 설정 확인:\n\n";
        
        // OVRCameraRig 확인
        OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
        if (cameraRig != null)
        {
            report += "✅ OVRCameraRig 발견\n";
            
            // VRPlayerHealth 확인
            VRPlayerHealth playerHealth = cameraRig.GetComponent<VRPlayerHealth>();
            if (playerHealth != null)
            {
                report += "✅ VRPlayerHealth 컴포넌트 존재\n";
                report += $"   - Max Health: {playerHealth.maxHealth}\n";
                report += $"   - Damage Duration: {playerHealth.damageEffectDuration}s\n";
                report += $"   - Damage Intensity: {playerHealth.damageScreenIntensity}\n";
            }
            else
            {
                report += "❌ VRPlayerHealth 컴포넌트 없음\n";
                report += "   → 'Auto Setup VR Player Health' 버튼을 클릭하세요\n";
            }
        }
        else
        {
            report += "❌ OVRCameraRig를 찾을 수 없음\n";
            report += "   → 씬에 VR 플레이어를 추가하세요\n";
        }

        // VRPostProcessingManager 확인
        VRPostProcessingManager postManager = FindFirstObjectByType<VRPostProcessingManager>();
        if (postManager != null)
        {
            report += "✅ VRPostProcessingManager 존재\n";
        }
        else
        {
            report += "❌ VRPostProcessingManager 없음\n";
            report += "   → Window → VR Horror Game → Setup VR Post Processing Manager\n";
        }

        Debug.Log(report);
        EditorUtility.DisplayDialog("Setup Check", report, "OK");
    }

    private void DisplayCurrentStatus()
    {
        EditorGUILayout.LabelField("Current Status:", EditorStyles.boldLabel);
        
        OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
        string cameraStatus = cameraRig != null ? "Found" : "Missing";
        EditorGUILayout.LabelField($"OVRCameraRig: {cameraStatus}");

        if (cameraRig != null)
        {
            VRPlayerHealth playerHealth = cameraRig.GetComponent<VRPlayerHealth>();
            string healthStatus = playerHealth != null ? "Installed" : "Missing";
            EditorGUILayout.LabelField($"VRPlayerHealth: {healthStatus}");
        }

        VRPostProcessingManager postManager = FindFirstObjectByType<VRPostProcessingManager>();
        string postStatus = postManager != null ? "Found" : "Missing";
        EditorGUILayout.LabelField($"VRPostProcessingManager: {postStatus}");
    }
}
#endif 