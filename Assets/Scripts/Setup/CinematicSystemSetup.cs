#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// VR Horror Game - 영상 시스템 자동 설정 도구
/// 첫번째 영상 -> Main Scene -> 두번째 영상 -> Boss Battle -> 세번째 영상 플로우 설정
/// </summary>
public class CinematicSystemSetup : EditorWindow
{
    [MenuItem("VR Horror Game/Cinematic System/🎬 Setup Cinematic System")]
    public static void ShowWindow()
    {
        GetWindow<CinematicSystemSetup>("Cinematic System Setup");
    }

    private Vector2 scrollPosition;
    private bool showAdvancedOptions = false;

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("VR Horror Game - Cinematic System Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "이 도구는 VR Horror Game의 영상 시스템을 자동으로 설정합니다:\n\n" +
            "✅ CinematicManager (영상 재생 관리)\n" +
            "✅ GameProgressManager (게임 진행 상태 관리)\n" +
            "✅ 보스룸 입구 트리거 (두 번째 영상)\n" +
            "✅ 딸 구출 트리거 (세 번째 영상)\n" +
            "✅ 기존 BossAI와 연동", 
            MessageType.Info);

        GUILayout.Space(15);

        // 메인 설정 버튼
        if (GUILayout.Button("🎬 Complete Cinematic Setup", GUILayout.Height(50)))
        {
            SetupCompleteCinematicSystem();
        }

        GUILayout.Space(20);

        // 개별 설정 옵션들
        EditorGUILayout.LabelField("Individual Setup Options:", EditorStyles.boldLabel);
        
        if (GUILayout.Button("1. Setup Cinematic Manager", GUILayout.Height(30)))
        {
            SetupCinematicManager();
        }

        if (GUILayout.Button("2. Setup Game Progress Manager", GUILayout.Height(30)))
        {
            SetupGameProgressManager();
        }

        if (GUILayout.Button("3. Add Boss Room Trigger", GUILayout.Height(30)))
        {
            AddBossRoomTrigger();
        }

        if (GUILayout.Button("4. Add Daughter Rescue Trigger", GUILayout.Height(30)))
        {
            AddDaughterRescueTrigger();
        }

        if (GUILayout.Button("5. Connect to Existing Boss AI", GUILayout.Height(30)))
        {
            ConnectToBossAI();
        }

        GUILayout.Space(15);

        // 고급 옵션
        showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options");
        if (showAdvancedOptions)
        {
            EditorGUI.indentLevel++;
            
            if (GUILayout.Button("Create Video Folders", GUILayout.Height(25)))
            {
                CreateVideoFolders();
            }

            if (GUILayout.Button("Reset All Cinematic Progress", GUILayout.Height(25)))
            {
                ResetCinematicProgress();
            }

            if (GUILayout.Button("Test Cinematic Flow", GUILayout.Height(25)))
            {
                TestCinematicFlow();
            }

            EditorGUI.indentLevel--;
        }

        GUILayout.Space(15);

        // 현재 상태 표시
        DisplaySystemStatus();

        EditorGUILayout.EndScrollView();
    }

    #region Main Setup

    static void SetupCompleteCinematicSystem()
    {
        Debug.Log("🎬 VR Horror Game Cinematic System 설정 시작...");

        bool success = true;

        try
        {
            // 1. 필수 폴더 생성
            CreateRequiredFolders();

            // 2. CinematicManager 설정
            if (!SetupCinematicManager())
            {
                success = false;
            }

            // 3. GameProgressManager 설정
            if (!SetupGameProgressManager())
            {
                success = false;
            }

            // 4. 트리거들 추가
            if (!SetupAllTriggers())
            {
                success = false;
            }

            // 5. 기존 시스템과 연동
            if (!ConnectToExistingSystems())
            {
                success = false;
            }

            // 6. 필수 태그 설정
            SetupRequiredTags();

            if (success)
            {
                EditorUtility.DisplayDialog("Setup Complete", 
                    "VR Horror Game Cinematic System 설정 완료!\n\n" +
                    "✅ CinematicManager 생성\n" +
                    "✅ GameProgressManager 생성\n" +
                    "✅ 보스룸 트리거 추가\n" +
                    "✅ 딸 구출 트리거 추가\n" +
                    "✅ 기존 시스템과 연동\n\n" +
                    "이제 Inspector에서 영상 클립들을 할당하면 됩니다!", "확인");
            }
            else
            {
                EditorUtility.DisplayDialog("Setup Warning", 
                    "일부 설정에서 문제가 발생했습니다.\n" +
                    "Console 창을 확인해주세요.", "확인");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CinematicSystemSetup] 설정 중 오류 발생: {e.Message}");
            EditorUtility.DisplayDialog("Setup Error", 
                $"설정 중 오류가 발생했습니다:\n{e.Message}", "확인");
        }

        Debug.Log("🎬 VR Horror Game Cinematic System 설정 완료");
    }

    static void CreateRequiredFolders()
    {
        string[] folders = {
            "Assets/Videos",
            "Assets/Videos/Cinematics",
            "Assets/Prefabs/Cinematics"
        };

        foreach (string folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parentFolder = Path.GetDirectoryName(folder);
                string folderName = Path.GetFileName(folder);
                AssetDatabase.CreateFolder(parentFolder, folderName);
                Debug.Log($"[CinematicSystemSetup] 폴더 생성: {folder}");
            }
        }

        AssetDatabase.Refresh();
    }

    #endregion

    #region Individual Setup Methods

    static bool SetupCinematicManager()
    {
        Debug.Log("[CinematicSystemSetup] CinematicManager 설정 시작...");

        try
        {
            // 기존 CinematicManager 확인
            CinematicManager existingManager = FindFirstObjectByType<CinematicManager>();
            if (existingManager != null)
            {
                Debug.Log("[CinematicSystemSetup] CinematicManager가 이미 존재합니다.");
                return true;
            }

            // CinematicManager 오브젝트 생성
            GameObject managerObj = new GameObject("CinematicManager");
            CinematicManager manager = managerObj.AddComponent<CinematicManager>();

            // DontDestroyOnLoad 설정
            managerObj.transform.SetParent(null);

            // 변경사항 저장
            EditorUtility.SetDirty(managerObj);

            Debug.Log("[CinematicSystemSetup] CinematicManager 생성 완료!");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CinematicSystemSetup] CinematicManager 설정 실패: {e.Message}");
            return false;
        }
    }

    static bool SetupGameProgressManager()
    {
        Debug.Log("[CinematicSystemSetup] GameProgressManager 설정 시작...");

        try
        {
            // 기존 GameProgressManager 확인
            GameProgressManager existingManager = FindFirstObjectByType<GameProgressManager>();
            if (existingManager != null)
            {
                Debug.Log("[CinematicSystemSetup] GameProgressManager가 이미 존재합니다.");
                return true;
            }

            // GameProgressManager 오브젝트 생성
            GameObject managerObj = new GameObject("GameProgressManager");
            GameProgressManager manager = managerObj.AddComponent<GameProgressManager>();

            // DontDestroyOnLoad 설정
            managerObj.transform.SetParent(null);

            // 변경사항 저장
            EditorUtility.SetDirty(managerObj);

            Debug.Log("[CinematicSystemSetup] GameProgressManager 생성 완료!");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CinematicSystemSetup] GameProgressManager 설정 실패: {e.Message}");
            return false;
        }
    }

    static bool SetupAllTriggers()
    {
        bool success = true;
        
        if (!AddBossRoomTrigger())
        {
            success = false;
        }
        
        if (!AddDaughterRescueTrigger())
        {
            success = false;
        }
        
        return success;
    }

    static bool AddBossRoomTrigger()
    {
        Debug.Log("[CinematicSystemSetup] 보스룸 트리거 추가 시작...");

        try
        {
            // 기존 보스룸 트리거 확인
            CinematicTrigger[] existingTriggers = FindObjectsByType<CinematicTrigger>(FindObjectsSortMode.None);
            foreach (var trigger in existingTriggers)
            {
                if (trigger.cinematicType == CinematicManager.CinematicType.BossIntro)
                {
                    Debug.Log("[CinematicSystemSetup] 보스룸 트리거가 이미 존재합니다.");
                    return true;
                }
            }

            // 보스룸 트리거 생성
            GameObject triggerObj = new GameObject("BossRoom_CinematicTrigger");
            
            // Collider 설정
            BoxCollider triggerCollider = triggerObj.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(4f, 3f, 2f);

            // CinematicTrigger 컴포넌트 추가
            CinematicTrigger cinematicTrigger = triggerObj.AddComponent<CinematicTrigger>();
            cinematicTrigger.cinematicType = CinematicManager.CinematicType.BossIntro;

            // 적절한 위치에 배치 (보스룸 입구 추정 위치)
            triggerObj.transform.position = new Vector3(0, 1.5f, 10f); // 필요시 수동 조정

            // 변경사항 저장
            EditorUtility.SetDirty(triggerObj);

            Debug.Log("[CinematicSystemSetup] 보스룸 트리거 추가 완료!");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CinematicSystemSetup] 보스룸 트리거 추가 실패: {e.Message}");
            return false;
        }
    }

    static bool AddDaughterRescueTrigger()
    {
        Debug.Log("[CinematicSystemSetup] 딸 구출 트리거 추가 시작...");

        try
        {
            // 기존 딸 구출 트리거 확인
            CinematicTrigger[] existingTriggers = FindObjectsByType<CinematicTrigger>(FindObjectsSortMode.None);
            foreach (var trigger in existingTriggers)
            {
                if (trigger.cinematicType == CinematicManager.CinematicType.Ending)
                {
                    Debug.Log("[CinematicSystemSetup] 딸 구출 트리거가 이미 존재합니다.");
                    return true;
                }
            }

            // 딸 구출 트리거 생성
            GameObject triggerObj = new GameObject("DaughterRescue_CinematicTrigger");
            
            // Collider 설정
            SphereCollider triggerCollider = triggerObj.AddComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = 2f;

            // CinematicTrigger 컴포넌트 추가
            CinematicTrigger cinematicTrigger = triggerObj.AddComponent<CinematicTrigger>();
            cinematicTrigger.cinematicType = CinematicManager.CinematicType.Ending;

            // 적절한 위치에 배치 (딸이 있을 위치 추정)
            triggerObj.transform.position = new Vector3(0, 1f, 15f); // 필요시 수동 조정

            // 딸 오브젝트 생성 (시각적 표시용)
            CreateDaughterObject(triggerObj.transform.position);

            // 변경사항 저장
            EditorUtility.SetDirty(triggerObj);

            Debug.Log("[CinematicSystemSetup] 딸 구출 트리거 추가 완료!");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CinematicSystemSetup] 딸 구출 트리거 추가 실패: {e.Message}");
            return false;
        }
    }

    static void CreateDaughterObject(Vector3 position)
    {
        // 딸을 나타내는 임시 오브젝트 생성
        GameObject daughterObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        daughterObj.name = "Daughter_Placeholder";
        daughterObj.transform.position = position + Vector3.up * 0.5f;
        daughterObj.transform.localScale = new Vector3(0.5f, 1f, 0.5f);

        // 재질 설정 (핑크색으로 표시)
        Renderer renderer = daughterObj.GetComponent<Renderer>();
        Material daughterMaterial = new Material(Shader.Find("Standard"));
        daughterMaterial.color = Color.magenta;
        renderer.material = daughterMaterial;

        Debug.Log("[CinematicSystemSetup] 딸 플레이스홀더 오브젝트 생성 완료");
    }

    static bool ConnectToExistingSystems()
    {
        Debug.Log("[CinematicSystemSetup] 기존 시스템과 연동 시작...");

        try
        {
            bool success = true;

            // BossAI 연동
            if (!ConnectToBossAI())
            {
                Debug.LogWarning("[CinematicSystemSetup] BossAI 연동 실패 (BossAI가 없을 수 있음)");
            }

            // VolumeManager 연동 확인
            if (VolumeManager.Instance == null)
            {
                Debug.LogWarning("[CinematicSystemSetup] VolumeManager를 찾을 수 없습니다.");
            }
            else
            {
                Debug.Log("[CinematicSystemSetup] VolumeManager 연동 확인 완료");
            }

            return success;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CinematicSystemSetup] 기존 시스템 연동 실패: {e.Message}");
            return false;
        }
    }

    static bool ConnectToBossAI()
    {
        Debug.Log("[CinematicSystemSetup] BossAI 연동 시작...");

        try
        {
            BossAI bossAI = FindFirstObjectByType<BossAI>();
            if (bossAI == null)
            {
                Debug.LogWarning("[CinematicSystemSetup] BossAI를 찾을 수 없습니다.");
                return false;
            }

            Debug.Log("[CinematicSystemSetup] BossAI 연동 완료 - 수동으로 Die() 메서드에 GameProgressManager.Instance.NotifyBossDefeated() 추가 필요");

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CinematicSystemSetup] BossAI 연동 실패: {e.Message}");
            return false;
        }
    }

    #endregion

    #region Utility Methods

    static void SetupRequiredTags()
    {
        string[] requiredTags = {
            "Player",
            "CinematicTrigger", 
            "Daughter"
        };

        foreach (string tagName in requiredTags)
        {
            CreateTagIfNotExists(tagName);
        }
    }

    static bool CreateTagIfNotExists(string tagName)
    {
        // SerializedObject를 이용한 태그 생성
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        // 이미 존재하는지 확인
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(tagName)) 
            {
                return true; // 이미 존재함
            }
        }

        // 새 태그 추가
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
        newTagProp.stringValue = tagName;
        tagManager.ApplyModifiedProperties();

        Debug.Log($"[CinematicSystemSetup] 태그 생성: {tagName}");
        return true;
    }

    static void CreateVideoFolders()
    {
        string[] videoFolders = {
            "Assets/Videos/Intro",
            "Assets/Videos/BossIntro", 
            "Assets/Videos/Ending"
        };

        foreach (string folder in videoFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parentFolder = Path.GetDirectoryName(folder);
                string folderName = Path.GetFileName(folder);
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("[CinematicSystemSetup] 비디오 폴더 생성 완료");
    }

    static void ResetCinematicProgress()
    {
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.ResetGameProgress();
            Debug.Log("[CinematicSystemSetup] 게임 진행 상황 리셋 완료");
        }
        else
        {
            PlayerPrefs.DeleteAll();
            Debug.Log("[CinematicSystemSetup] PlayerPrefs 삭제 완료");
        }
    }

    static void TestCinematicFlow()
    {
        Debug.Log("[CinematicSystemSetup] 시네마틱 플로우 테스트 시작");
        
        if (CinematicManager.Instance != null)
        {
            // 인트로 영상 테스트 재생
            CinematicManager.Instance.PlayCinematic(CinematicManager.CinematicType.Intro);
        }
        else
        {
            Debug.LogWarning("[CinematicSystemSetup] CinematicManager를 찾을 수 없습니다!");
        }
    }

    #endregion

    #region Status Display

    void DisplaySystemStatus()
    {
        EditorGUILayout.LabelField("Current System Status:", EditorStyles.boldLabel);

        // CinematicManager 상태
        CinematicManager cinematicManager = FindFirstObjectByType<CinematicManager>();
        EditorGUILayout.LabelField("CinematicManager:", cinematicManager != null ? "✅ 설치됨" : "❌ 없음");

        // GameProgressManager 상태
        GameProgressManager progressManager = FindFirstObjectByType<GameProgressManager>();
        EditorGUILayout.LabelField("GameProgressManager:", progressManager != null ? "✅ 설치됨" : "❌ 없음");

        // 트리거들 상태
        CinematicTrigger[] triggers = FindObjectsByType<CinematicTrigger>(FindObjectsSortMode.None);
        int bossIntroTriggers = 0;
        int endingTriggers = 0;

        foreach (var trigger in triggers)
        {
            if (trigger.cinematicType == CinematicManager.CinematicType.BossIntro)
                bossIntroTriggers++;
            else if (trigger.cinematicType == CinematicManager.CinematicType.Ending)
                endingTriggers++;
        }

        EditorGUILayout.LabelField("Boss Intro Trigger:", bossIntroTriggers > 0 ? $"✅ {bossIntroTriggers}개" : "❌ 없음");
        EditorGUILayout.LabelField("Ending Trigger:", endingTriggers > 0 ? $"✅ {endingTriggers}개" : "❌ 없음");

        // 기존 시스템 상태
        VolumeManager volumeManager = FindFirstObjectByType<VolumeManager>();
        EditorGUILayout.LabelField("VolumeManager:", volumeManager != null ? "✅ 연동 가능" : "❌ 없음");

        BossAI bossAI = FindFirstObjectByType<BossAI>();
        EditorGUILayout.LabelField("BossAI:", bossAI != null ? "✅ 연동 가능" : "❌ 없음");

        if (progressManager != null)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Game Progress:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Current State:", progressManager.CurrentState.ToString());
            EditorGUILayout.LabelField("Has Seen Intro:", progressManager.HasSeenIntro ? "✅" : "❌");
            EditorGUILayout.LabelField("Has Seen Boss Intro:", progressManager.HasSeenBossIntro ? "✅" : "❌");
            EditorGUILayout.LabelField("Boss Defeated:", progressManager.IsBossDefeated ? "✅" : "❌");
            EditorGUILayout.LabelField("Daughter Rescued:", progressManager.IsDaughterRescued ? "✅" : "❌");
        }
    }

    #endregion
}
#endif 