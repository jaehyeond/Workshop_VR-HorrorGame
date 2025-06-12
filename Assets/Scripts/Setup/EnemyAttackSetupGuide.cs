using UnityEngine;
using UnityEditor;

/// <summary>
/// Enemy Attack 시스템 설정 가이드
/// 
/// VR 호러 게임에서 Enemy가 Attack1으로 플레이어를 공격할 때 
/// Meta Quest 헤드셋이 빨갛게 변하는 시스템을 완성하기 위한 설정 가이드
/// 
/// 필요한 설정:
/// 1. Enemy 프리팹에 EnemyAttackSystem 컴포넌트 추가
/// 2. Attack1 애니메이션에 Animation Event 추가
/// 3. 플레이어(VR Camera)에 VRPlayerHealth 컴포넌트 추가
/// 4. VRPostProcessingManager가 활성화되어 있는지 확인
/// 
/// 결과:
/// - Enemy Attack1 → 플레이어 데미지 → VR 화면이 빨갛게 변함 + 햅틱 피드백
/// </summary>

#if UNITY_EDITOR
[System.Serializable]
public class EnemyAttackSetupGuide
{
    [Header("Enemy Attack 시스템 설정 가이드")]
    [TextArea(10, 20)]
    public string guideText = @"VR Enemy Attack 시스템 설정 가이드

1. Enemy 프리팹 설정:
   - Fanatic Enemy, Main Boss, Priest 등 모든 Enemy 프리팹 선택
   - Inspector에서 'Add Component' → 'EnemyAttackSystem' 추가
   - Attack Damage: 25 (권장값)
   - Attack Range: 2.5 (권장값)

2. Attack1 애니메이션에 Animation Event 추가:
   - Enemy의 Animator Controller 열기
   - Attack1 애니메이션 클립 선택
   - Animation 창에서 타격 순간(보통 50~70% 지점)에 Event 추가
   - Function: OnAttack1Hit
   - 이벤트가 EnemyAttackSystem.OnAttack1Hit() 함수를 호출함

3. 플레이어 VR 설정:
   - VR Camera(OVRCameraRig) 오브젝트 선택
   - Inspector에서 'Add Component' → 'VRPlayerHealth' 추가
   - Max Health: 100 (권장값)
   - Damage Effect Duration: 1.5초 (권장값)

4. Post Processing 확인:
   - VRPostProcessingManager가 씬에 있는지 확인
   - Global Volume이 활성화되어 있는지 확인

완료 후 결과:
   Enemy가 Attack1으로 플레이어를 때릴 때마다
   → VR 헤드셋 화면이 빨갛게 변함
   → 양손 컨트롤러에 햅틱 피드백
   → 체력에 따른 동적 Post Processing 효과

주의사항:
   - 모든 Enemy 프리팹에 EnemyAttackSystem 추가 필요
   - Animation Event는 각 Enemy의 Attack1 애니메이션마다 개별 설정
   - VRPlayerHealth는 플레이어 오브젝트에만 한 번만 추가";

    [Header("자동 설정 도구")]
    public bool setupAllEnemyPrefabs = false;
    public bool setupPlayerHealth = false;
    public bool validateSetup = false;
}

/// <summary>
/// Editor 창에서 Enemy Attack 설정을 도와주는 도구
/// </summary>
public class EnemyAttackSetupWindow : EditorWindow
{
    private EnemyAttackSetupGuide guide = new EnemyAttackSetupGuide();
    private Vector2 scrollPosition;

    [MenuItem("Window/VR Horror Game/Enemy Attack Setup")]
    public static void ShowWindow()
    {
        GetWindow<EnemyAttackSetupWindow>("Enemy Attack Setup");
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("VR Enemy Attack 설정 도구", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 가이드 텍스트
        EditorGUILayout.LabelField("설정 가이드:", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(guide.guideText, GUILayout.Height(300));
        
        GUILayout.Space(20);
        
        // 자동 설정 버튼들
        EditorGUILayout.LabelField("자동 설정 도구:", EditorStyles.boldLabel);
        
        if (GUILayout.Button("1. 모든 Enemy 프리팹에 EnemyAttackSystem 추가", GUILayout.Height(30)))
        {
            AutoSetupEnemyPrefabs();
        }
        
        if (GUILayout.Button("2. VR 플레이어에 VRPlayerHealth 추가", GUILayout.Height(30)))
        {
            AutoSetupPlayerHealth();
        }
        
        if (GUILayout.Button("3. 현재 설정 상태 검증", GUILayout.Height(30)))
        {
            ValidateCurrentSetup();
        }
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox("Animation Event는 수동으로 추가해야 합니다!\nEnemy의 Attack1 애니메이션에서 타격 순간에 OnAttack1Hit 이벤트를 추가하세요.", MessageType.Warning);
        
        if (GUILayout.Button("Animation Event 설정 가이드 열기", GUILayout.Height(25)))
        {
            Application.OpenURL("https://docs.unity3d.com/Manual/script-AnimationWindowEvent.html");
        }

        EditorGUILayout.EndScrollView();
    }

    private void AutoSetupEnemyPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int setupCount = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null && IsEnemyPrefab(prefab))
            {
                GameObject prefabInstance = PrefabUtility.LoadPrefabContents(path);
                
                if (prefabInstance.GetComponent<EnemyAttackSystem>() == null)
                {
                    prefabInstance.AddComponent<EnemyAttackSystem>();
                    PrefabUtility.SaveAsPrefabAsset(prefabInstance, path);
                    setupCount++;
                    Debug.Log($"[EnemyAttackSetup] EnemyAttackSystem 추가됨: {prefab.name}");
                }
                
                PrefabUtility.UnloadPrefabContents(prefabInstance);
            }
        }

        EditorUtility.DisplayDialog("완료", $"{setupCount}개의 Enemy 프리팹에 EnemyAttackSystem을 추가했습니다.", "확인");
    }

    private bool IsEnemyPrefab(GameObject prefab)
    {
        string name = prefab.name.ToLower();
        return name.Contains("enemy") || name.Contains("fanatic") || name.Contains("boss") || 
               name.Contains("priest") || name.Contains("cultist") || 
               prefab.GetComponent<CultistAI>() != null;
    }

    private void AutoSetupPlayerHealth()
    {
        // OVRCameraRig 찾기
        OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
        if (cameraRig != null)
        {
            if (cameraRig.GetComponent<VRPlayerHealth>() == null)
            {
                cameraRig.gameObject.AddComponent<VRPlayerHealth>();
                Debug.Log("[EnemyAttackSetup] VRPlayerHealth 추가됨: " + cameraRig.name);
                EditorUtility.DisplayDialog("완료", "VR 플레이어에 VRPlayerHealth를 추가했습니다.", "확인");
            }
            else
            {
                EditorUtility.DisplayDialog("알림", "VRPlayerHealth가 이미 추가되어 있습니다.", "확인");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("오류", "OVRCameraRig를 찾을 수 없습니다.\n씬에 VR 플레이어가 있는지 확인하세요.", "확인");
        }
    }

    private void ValidateCurrentSetup()
    {
        string report = "VR Enemy Attack 시스템 검증 결과:\n\n";
        
        // 1. Enemy 프리팹 체크
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int enemyPrefabCount = 0;
        int setupPrefabCount = 0;
        
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null && IsEnemyPrefab(prefab))
            {
                enemyPrefabCount++;
                if (prefab.GetComponent<EnemyAttackSystem>() != null)
                {
                    setupPrefabCount++;
                }
            }
        }
        
        report += $"Enemy 프리팹: {setupPrefabCount}/{enemyPrefabCount} 설정됨\n";
        
        // 2. VR 플레이어 체크
        OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
        VRPlayerHealth playerHealth = FindFirstObjectByType<VRPlayerHealth>();
        
        report += $"VR 플레이어: {(cameraRig != null ? "발견" : "없음")}\n";
        report += $"VRPlayerHealth: {(playerHealth != null ? "설정됨" : "필요함")}\n";
        
        // 3. Post Processing 체크
        VRPostProcessingManager postProcessing = FindFirstObjectByType<VRPostProcessingManager>();
        report += $"Post Processing: {(postProcessing != null ? "활성화됨" : "필요함")}\n";
        
        // 결과 출력
        report += "\n" + (setupPrefabCount == enemyPrefabCount && playerHealth != null && postProcessing != null 
            ? "모든 설정이 완료되었습니다!" 
            : "일부 설정이 누락되었습니다.");
            
        EditorUtility.DisplayDialog("검증 결과", report, "확인");
        Debug.Log("[EnemyAttackSetup] " + report.Replace("\n", " | "));
    }
}
#endif 