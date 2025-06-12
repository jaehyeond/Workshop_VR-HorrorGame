#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Unity 6 Input System 설정 가이드
/// New Input System Package 사용을 위한 올바른 설정 방법 안내
/// </summary>
public class Unity6InputSystemGuide : EditorWindow
{
    [MenuItem("Window/VR Horror Game/Unity 6 Input System Guide")]
    public static void ShowWindow()
    {
        GetWindow<Unity6InputSystemGuide>("Unity 6 Input Guide");
    }

    void OnGUI()
    {
        GUILayout.Label("Unity 6 Input System - Correct Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Unity 6에서는 New Input System Package가 기본이며 권장됩니다.\n" +
            "Meta Quest VR 프로젝트에서는 반드시 New Input System을 사용해야 합니다.", 
            MessageType.Info);

        GUILayout.Space(15);

        EditorGUILayout.LabelField("Correct Setup Steps:", EditorStyles.boldLabel);
        
        EditorGUILayout.LabelField("1. Edit → Project Settings");
        EditorGUILayout.LabelField("2. Player → Configuration");
        EditorGUILayout.LabelField("3. Active Input Handling → Input System Package (New)");
        EditorGUILayout.LabelField("4. Unity Restart");
        
        GUILayout.Space(15);

        if (GUILayout.Button("Open Project Settings", GUILayout.Height(30)))
        {
            SettingsService.OpenProjectSettings("Project/Player");
        }

        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "After setup: VRAttackDebugger [T] key test will work properly.\n" +
            "'Both' setting is unnecessary in Unity 6 and may cause conflicts.", 
            MessageType.Warning);

        GUILayout.Space(15);

        EditorGUILayout.LabelField("Current Input System Status:", EditorStyles.boldLabel);
        
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        EditorGUILayout.HelpBox("New Input System Only - Correct Setup!", MessageType.Info);
#elif ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
        EditorGUILayout.HelpBox("Both Systems - Unity 6 recommends New Only", MessageType.Warning);
#elif !ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
        EditorGUILayout.HelpBox("Legacy Only - Not suitable for VR projects", MessageType.Error);
#else
        EditorGUILayout.HelpBox("Unknown Status - Please check settings", MessageType.Error);
#endif

        GUILayout.Space(10);

        EditorGUILayout.LabelField("Required Packages:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("• Input System (com.unity.inputsystem)");
        EditorGUILayout.LabelField("• XR Plugin Management");
        EditorGUILayout.LabelField("• Oculus XR Plugin");
        
        GUILayout.Space(10);

        if (GUILayout.Button("Open Package Manager", GUILayout.Height(25)))
        {
            UnityEditor.PackageManager.UI.Window.Open("");
        }
    }
}
#endif 