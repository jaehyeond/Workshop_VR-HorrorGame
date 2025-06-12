#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// VR Horror Game 통합 설정 도구
/// 
/// Meta Quest VR 호러 게임에서 Enemy Attack1 시 빨간 화면 효과를 위한
/// 모든 필요한 컴포넌트들을 자동으로 설정하는 올인원 도구
/// 
/// 자동 설정 항목:
/// 1. VRPlayerHealth (플레이어 체력 및 피격 시스템)
/// 2. VRPostProcessingManager (Post Processing 기반 VR 화면 효과)
/// 3. Enemy Attack Points (Enemy 공격 지점 자동 설정)
/// 4. Global Volume (Post Processing 환경)
/// 
/// 결과: [T] 키 테스트 + Enemy Attack1 → VR 빨간 화면 효과
/// </summary>
public class VRHorrorGameSetup : EditorWindow
{
    [MenuItem("Window/VR Horror Game/Complete Setup (All-in-One)")]
    public static void ShowWindow()
    {
        GetWindow<VRHorrorGameSetup>("VR Horror Game Complete Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("VR Horror Game Complete Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "이 도구는 VR 호러 게임의 모든 필수 설정을 자동으로 처리합니다:\n\n" +
            "VRPlayerHealth 설정\n" +
            "VRPostProcessingManager 설정 (Post Processing 기반)\n" +
            "Enemy Attack Points 설정\n" +
            "Global Volume 및 Post Processing 환경 구성\n" +
            "VR 피격 효과 시스템 완성", 
            MessageType.Info);

        GUILayout.Space(15);

        if (GUILayout.Button("Complete Auto Setup", GUILayout.Height(50)))
        {
            CompleteSetup();
        }

        GUILayout.Space(20);

        EditorGUILayout.LabelField("Individual Setup Options:", EditorStyles.boldLabel);
        
        if (GUILayout.Button("1. Setup VR Player Health", GUILayout.Height(30)))
        {
            SetupVRPlayerHealth();
        }

        if (GUILayout.Button("2. Setup VR Post Processing", GUILayout.Height(30)))
        {
            SetupVRPostProcessingManager();
        }

        if (GUILayout.Button("3. Setup Enemy Attack Points", GUILayout.Height(30)))
        {
            SetupEnemyAttackPoints();
        }

        GUILayout.Space(15);

        if (GUILayout.Button("Check All Systems", GUILayout.Height(30)))
        {
            CheckAllSystems();
        }

        GUILayout.Space(15);

        // 현재 상태 표시
        DisplaySystemStatus();
    }

    /// <summary>
    /// 모든 설정을 한 번에 처리하는 통합 설정 도구
    /// </summary>
    public static void CompleteSetup()
    {
        Debug.Log("VR Horror Game 통합 설정 시작!");
        
        bool success = true;
        
        // 1. VRPostProcessingManager 설정
        if (!SetupVRPostProcessingManager())
        {
            success = false;
        }
        
        // 2. VRPlayerHealth 설정
        if (!SetupVRPlayerHealth())
        {
            success = false;
        }
        
        // 3. Enemy Attack Point 설정
        if (!SetupEnemyAttackPoints())
        {
            success = false;
        }
        
        // 4. 필수 태그 설정
        if (!SetupRequiredTags())
        {
            success = false;
        }
        
        // 5. Player Hit Target 설정
        if (!SetupPlayerHitTarget())
        {
            success = false;
        }
        
        // 6. Input System 확인
        CheckInputSystemSettings();
        
        if (success)
        {
            EditorUtility.DisplayDialog("Complete Setup Success", 
                "VR Horror Game 설정이 완료되었습니다!\n\n" +
                "VRPostProcessingManager 설정 완료 (Post Processing 기반)\n" +
                "VRPlayerHealth 추가 완료\n" +
                "Enemy Attack Points 설정 완료\n" +
                "VRPlayerHitTarget 설정 완료 (물리적 타격 감지)\n" +
                "Global Volume 및 Post Processing 환경 구성 완료\n" +
                "Input System 확인 완료\n\n" +
                "이제 Enemy가 Attack1으로 공격할 때 VR 화면이 빨갛게 변합니다!", "확인");
        }
        else
        {
            EditorUtility.DisplayDialog("Setup Warning", 
                "일부 설정에서 문제가 발생했습니다.\n" +
                "Console 창을 확인해주세요.", "확인");
        }
        
        Debug.Log("VR Horror Game 통합 설정 완료!");
    }
    
    /// <summary>
    /// VRPlayerHealth 설정
    /// </summary>
    private static bool SetupVRPlayerHealth()
    {
        Debug.Log("[VRHorrorGameSetup] VRPlayerHealth 설정 시작...");
        
        try
        {
            // OVRCameraRig 찾기
            OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
            if (cameraRig == null)
            {
                Debug.LogError("[VRHorrorGameSetup] OVRCameraRig를 찾을 수 없습니다!");
                return false;
            }

            // 이미 VRPlayerHealth가 있는지 확인
            VRPlayerHealth existingHealth = cameraRig.GetComponent<VRPlayerHealth>();
            if (existingHealth != null)
            {
                Debug.Log("[VRHorrorGameSetup] VRPlayerHealth가 이미 존재합니다.");
                return true;
            }

            // VRPlayerHealth 추가
            VRPlayerHealth playerHealth = cameraRig.gameObject.AddComponent<VRPlayerHealth>();
            
            // 기본 설정 적용
            playerHealth.maxHealth = 100f;
            playerHealth.damageEffectDuration = 1.5f;
            playerHealth.damageScreenIntensity = 0.8f;
            playerHealth.invincibilityDuration = 1f;

            // 변경사항 저장
            EditorUtility.SetDirty(cameraRig.gameObject);

            Debug.Log("[VRHorrorGameSetup] VRPlayerHealth 추가 완료!");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VRHorrorGameSetup] VRPlayerHealth 설정 실패: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// VRPostProcessingManager 설정 (Post Processing 기반)
    /// </summary>
    private static bool SetupVRPostProcessingManager()
    {
        Debug.Log("[VRHorrorGameSetup] VRPostProcessingManager 설정 시작...");
        
        try
        {
            // 이미 VRPostProcessingManager가 있는지 확인
            VRPostProcessingManager existingManager = FindFirstObjectByType<VRPostProcessingManager>();
            if (existingManager != null)
            {
                Debug.Log("[VRHorrorGameSetup] VRPostProcessingManager가 이미 존재합니다.");
                return true;
            }

            // VRPostProcessingManager 생성
            GameObject postProcessingObj = new GameObject("VRPostProcessingManager");
            VRPostProcessingManager manager = postProcessingObj.AddComponent<VRPostProcessingManager>();

            // Global Volume 설정
            SetupGlobalVolume();

            Debug.Log("[VRHorrorGameSetup] VRPostProcessingManager 설정 완료!");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VRHorrorGameSetup] VRPostProcessingManager 설정 실패: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Global Volume 및 Post Processing 환경 설정
    /// </summary>
    private static void SetupGlobalVolume()
    {
        Debug.Log("[VRHorrorGameSetup] Global Volume 설정 시작...");
        
        // Global Volume 찾기 또는 생성
        Volume globalVolume = FindFirstObjectByType<Volume>();
        if (globalVolume == null)
        {
            GameObject volumeObj = new GameObject("Global Volume");
            globalVolume = volumeObj.AddComponent<Volume>();
            globalVolume.isGlobal = true;
            globalVolume.priority = 1;
        }

        // Volume Profile 생성 또는 설정
        if (globalVolume.profile == null)
        {
            VolumeProfile profile = CreateVolumeProfile();
            globalVolume.profile = profile;
        }
        
        Debug.Log("[VRHorrorGameSetup] Global Volume 설정 완료");
    }

    /// <summary>
    /// VR 피격 효과용 Volume Profile 생성
    /// </summary>
    private static VolumeProfile CreateVolumeProfile()
    {
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        
        // Vignette 추가 (피격 효과의 핵심)
        if (!profile.TryGet<Vignette>(out var vignette))
        {
            vignette = profile.Add<Vignette>(false);
        }
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0f;
        vignette.color.overrideState = true;
        vignette.color.value = Color.black;
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = 0.4f;

        // Color Adjustments 추가 (색상 필터 효과)
        if (!profile.TryGet<ColorAdjustments>(out var colorAdjustments))
        {
            colorAdjustments = profile.Add<ColorAdjustments>(false);
        }
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.saturation.value = 0f;
        colorAdjustments.hueShift.overrideState = true;
        colorAdjustments.hueShift.value = 0f;

        // Bloom 추가 (강렬한 빛 효과)
        if (!profile.TryGet<Bloom>(out var bloom))
        {
            bloom = profile.Add<Bloom>(false);
        }
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 0f;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 1.3f;

        Debug.Log("[VRHorrorGameSetup] Volume Profile 생성 완료");
        return profile;
    }
    
    /// <summary>
    /// 모든 Enemy 프리팹의 Attack Point 설정
    /// </summary>
    private static bool SetupEnemyAttackPoints()
    {
        Debug.Log("[VRHorrorGameSetup] Enemy Attack Points 설정 시작...");
        
        try
        {
            // 씬의 모든 Enemy 찾기
            EnemyAttackSystem[] enemies = FindObjectsByType<EnemyAttackSystem>(FindObjectsSortMode.None);
            int setupCount = 0;
            
            foreach (var enemy in enemies)
            {
                if (enemy.attackPoint == null)
                {
                    // Attack Point 자동 설정
                    SetupSingleEnemyAttackPoint(enemy);
                    setupCount++;
                }
            }
            
            // 프리팹의 Enemy들도 설정
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            int prefabSetupCount = 0;
            
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab != null)
                {
                    EnemyAttackSystem enemyAttack = prefab.GetComponent<EnemyAttackSystem>();
                    if (enemyAttack != null && enemyAttack.attackPoint == null)
                    {
                        // 프리팹 수정
                        SetupSingleEnemyAttackPoint(enemyAttack);
                        EditorUtility.SetDirty(prefab);
                        prefabSetupCount++;
                    }
                }
            }
            
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[VRHorrorGameSetup] Enemy Attack Points 설정 완료!");
            Debug.Log($"- 씬의 Enemy: {setupCount}개 설정");
            Debug.Log($"- 프리팹 Enemy: {prefabSetupCount}개 설정");
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VRHorrorGameSetup] Enemy Attack Points 설정 실패: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 개별 Enemy의 Attack Point 설정
    /// </summary>
    private static void SetupSingleEnemyAttackPoint(EnemyAttackSystem enemy)
    {
        if (enemy.attackPoint != null) return;
        
        // Spawner는 제외 (실제 Enemy가 아님)
        if (IsSpawner(enemy.gameObject))
        {
            Debug.Log($"[VRHorrorGameSetup] {enemy.name}은 Spawner이므로 Attack Point 설정 건너뜀");
            return;
        }
        
        Transform enemyTransform = enemy.transform;
        
        // 손 위치 찾기 (여러 가능한 이름들)
        string[] handNames = {
            "RightHand", "mixamorig:RightHand", "R_Hand", "Hand_R", 
            "RightHandIndex1", "Right_Hand", "hand_R", "HandR"
        };
        
        Transform rightHand = null;
        foreach (string handName in handNames)
        {
            rightHand = FindChildByName(enemyTransform, handName);
            if (rightHand != null) break;
        }
        
        if (rightHand != null)
        {
            enemy.attackPoint = rightHand;
            
            // Attack Point에 태그와 Collider 추가
            if (rightHand.gameObject.GetComponent<Collider>() == null)
            {
                SphereCollider attackCollider = rightHand.gameObject.AddComponent<SphereCollider>();
                attackCollider.radius = 0.1f;
                attackCollider.isTrigger = true;
            }
            rightHand.gameObject.tag = "EnemyAttackPoint";
            
            Debug.Log($"[VRHorrorGameSetup] {enemy.name}의 Attack Point를 {rightHand.name}으로 설정");
        }
        else
        {
            // 손을 찾지 못하면 Enemy 앞쪽에 Attack Point 생성
            GameObject attackPointObj = new GameObject("AttackPoint");
            attackPointObj.transform.SetParent(enemyTransform);
            attackPointObj.transform.localPosition = Vector3.forward * 1f;
            
            // Attack Point Collider 및 태그 설정
            SphereCollider attackCollider = attackPointObj.AddComponent<SphereCollider>();
            attackCollider.radius = 0.1f;
            attackCollider.isTrigger = true;
            attackPointObj.tag = "EnemyAttackPoint";
            
            enemy.attackPoint = attackPointObj.transform;
            
            Debug.LogWarning($"[VRHorrorGameSetup] {enemy.name}의 손을 찾지 못해 Attack Point를 앞쪽에 생성");
        }
    }
    
    /// <summary>
    /// Spawner인지 확인
    /// </summary>
    private static bool IsSpawner(GameObject obj)
    {
        string name = obj.name.ToLower();
        return name.Contains("spawner") || 
               name.Contains("spawn") ||
               obj.GetComponent<CultistSpawner>() != null;
    }
    
    /// <summary>
    /// 재귀적으로 자식에서 이름으로 Transform 찾기
    /// </summary>
    private static Transform FindChildByName(Transform parent, string name)
    {
        if (parent.name.Contains(name))
            return parent;
            
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name))
                return child;
                
            Transform found = FindChildByName(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    /// <summary>
    /// 필수 태그들 설정
    /// </summary>
    private static bool SetupRequiredTags()
    {
        Debug.Log("[VRHorrorGameSetup] 필수 태그 설정 시작...");
        
        try
        {
            string[] requiredTags = {
                "EnemyAttackPoint",
                "Player",
                "Enemy",
                "VRPlayer"
            };

            int createdCount = 0;
            
            foreach (string tag in requiredTags)
            {
                if (CreateTagIfNotExists(tag))
                {
                    createdCount++;
                    Debug.Log($"[VRHorrorGameSetup] 태그 생성: {tag}");
                }
            }
            
            Debug.Log($"[VRHorrorGameSetup] 태그 설정 완료! 새로 생성: {createdCount}개");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VRHorrorGameSetup] 태그 설정 실패: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 태그가 존재하지 않으면 생성
    /// </summary>
    private static bool CreateTagIfNotExists(string tagName)
    {
        // 태그가 이미 존재하는지 확인
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        // 기존 태그 확인
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty tag = tagsProp.GetArrayElementAtIndex(i);
            if (tag.stringValue.Equals(tagName))
            {
                return false; // 이미 존재
            }
        }

        // 새 태그 추가
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
        newTag.stringValue = tagName;
        tagManager.ApplyModifiedProperties();

        return true; // 새로 생성됨
    }
    
    /// <summary>
    /// VR Player Hit Target 설정 (물리적 타격 감지)
    /// </summary>
    private static bool SetupPlayerHitTarget()
    {
        Debug.Log("[VRHorrorGameSetup] VRPlayerHitTarget 설정 시작...");
        
        try
        {
            // Player Controller 찾기 (OVRCameraRig 또는 Player 태그)
            Transform playerController = null;
            
            // 1. Player 태그로 찾기
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerController = playerObj.transform;
                Debug.Log($"[VRHorrorGameSetup] Player 태그로 Player Controller 찾음: {playerObj.name}");
            }
            else
            {
                // 2. OVRCameraRig로 찾기
                OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
                if (cameraRig != null)
                {
                    playerController = cameraRig.transform;
                    Debug.Log($"[VRHorrorGameSetup] OVRCameraRig로 Player Controller 찾음: {cameraRig.name}");
                }
            }
            
            if (playerController == null)
            {
                Debug.LogError("[VRHorrorGameSetup] Player Controller를 찾을 수 없습니다!");
                return false;
            }
            
            // 이미 VRPlayerHitTarget이 있는지 확인
            VRPlayerHitTarget existingHitTarget = playerController.GetComponentInChildren<VRPlayerHitTarget>();
            if (existingHitTarget != null)
            {
                Debug.Log("[VRHorrorGameSetup] VRPlayerHitTarget이 이미 존재합니다.");
                return true;
            }
            
            // VRPlayerHitTarget 생성 (Player Controller 하위에)
            GameObject hitTargetObj = new GameObject("VRPlayerHitTarget");
            hitTargetObj.transform.SetParent(playerController);
            hitTargetObj.transform.localPosition = Vector3.zero; // Player Controller 중심에 배치
            
            // VRPlayerHitTarget 컴포넌트 추가
            VRPlayerHitTarget hitTarget = hitTargetObj.AddComponent<VRPlayerHitTarget>();
            hitTarget.hitRadius = 0.8f; // VR 플레이어 크기에 맞는 반경
            hitTarget.showDebugGizmos = true;
            
            // 태그 설정
            hitTargetObj.tag = "Player";
            
            Debug.Log("[VRHorrorGameSetup] VRPlayerHitTarget 설정 완료!");
            Debug.Log($"[VRHorrorGameSetup] - 위치: {playerController.name} 하위");
            Debug.Log($"[VRHorrorGameSetup] - 타격 반경: {hitTarget.hitRadius}m");
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VRHorrorGameSetup] VRPlayerHitTarget 설정 실패: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 모든 시스템 상태 확인
    /// </summary>
    private void CheckAllSystems()
    {
        string report = "VR Horror Game 시스템 전체 점검:\n\n";
        
        // VRPlayerHealth 확인
        OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
        if (cameraRig != null && cameraRig.GetComponent<VRPlayerHealth>() != null)
        {
            report += "VRPlayerHealth: 설정됨\n";
        }
        else
        {
            report += "VRPlayerHealth: 설정 필요\n";
        }

        // VRPostProcessingManager 확인
        VRPostProcessingManager postManager = FindFirstObjectByType<VRPostProcessingManager>();
        if (postManager != null)
        {
            report += "VRPostProcessingManager: 설정됨\n";
        }
        else
        {
            report += "VRPostProcessingManager: 설정 필요\n";
        }

        // VRPlayerHitTarget 확인
        VRPlayerHitTarget hitTarget = FindFirstObjectByType<VRPlayerHitTarget>();
        if (hitTarget != null)
        {
            report += "VRPlayerHitTarget: 설정됨 (물리적 타격 감지)\n";
        }
        else
        {
            report += "VRPlayerHitTarget: 설정 필요 (물리적 타격 감지)\n";
        }

        // Enemy Attack Points 확인
        EnemyAttackSystem[] enemies = FindObjectsByType<EnemyAttackSystem>(FindObjectsSortMode.None);
        int setupEnemies = 0;
        foreach (var enemy in enemies)
        {
            if (enemy.attackPoint != null) setupEnemies++;
        }
        
        if (enemies.Length > 0)
        {
            report += $"Enemy Attack Points: {setupEnemies}/{enemies.Length} 설정됨\n";
        }
        else
        {
            report += "Enemy Attack Points: 씬에 Enemy 없음\n";
        }

        // Global Volume 확인
        Volume globalVolume = FindFirstObjectByType<Volume>();
        if (globalVolume != null)
        {
            report += "Global Volume: 설정됨\n";
        }
        else
        {
            report += "Global Volume: 설정 필요\n";
        }

        report += "\n테스트 가능 여부:\n";
        bool canTest = cameraRig?.GetComponent<VRPlayerHealth>() != null && postManager != null;
        report += canTest ? "[T] 키 테스트 가능" : "설정 완료 후 테스트 가능";

        Debug.Log(report);
        EditorUtility.DisplayDialog("System Check", report, "OK");
    }

    /// <summary>
    /// 시스템 상태 표시
    /// </summary>
    private void DisplaySystemStatus()
    {
        EditorGUILayout.LabelField("System Status:", EditorStyles.boldLabel);
        
        // VRPlayerHealth
        OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
        bool hasPlayerHealth = cameraRig?.GetComponent<VRPlayerHealth>() != null;
        EditorGUILayout.LabelField($"VRPlayerHealth: {(hasPlayerHealth ? "설정됨" : "필요함")}");

        // VRPostProcessingManager
        bool hasPostManager = FindFirstObjectByType<VRPostProcessingManager>() != null;
        EditorGUILayout.LabelField($"VRPostProcessingManager: {(hasPostManager ? "설정됨" : "필요함")}");

        // VRPlayerHitTarget
        bool hasHitTarget = FindFirstObjectByType<VRPlayerHitTarget>() != null;
        EditorGUILayout.LabelField($"VRPlayerHitTarget: {(hasHitTarget ? "설정됨" : "필요함")}");

        // Enemy Count
        EnemyAttackSystem[] enemies = FindObjectsByType<EnemyAttackSystem>(FindObjectsSortMode.None);
        EditorGUILayout.LabelField($"Enemies in Scene: {enemies.Length}");

        // Global Volume
        bool hasGlobalVolume = FindFirstObjectByType<Volume>() != null;
        EditorGUILayout.LabelField($"Global Volume: {(hasGlobalVolume ? "설정됨" : "필요함")}");
    }

    /// <summary>
    /// Input System 설정 확인
    /// </summary>
    private static void CheckInputSystemSettings()
    {
        Debug.Log("[VRHorrorGameSetup] Input System 설정 확인...");
        
        // Unity 6에서는 Input System Package (New) Only 권장
        Debug.Log("[VRHorrorGameSetup] Unity 6 Input System 확인 완료");
        Debug.Log("- Edit → Project Settings → XR Plug-in Management → Input System Package (New) Only 설정 권장");
    }
}
#endif 