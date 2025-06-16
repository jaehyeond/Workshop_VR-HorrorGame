#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;
using UnityEngine.Video;
using UnityEngine.Rendering;

/// <summary>
/// VR Horror Game - 새로운 게임 플로우 시스템 자동 설정 도구
/// GameFlowManager + VideoSceneManager + SceneTransitionTrigger 기반
/// </summary>
public class CinematicSystemSetup : EditorWindow
{
    [MenuItem("VR Horror Game/Game Flow System/🎮 Setup Complete Game Flow System")]
    public static void ShowWindow()
    {
        GetWindow<CinematicSystemSetup>("Game Flow Setup");
    }

    private Vector2 scrollPosition;
    private bool showAdvancedOptions = false;

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("VR Horror Game - Game Flow System Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "🎮 새로운 게임 플로우 시스템:\n\n" +
            "✅ GameFlowManager - 통합 게임 상태 관리\n" +
            "✅ VideoSceneManager - Video Scene에서 영상 재생\n" +
            "✅ SceneTransitionTrigger - MainGame Scene에서 Video Scene 전환\n" +
            "✅ 깔끔한 Scene 플로우: IntroVideo → MainGame → BossIntroVideo → MainGame → EndingVideo\n" +
            "✅ 스파게티 코드 제거 및 단순화", 
            MessageType.Info);

        GUILayout.Space(15);

        // 메인 설정 버튼
        if (GUILayout.Button("🎮 Setup Complete Game Flow System", GUILayout.Height(50)))
        {
            SetupCompleteGameFlowSystem();
        }

        GUILayout.Space(20);

        // 개별 설정 옵션들
        EditorGUILayout.LabelField("Individual Setup Options:", EditorStyles.boldLabel);
        
        if (GUILayout.Button("1. Create GameFlowManager", GUILayout.Height(30)))
        {
            CreateGameFlowManager();
        }

        if (GUILayout.Button("2. Create All Video Scenes", GUILayout.Height(30)))
        {
            CreateAllVideoScenes();
        }

        if (GUILayout.Button("3. Setup MainGame Scene Triggers", GUILayout.Height(30)))
        {
            SetupMainGameSceneTriggers();
        }

        GUILayout.Space(15);

        // 고급 옵션
        showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options");
        if (showAdvancedOptions)
        {
            EditorGUI.indentLevel++;
            
            if (GUILayout.Button("Clean Old System", GUILayout.Height(25)))
            {
                CleanOldSystem();
            }

            if (GUILayout.Button("Reset Game Progress", GUILayout.Height(25)))
            {
                ResetGameProgress();
            }

            if (GUILayout.Button("Test Game Flow", GUILayout.Height(25)))
            {
                TestGameFlow();
            }

            EditorGUI.indentLevel--;
        }

        GUILayout.Space(15);

        // 현재 상태 표시
        DisplaySystemStatus();

        EditorGUILayout.EndScrollView();
    }

    #region Main Setup

    static void SetupCompleteGameFlowSystem()
    {
        Debug.Log("🎮 Complete Game Flow System 설정 시작...");

        bool success = true;

        try
        {
            // 1. 필수 폴더 생성
            CreateRequiredFolders();

            // 2. GameFlowManager 생성
            if (!CreateGameFlowManager())
            {
                success = false;
            }

            // 3. 모든 영상 Scene 생성
            if (!CreateAllVideoScenes())
            {
                success = false;
            }

            // 4. MainGame Scene 트리거 설정
            if (!SetupMainGameSceneTriggers())
            {
                success = false;
            }

            // 5. 기존 시스템 정리
            CleanOldSystem();

            if (success)
            {
                EditorUtility.DisplayDialog("Setup Complete", 
                    "🎮 Complete Game Flow System 설정 완료!\n\n" +
                    "✅ GameFlowManager 생성\n" +
                    "✅ Video Scene들 생성 (VideoSceneManager 포함)\n" +
                    "✅ SceneTransitionTrigger들 생성\n" +
                    "✅ 기존 복잡한 시스템 정리\n\n" +
                    "이제 각 Video Scene에 영상을 할당하면 됩니다!", "확인");
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
            string errorMessage = e?.Message ?? "알 수 없는 오류";
            Debug.LogError($"[CinematicSystemSetup] 설정 중 오류 발생: {errorMessage}");
            EditorUtility.DisplayDialog("Setup Error", 
                $"설정 중 오류가 발생했습니다:\n{errorMessage}", "확인");
        }

        Debug.Log("🎮 Complete Game Flow System 설정 완료");
    }

    static void CreateRequiredFolders()
    {
        string[] folders = {
            "Assets/Scenes/VideoScenes",
            "Assets/Videos/Cinematics",
            "Assets/Prefabs/GameFlow"
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

    #region GameFlowManager Setup

    static bool CreateGameFlowManager()
    {
        Debug.Log("[CinematicSystemSetup] GameFlowManager 생성 시작...");

        try
        {
            // 기존 GameFlowManager 확인
            GameFlowManager existingManager = FindFirstObjectByType<GameFlowManager>();
            if (existingManager != null)
            {
                Debug.Log("[CinematicSystemSetup] GameFlowManager가 이미 존재합니다.");
                return true;
            }

            // GameFlowManager 오브젝트 생성
            GameObject managerObj = new GameObject("GameFlowManager");
            GameFlowManager manager = managerObj.AddComponent<GameFlowManager>();

            // DontDestroyOnLoad 설정
            managerObj.transform.SetParent(null);

            // 변경사항 저장
            EditorUtility.SetDirty(managerObj);

            Debug.Log("[CinematicSystemSetup] GameFlowManager 생성 완료!");
            return true;
        }
        catch (System.Exception e)
        {
            string errorMessage = e?.Message ?? "알 수 없는 오류";
            Debug.LogError($"[CinematicSystemSetup] GameFlowManager 생성 실패: {errorMessage}");
            return false;
        }
    }

    #endregion

    #region Video Scene Creation

    static bool CreateAllVideoScenes()
    {
        Debug.Log("[CinematicSystemSetup] 모든 영상 Scene 생성 시작...");

        bool success = true;

        // IntroVideo Scene
        if (!CreateVideoScene("IntroVideo"))
        {
            success = false;
        }

        // BossIntroVideo Scene  
        if (!CreateVideoScene("BossIntroVideo"))
        {
            success = false;
        }

        // EndingVideo Scene
        if (!CreateVideoScene("EndingVideo"))
        {
            success = false;
        }

        return success;
    }

    static bool CreateVideoScene(string sceneName)
    {
        Debug.Log($"[CinematicSystemSetup] {sceneName} Scene 생성 시작...");

        try
        {
            // Scene 경로 설정
            string scenePath = $"Assets/Scenes/VideoScenes/{sceneName}.unity";

            // Scene이 이미 존재하는지 확인
            if (File.Exists(scenePath))
            {
                Debug.Log($"[CinematicSystemSetup] {sceneName} Scene이 이미 존재합니다.");
                return true;
            }

            // 새로운 Scene 생성
            UnityEngine.SceneManagement.Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. 검정 Skybox 설정
            SetupBlackSkybox();

            // 2. VR Camera 설정 (Audio Listener 포함)
            GameObject vrCameraRig = SetupVRCamera();

            // 3. Video System 설정
            GameObject videoSystem = SetupVideoSystem();

            // 4. VideoSceneManager 설정
            SetupVideoSceneManager(sceneName);

            // 5. 최소 조명 설정
            SetupMinimalLighting();

            // Scene 저장
            Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            EditorSceneManager.SaveScene(newScene, scenePath);

            Debug.Log($"[CinematicSystemSetup] {sceneName} Scene 생성 완료: {scenePath}");
            return true;
        }
        catch (System.Exception e)
        {
            string errorMessage = e?.Message ?? "알 수 없는 오류";
            Debug.LogError($"[CinematicSystemSetup] {sceneName} Scene 생성 실패: {errorMessage}");
            return false;
        }
    }

    static void SetupBlackSkybox()
    {
        // 검정색 Skybox Material 생성 또는 찾기
        Material blackSkybox = CreateBlackSkyboxMaterial();
        
        // RenderSettings에서 Skybox 설정
        RenderSettings.skybox = blackSkybox;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.fog = false;

        Debug.Log("[CinematicSystemSetup] 검정 Skybox 설정 완료");
    }

    static Material CreateBlackSkyboxMaterial()
    {
        string materialPath = "Assets/Materials/BlackSkybox.mat";
        
        // 이미 존재하는지 확인
        Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (existingMaterial != null)
        {
            return existingMaterial;
        }

        // Materials 폴더 생성
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        // 새로운 Skybox Material 생성
        Material blackSkybox = new Material(Shader.Find("Skybox/Procedural"));
        blackSkybox.SetFloat("_SunSize", 0.04f);
        blackSkybox.SetFloat("_SunSizeConvergence", 5f);
        blackSkybox.SetFloat("_AtmosphereThickness", 1f);
        blackSkybox.SetColor("_SkyTint", Color.black);
        blackSkybox.SetColor("_GroundColor", Color.black);
        blackSkybox.SetFloat("_Exposure", 0f);

        // Material 저장
        AssetDatabase.CreateAsset(blackSkybox, materialPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[CinematicSystemSetup] 검정 Skybox Material 생성: {materialPath}");
        return blackSkybox;
    }

    static GameObject SetupVRCamera()
    {
        // VR Camera Rig 생성
        GameObject vrCameraRig = new GameObject("VRCameraRig");
        GameObject cameraObj = null;
        
        try
        {
            // OVRCameraRig 컴포넌트 추가 시도
            var ovrCameraRigType = System.Type.GetType("OVRCameraRig");
            if (ovrCameraRigType != null)
            {
                vrCameraRig.AddComponent(ovrCameraRigType);
                Debug.Log("[CinematicSystemSetup] OVRCameraRig 설정 완료");
                
                // OVRCameraRig의 CenterEyeAnchor를 찾아서 Audio Listener 추가
                cameraObj = new GameObject("CenterEyeAnchor");
                cameraObj.transform.SetParent(vrCameraRig.transform);
                Camera camera = cameraObj.AddComponent<Camera>();
                camera.tag = "MainCamera";
            }
            else
            {
                // OVR이 없으면 일반 Camera 사용
                cameraObj = new GameObject("MainCamera");
                cameraObj.transform.SetParent(vrCameraRig.transform);
                Camera camera = cameraObj.AddComponent<Camera>();
                camera.tag = "MainCamera";
                Debug.Log("[CinematicSystemSetup] 일반 Camera 설정 완료 (OVR 없음)");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CinematicSystemSetup] OVRCameraRig 설정 실패, 일반 Camera 사용: {e.Message}");
            
            // 일반 Camera로 폴백
            cameraObj = new GameObject("MainCamera");
            cameraObj.transform.SetParent(vrCameraRig.transform);
            Camera camera = cameraObj.AddComponent<Camera>();
            camera.tag = "MainCamera";
        }

        // 핵심: Audio Listener 추가
        if (cameraObj != null)
        {
            AudioListener audioListener = cameraObj.AddComponent<AudioListener>();
            Debug.Log("[CinematicSystemSetup] Audio Listener 추가 완료");
        }

        // 위치 설정
        vrCameraRig.transform.position = Vector3.zero;

        Debug.Log("[CinematicSystemSetup] VR Camera 및 Audio Listener 설정 완료");
        return vrCameraRig;
    }

    static GameObject SetupVideoSystem()
    {
        // Video System 부모 오브젝트
        GameObject videoSystem = new GameObject("VideoSystem");

        // VideoPlayer 설정
        GameObject videoPlayerObj = new GameObject("VideoPlayer");
        videoPlayerObj.transform.SetParent(videoSystem.transform);
        VideoPlayer videoPlayer = videoPlayerObj.AddComponent<VideoPlayer>();

        // VideoPlayer 기본 설정
        videoPlayer.playOnAwake = true; // 자동 재생
        videoPlayer.isLooping = false;
        videoPlayer.skipOnDrop = true;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        // RenderTexture 생성
        RenderTexture renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        renderTexture.Create();
        videoPlayer.targetTexture = renderTexture;

        // Canvas 설정
        GameObject canvasObj = new GameObject("VideoCanvas");
        canvasObj.transform.SetParent(videoSystem.transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Canvas 위치 및 크기 설정 (VR 최적화)
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(16f, 9f);
        canvasRect.position = new Vector3(0, 2f, 5f); // 플레이어 앞 5미터
        canvasRect.localRotation = Quaternion.identity;

        // RawImage 설정
        GameObject rawImageObj = new GameObject("VideoScreen");
        rawImageObj.transform.SetParent(canvasObj.transform);
        UnityEngine.UI.RawImage rawImage = rawImageObj.AddComponent<UnityEngine.UI.RawImage>();
        rawImage.texture = renderTexture;

        // RawImage RectTransform 설정
        RectTransform imageRect = rawImage.rectTransform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
        rawImage.color = Color.white;

        // AudioSource 설정
        AudioSource audioSource = videoPlayerObj.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // 2D 사운드
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audioSource);

        Debug.Log("[CinematicSystemSetup] Video System 설정 완료");
        return videoSystem;
    }

    static void SetupVideoSceneManager(string sceneName)
    {
        // VideoSceneManager 생성
        GameObject managerObj = new GameObject("VideoSceneManager");
        VideoSceneManager manager = managerObj.AddComponent<VideoSceneManager>();

        // VideoPlayer 자동 할당 (리플렉션 사용)
        VideoPlayer videoPlayer = FindFirstObjectByType<VideoPlayer>();
        if (videoPlayer != null)
        {
            var videoPlayerField = typeof(VideoSceneManager).GetField("videoPlayer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (videoPlayerField != null)
            {
                videoPlayerField.SetValue(manager, videoPlayer);
            }
        }

        // nextSceneName 설정 (리플렉션 사용)
        var nextSceneField = typeof(VideoSceneManager).GetField("nextSceneName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (nextSceneField != null)
        {
            string nextScene = sceneName == "EndingVideo" ? "MainMenu" : "Beta(Map Light)";
            nextSceneField.SetValue(manager, nextScene);
        }

        Debug.Log($"[CinematicSystemSetup] VideoSceneManager 설정 완료: {sceneName}");
    }

    static void SetupMinimalLighting()
    {
        // 최소한의 Directional Light (매우 어둡게)
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.1f; // 매우 어둡게
        light.color = Color.white;
        lightObj.transform.rotation = Quaternion.Euler(30f, 0f, 0f);

        Debug.Log("[CinematicSystemSetup] 최소 조명 설정 완료");
    }

    #endregion

    #region MainGame Scene Setup

    static bool SetupMainGameSceneTriggers()
    {
        Debug.Log("[CinematicSystemSetup] MainGame Scene 트리거 설정 시작...");

        try
        {
            // 현재 Scene이 Main Game Scene인지 확인
            UnityEngine.SceneManagement.Scene currentScene = EditorSceneManager.GetActiveScene();
            string currentSceneName = currentScene.name;

            if (!IsMainGameScene(currentSceneName))
            {
                Debug.LogWarning($"[CinematicSystemSetup] MainGame Scene에서 실행해주세요. 현재: {currentSceneName}");
                return false;
            }

            // 기존 트리거들 정리
            CleanOldTriggers();

            // 새로운 SceneTransitionTrigger들 생성
            CreateBossRoomTrigger();
            CreateDaughterRescueTrigger();

            Debug.Log("[CinematicSystemSetup] MainGame Scene 트리거 설정 완료");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CinematicSystemSetup] MainGame Scene 트리거 설정 실패: {e.Message}");
            return false;
        }
    }

    static bool IsMainGameScene(string sceneName)
    {
        return sceneName.Contains("Beta") || 
               sceneName.Contains("Map") ||
               sceneName.Contains("Main") ||
               sceneName.Contains("Game");
    }

    static void CleanOldTriggers()
    {
        // 기존 CinematicTrigger들 찾아서 삭제 (더 이상 존재하지 않지만 안전하게)
        var oldTriggers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var trigger in oldTriggers)
        {
            if (trigger != null && trigger.GetType().Name == "CinematicTrigger")
            {
                Debug.Log($"[CinematicSystemSetup] 기존 CinematicTrigger 삭제: {trigger.name}");
                DestroyImmediate(trigger.gameObject);
            }
        }
    }

    static void CreateBossRoomTrigger()
    {
        // 보스룸 트리거 생성
        GameObject triggerObj = new GameObject("BossRoom_SceneTransitionTrigger");
        
        // Collider 설정
        BoxCollider triggerCollider = triggerObj.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector3(4f, 3f, 2f);

        // SceneTransitionTrigger 컴포넌트 추가
        SceneTransitionTrigger sceneTransitionTrigger = triggerObj.AddComponent<SceneTransitionTrigger>();
        
        // triggerType을 BossIntroVideo로 설정 (리플렉션 사용)
        var triggerTypeField = typeof(SceneTransitionTrigger).GetField("triggerType", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (triggerTypeField != null)
        {
            triggerTypeField.SetValue(sceneTransitionTrigger, SceneTransitionTrigger.TriggerType.BossIntroVideo);
        }

        // 보스룸 입구 위치에 배치
        Vector3 bossRoomPosition = new Vector3(-22.881f, 3.67f, -20f); // 보스룸 앞
        triggerObj.transform.position = bossRoomPosition;

        Debug.Log("[CinematicSystemSetup] 보스룸 SceneTransitionTrigger 생성 완료");
    }

    static void CreateDaughterRescueTrigger()
    {
        // 딸 구출 트리거 생성
        GameObject triggerObj = new GameObject("DaughterRescue_SceneTransitionTrigger");
        
        // Collider 설정
        SphereCollider triggerCollider = triggerObj.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = 2f;

        // SceneTransitionTrigger 컴포넌트 추가
        SceneTransitionTrigger sceneTransitionTrigger = triggerObj.AddComponent<SceneTransitionTrigger>();
        
        // triggerType을 EndingVideo로 설정 (리플렉션 사용)
        var triggerTypeField = typeof(SceneTransitionTrigger).GetField("triggerType", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (triggerTypeField != null)
        {
            triggerTypeField.SetValue(sceneTransitionTrigger, SceneTransitionTrigger.TriggerType.EndingVideo);
        }

        // 딸 위치에 배치
        Vector3 daughterPosition = new Vector3(-25f, 4f, -30f); // 보스룸 근처
        
        // Daughter Spot이 있으면 그 위치 사용
        GameObject daughterSpot = GameObject.Find("Daughter Spot");
        if (daughterSpot != null)
        {
            daughterPosition = daughterSpot.transform.position;
        }
        
        triggerObj.transform.position = daughterPosition;

        Debug.Log("[CinematicSystemSetup] 딸 구출 SceneTransitionTrigger 생성 완료");
    }

    #endregion

    #region Cleanup

    static void CleanOldSystem()
    {
        Debug.Log("[CinematicSystemSetup] 기존 시스템 정리 시작...");

        // 기존 Manager들 찾아서 삭제 (더 이상 존재하지 않지만 안전하게)
        var oldManagers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var manager in oldManagers)
        {
            if (manager != null && 
                (manager.GetType().Name == "CinematicManager" || 
                 manager.GetType().Name == "GameProgressManager"))
            {
                Debug.Log($"[CinematicSystemSetup] 기존 Manager 삭제: {manager.name}");
                DestroyImmediate(manager.gameObject);
            }
        }

        Debug.Log("[CinematicSystemSetup] 기존 시스템 정리 완료");
    }

    [MenuItem("VR Horror Game/Game Flow System/🧹 Clean MainGame Scene")]
    public static void CleanMainGameScene()
    {
        Debug.Log("[CinematicSystemSetup] MainGame Scene 정리 시작...");

        // 현재 Scene이 MainGame Scene인지 확인
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (!IsMainGameScene(currentSceneName))
        {
            EditorUtility.DisplayDialog("경고", 
                $"MainGame Scene에서 실행해주세요.\n현재 Scene: {currentSceneName}", "확인");
            return;
        }

        int cleanedCount = 0;

        // 1. Missing Script가 있는 오브젝트들 찾기
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            // Missing Script 확인
            Component[] components = obj.GetComponents<Component>();
            bool hasMissingScript = false;
            
            foreach (Component comp in components)
            {
                if (comp == null)
                {
                    hasMissingScript = true;
                    break;
                }
            }

            if (hasMissingScript)
            {
                // CinematicManager나 GameProgressManager 오브젝트인 경우 삭제
                if (obj.name.Contains("CinematicManager") || 
                    obj.name.Contains("GameProgressManager"))
                {
                    Debug.Log($"[CinematicSystemSetup] Missing Script 오브젝트 삭제: {obj.name}");
                    DestroyImmediate(obj);
                    cleanedCount++;
                }
                // BossRoomTransition, DaughterRescueTransition은 보존하고 컴포넌트만 교체
                else if (obj.name.Contains("BossRoomTransition") || 
                         obj.name.Contains("DaughterRescueTransition"))
                {
                    Debug.Log($"[CinematicSystemSetup] {obj.name} - Missing Script 제거 후 새 컴포넌트 추가");
                    
                    // Missing Script 제거
                    RemoveMissingScripts(obj);
                    
                    // 새로운 SceneTransitionTrigger 추가
                    ConvertToSceneTransitionTrigger(obj);
                    cleanedCount++;
                }
            }
        }

        // 2. GameFlowManager 생성
        CreateGameFlowManager();

        Debug.Log($"[CinematicSystemSetup] MainGame Scene 정리 완료 - {cleanedCount}개 오브젝트 처리");
        
        EditorUtility.DisplayDialog("Scene 정리 완료", 
            $"MainGame Scene 정리가 완료되었습니다!\n\n" +
            $"✅ {cleanedCount}개 오브젝트 처리\n" +
            $"✅ Missing Script 제거\n" +
            $"✅ 기존 트리거들을 SceneTransitionTrigger로 변환\n" +
            $"✅ GameFlowManager 생성", "확인");
    }

    static void RemoveMissingScripts(GameObject obj)
    {
        // SerializedObject를 사용하여 Missing Script 제거
        SerializedObject serializedObject = new SerializedObject(obj);
        SerializedProperty prop = serializedObject.FindProperty("m_Component");
        
        for (int i = prop.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty componentProp = prop.GetArrayElementAtIndex(i);
            if (componentProp.objectReferenceValue == null)
            {
                prop.DeleteArrayElementAtIndex(i);
                Debug.Log($"[CinematicSystemSetup] {obj.name}에서 Missing Script 제거");
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }

    static void ConvertToSceneTransitionTrigger(GameObject obj)
    {
        // 기존 Collider 확인
        Collider existingCollider = obj.GetComponent<Collider>();
        if (existingCollider == null)
        {
            // Collider가 없으면 추가
            if (obj.name.Contains("BossRoom"))
            {
                BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
                boxCollider.size = new Vector3(4f, 3f, 2f);
            }
            else if (obj.name.Contains("DaughterRescue"))
            {
                SphereCollider sphereCollider = obj.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = 2f;
            }
        }
        else
        {
            existingCollider.isTrigger = true;
        }

        // SceneTransitionTrigger 추가
        SceneTransitionTrigger sceneTransitionTrigger = obj.AddComponent<SceneTransitionTrigger>();
        
        // triggerType 설정 (리플렉션 사용)
        var triggerTypeField = typeof(SceneTransitionTrigger).GetField("triggerType", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (triggerTypeField != null)
        {
            if (obj.name.Contains("BossRoom"))
            {
                triggerTypeField.SetValue(sceneTransitionTrigger, SceneTransitionTrigger.TriggerType.BossIntroVideo);
                Debug.Log($"[CinematicSystemSetup] {obj.name} → BossIntroVideo 트리거로 변환");
            }
            else if (obj.name.Contains("DaughterRescue"))
            {
                triggerTypeField.SetValue(sceneTransitionTrigger, SceneTransitionTrigger.TriggerType.EndingVideo);
                Debug.Log($"[CinematicSystemSetup] {obj.name} → EndingVideo 트리거로 변환");
            }
        }

        // 변경사항 저장
        EditorUtility.SetDirty(obj);
    }

    #endregion

    #region Utility Methods

    static void ResetGameProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[CinematicSystemSetup] 게임 진행 상황 리셋 완료");
        EditorUtility.DisplayDialog("리셋 완료", "모든 게임 진행상황이 리셋되었습니다!", "확인");
    }

    static void TestGameFlow()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("경고", "게임이 실행 중일 때만 테스트할 수 있습니다!", "확인");
            return;
        }
        
        Debug.Log("[CinematicSystemSetup] 게임 플로우 테스트 시작");
        
        if (GameFlowManager.Instance != null)
        {
            Debug.Log($"[CinematicSystemSetup] 현재 상태: {GameFlowManager.Instance.CurrentState}");
        }
        else
        {
            Debug.LogWarning("[CinematicSystemSetup] GameFlowManager를 찾을 수 없습니다!");
        }
    }

    #endregion

    #region Status Display

    void DisplaySystemStatus()
    {
        EditorGUILayout.LabelField("Current System Status:", EditorStyles.boldLabel);

        // GameFlowManager 상태
        GameFlowManager gameFlowManager = FindFirstObjectByType<GameFlowManager>();
        EditorGUILayout.LabelField("GameFlowManager:", gameFlowManager != null ? "✅ 설치됨" : "❌ 없음");

        // 현재 Scene 정보
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        EditorGUILayout.LabelField("Current Scene:", currentSceneName);

        // 생성된 Video Scene들 확인
        string[] videoScenePaths = {
            "Assets/Scenes/VideoScenes/IntroVideo.unity",
            "Assets/Scenes/VideoScenes/BossIntroVideo.unity",
            "Assets/Scenes/VideoScenes/EndingVideo.unity"
        };

        EditorGUILayout.LabelField("Video Scenes:", EditorStyles.boldLabel);
        foreach (string scenePath in videoScenePaths)
        {
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            bool exists = File.Exists(scenePath);
            EditorGUILayout.LabelField($"{sceneName}:", exists ? "✅ 생성됨" : "❌ 없음");
        }

        // SceneTransitionTrigger들 확인
        if (IsMainGameScene(currentSceneName))
        {
            var triggers = FindObjectsByType<SceneTransitionTrigger>(FindObjectsSortMode.None);
            EditorGUILayout.LabelField($"Scene Transition Triggers:", $"{triggers.Length}개");
        }

        // 기존 시스템 상태
        VolumeManager volumeManager = FindFirstObjectByType<VolumeManager>();
        EditorGUILayout.LabelField("VolumeManager:", volumeManager != null ? "✅ 연동 가능" : "❌ 없음");

        if (gameFlowManager != null && Application.isPlaying)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Game Progress:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Current State:", gameFlowManager.CurrentState.ToString());
            EditorGUILayout.LabelField("Has Seen Intro:", gameFlowManager.HasSeenIntro ? "✅" : "❌");
            EditorGUILayout.LabelField("Has Seen Boss Intro:", gameFlowManager.HasSeenBossIntro ? "✅" : "❌");
            EditorGUILayout.LabelField("Boss Defeated:", gameFlowManager.IsBossDefeated ? "✅" : "❌");
        }
    }

    #endregion

    [MenuItem("VR Horror Game/Fix Video Scenes VR Camera")]
    static void FixVideoScenesVRCamera()
    {
        string[] videoScenes = { "IntroVideo", "BossIntroVideo", "EndingVideo" };
        
        foreach (string sceneName in videoScenes)
        {
            string scenePath = $"Assets/Scenes/VideoScenes/{sceneName}.unity";
            
            if (!File.Exists(scenePath))
            {
                Debug.LogWarning($"Scene not found: {scenePath}");
                continue;
            }

            // Scene 열기
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            
            Debug.Log($"Setting up OVRCameraRig in {sceneName}...");
            
            // 기존 OVRCameraRig 확인
            GameObject existingOVRRig = GameObject.Find("OVRCameraRig");
            if (existingOVRRig != null)
            {
                Debug.Log($"OVRCameraRig already exists in {sceneName}");
                continue;
            }

            // 기존 VRCameraRig 삭제
            GameObject existingVRRig = GameObject.Find("VRCameraRig");
            if (existingVRRig != null)
            {
                DestroyImmediate(existingVRRig);
            }

            // OVRCameraRig 생성 (메인 게임과 동일한 구조)
            CreateOVRCameraRigForVideoScene();

            // Scene 저장
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"OVRCameraRig setup completed for {sceneName}");
        }
        
        Debug.Log("All Video Scenes OVRCameraRig setup completed!");
        AssetDatabase.Refresh();
    }

    static void CreateOVRCameraRigForVideoScene()
    {
        // 1. OVRCameraRig 생성
        GameObject ovrCameraRig = new GameObject("OVRCameraRig");
        
        // OVRCameraRig 컴포넌트 추가 (스크립트로 설정)
        var cameraRigScript = ovrCameraRig.AddComponent<MonoBehaviour>();
        
        // 2. TrackingSpace 생성
        GameObject trackingSpace = new GameObject("TrackingSpace");
        trackingSpace.transform.SetParent(ovrCameraRig.transform);
        trackingSpace.transform.localPosition = Vector3.zero;
        
        // 3. CenterEyeAnchor 생성 (메인 카메라)
        GameObject centerEyeAnchor = new GameObject("CenterEyeAnchor");
        centerEyeAnchor.transform.SetParent(trackingSpace.transform);
        centerEyeAnchor.transform.localPosition = Vector3.zero;
        centerEyeAnchor.tag = "MainCamera";
        
        // Camera 컴포넌트 추가
        Camera camera = centerEyeAnchor.AddComponent<Camera>();
        camera.stereoTargetEye = StereoTargetEyeMask.Both;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 1000f;
        
        // AudioListener 추가
        AudioListener audioListener = centerEyeAnchor.AddComponent<AudioListener>();
        
        // 4. LeftEyeAnchor 생성
        GameObject leftEyeAnchor = new GameObject("LeftEyeAnchor");
        leftEyeAnchor.transform.SetParent(trackingSpace.transform);
        leftEyeAnchor.transform.localPosition = new Vector3(-0.032f, 0, 0);
        
        // 5. RightEyeAnchor 생성
        GameObject rightEyeAnchor = new GameObject("RightEyeAnchor");
        rightEyeAnchor.transform.SetParent(trackingSpace.transform);
        rightEyeAnchor.transform.localPosition = new Vector3(0.032f, 0, 0);
        
        // 6. TrackerAnchor 생성
        GameObject trackerAnchor = new GameObject("TrackerAnchor");
        trackerAnchor.transform.SetParent(trackingSpace.transform);
        trackerAnchor.transform.localPosition = Vector3.zero;
        
        // 7. LeftHandAnchor 생성
        GameObject leftHandAnchor = new GameObject("LeftHandAnchor");
        leftHandAnchor.transform.SetParent(trackingSpace.transform);
        leftHandAnchor.transform.localPosition = Vector3.zero;
        
        // 8. RightHandAnchor 생성
        GameObject rightHandAnchor = new GameObject("RightHandAnchor");
        rightHandAnchor.transform.SetParent(trackingSpace.transform);
        rightHandAnchor.transform.localPosition = Vector3.zero;
        
        Debug.Log("OVRCameraRig structure created for Video Scene");
    }

    [MenuItem("VR Horror Game/Setup BetaAfterBoss Scene")]
    static void SetupBetaAfterBossScene()
    {
        Debug.Log("[CinematicSystemSetup] BetaAfterBoss 씬 설정 시작...");
        
        // 현재 씬이 BetaAfterBoss(Map Light)인지 확인
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentSceneName != "BetaAfterBoss(Map Light)")
        {
            Debug.LogWarning($"[CinematicSystemSetup] 현재 씬이 BetaAfterBoss(Map Light)가 아닙니다. 현재: {currentSceneName}");
            Debug.Log("BetaAfterBoss(Map Light) 씬을 열고 다시 실행해주세요.");
            return;
        }
        
        // DoorD_V2 찾아서 비활성화
        GameObject bossDoor = GameObject.Find("DoorD_V2");
        if (bossDoor != null)
        {
            bossDoor.SetActive(false);
            Debug.Log("[CinematicSystemSetup] DoorD_V2 보스문 비활성화 완료");
        }
        else
        {
            Debug.LogWarning("[CinematicSystemSetup] DoorD_V2를 찾을 수 없습니다.");
            
            // 다른 가능한 이름들로 시도
            string[] possibleDoorNames = { "DoorD_V2 (1)", "DoorD_V2 (2)" };
            
            foreach (string doorName in possibleDoorNames)
            {
                GameObject door = GameObject.Find(doorName);
                if (door != null)
                {
                    door.SetActive(false);
                    Debug.Log($"[CinematicSystemSetup] {doorName} 보스문 비활성화 완료");
                    break;
                }
            }
        }
        
        // 모든 적 비활성화 (보스 인트로 후에는 적이 없어야 함)
        DisableAllEnemies();
        
        // 플레이어 스폰 위치를 보스룸 앞으로 설정
        SetupPlayerSpawnAtBossRoom();
        
        // 씬 저장
        #if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[CinematicSystemSetup] BetaAfterBoss 씬 설정 및 저장 완료!");
        #endif
    }
    
    static void DisableAllEnemies()
    {
        // CultistAI 비활성화
        var cultistAIs = FindObjectsByType<CultistAI>(FindObjectsSortMode.None);
        foreach (var ai in cultistAIs)
        {
            if (ai != null)
            {
                ai.gameObject.SetActive(false);
                Debug.Log($"[CinematicSystemSetup] {ai.name} CultistAI 비활성화");
            }
        }
        
        // NecromancerBoss 비활성화 (아직 보스전이 아니므로)
        var necromancerBoss = FindFirstObjectByType<NecromancerBoss>();
        if (necromancerBoss != null)
        {
            necromancerBoss.gameObject.SetActive(false);
            Debug.Log("[CinematicSystemSetup] NecromancerBoss 비활성화");
        }
        
        Debug.Log("[CinematicSystemSetup] 모든 적 비활성화 완료");
    }
    
    static void SetupPlayerSpawnAtBossRoom()
    {
        // PlayerSpawn 오브젝트 찾기
        GameObject playerSpawn = GameObject.Find("PlayerSpawn");
        if (playerSpawn != null)
        {
            // 보스룸 앞 위치로 이동 (DoorD_V2가 있던 위치 근처)
            Vector3 bossRoomSpawnPosition = new Vector3(-22.881f, 3.67f, -18f); // 문 앞쪽
            playerSpawn.transform.position = bossRoomSpawnPosition;
            Debug.Log($"[CinematicSystemSetup] PlayerSpawn 위치를 보스룸 앞으로 이동: {bossRoomSpawnPosition}");
        }
        else
        {
            Debug.LogWarning("[CinematicSystemSetup] PlayerSpawn 오브젝트를 찾을 수 없습니다.");
        }
    }
}
#endif 