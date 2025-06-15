using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;
using System.IO;

/// <summary>
/// VolumeManager 자동 설정 도구
/// AudioMixer 생성, VolumeManager 프리팹 생성, 씬 배치를 자동화
/// </summary>
public class VolumeManagerSetup : EditorWindow
{
    [MenuItem("VR Horror Game/Audio/Volume Manager Setup")]
    public static void ShowWindow()
    {
        GetWindow<VolumeManagerSetup>("Volume Manager Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("VR Horror Game - Volume Manager Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "이 도구는 VR Horror Game의 통합 사운드 시스템을 자동으로 설정합니다:\n\n" +
            "1. AudioMixer 생성 (Master, BGM, SFX, Spatial SFX 그룹)\n" +
            "2. VolumeManager 프리팹 생성\n" +
            "3. 씬에 VolumeManager 배치\n" +
            "4. 기본 사운드 클립 슬롯 설정",
            MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("🎵 Complete Volume Manager Setup", GUILayout.Height(40)))
        {
            SetupCompleteVolumeManager();
        }

        GUILayout.Space(10);

        EditorGUILayout.LabelField("개별 설정 도구:", EditorStyles.boldLabel);

        if (GUILayout.Button("1. AudioMixer 생성 (수동)", GUILayout.Height(30)))
        {
            ShowAudioMixerCreationGuide();
        }

        if (GUILayout.Button("2. VolumeManager 프리팹 생성", GUILayout.Height(30)))
        {
            CreateVolumeManagerPrefab();
        }

        if (GUILayout.Button("3. 씬에 VolumeManager 배치", GUILayout.Height(30)))
        {
            PlaceVolumeManagerInScene();
        }

        GUILayout.Space(10);

        // 현재 상태 표시
        ShowCurrentStatus();
    }

    void SetupCompleteVolumeManager()
    {
        try
        {
            // 1. AudioMixer 생성 (또는 확인)
            AudioMixer mixer = CreateAudioMixer();
            
            if (mixer == null)
            {
                // AudioMixer가 없으면 중단
                return;
            }
            
            // 2. VolumeManager 프리팹 생성
            GameObject prefab = CreateVolumeManagerPrefab();
            
            // 3. 씬에 배치
            PlaceVolumeManagerInScene();

            EditorUtility.DisplayDialog(
                "설정 완료!",
                "VolumeManager 시스템이 성공적으로 설정되었습니다!\n\n" +
                "다음 단계:\n" +
                "1. VolumeManager Inspector에서 AudioMixer Groups를 할당하세요\n" +
                "2. BGM/SFX 클립들을 할당하세요\n" +
                "3. 기존 스크립트들의 'Use Volume Manager' 옵션을 활성화하세요\n" +
                "4. 게임을 실행하여 사운드 시스템을 테스트하세요",
                "확인");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("오류", $"설정 중 오류가 발생했습니다:\n{e.Message}", "확인");
        }
    }

    AudioMixer CreateAudioMixer()
    {
        string mixerPath = "Assets/Audio/VR_Horror_AudioMixer.mixer";
        
        // Audio 폴더 생성
        string audioFolder = "Assets/Audio";
        if (!AssetDatabase.IsValidFolder(audioFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Audio");
        }

        // 기존 AudioMixer 확인
        AudioMixer existingMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath);
        if (existingMixer != null)
        {
            Debug.Log("[VolumeManagerSetup] AudioMixer가 이미 존재합니다: " + mixerPath);
            return existingMixer;
        }

        // AudioMixer는 코드로 직접 생성할 수 없으므로 사용자에게 안내
        EditorUtility.DisplayDialog(
            "AudioMixer 수동 생성 필요",
            "AudioMixer는 코드로 자동 생성할 수 없습니다.\n\n" +
            "다음 단계를 따라 수동으로 생성해주세요:\n\n" +
            "1. Project 창에서 Assets/Audio 폴더를 선택\n" +
            "2. 우클릭 → Create → Audio Mixer\n" +
            "3. 이름을 'VR_Horror_AudioMixer'로 변경\n" +
            "4. 다시 이 버튼을 클릭하세요",
            "확인");

        Debug.LogWarning("[VolumeManagerSetup] AudioMixer를 수동으로 생성해주세요: " + mixerPath);
        return null;
    }

    GameObject CreateVolumeManagerPrefab()
    {
        string prefabPath = "Assets/Prefabs/VolumeManager.prefab";
        
        // Prefabs 폴더 생성
        string prefabsFolder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabsFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // 기존 프리팹 확인
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existingPrefab != null)
        {
            Debug.Log("[VolumeManagerSetup] VolumeManager 프리팹이 이미 존재합니다: " + prefabPath);
            return existingPrefab;
        }

        // VolumeManager GameObject 생성
        GameObject volumeManagerObj = new GameObject("VolumeManager");
        VolumeManager volumeManager = volumeManagerObj.AddComponent<VolumeManager>();

        // AudioMixer 할당
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/VR_Horror_AudioMixer.mixer");
        if (mixer != null)
        {
            // Reflection을 사용하여 private 필드에 접근
            var mixerGroupsField = typeof(VolumeManager).GetField("masterMixerGroup", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (mixerGroupsField != null)
            {
                mixerGroupsField.SetValue(volumeManager, mixer.outputAudioMixerGroup);
            }
        }

        // 기본 BGM 클립 데이터 설정
        SetupDefaultBGMClips(volumeManager);
        
        // 기본 SFX 클립 데이터 설정
        SetupDefaultSFXClips(volumeManager);

        // 프리팹으로 저장
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(volumeManagerObj, prefabPath);
        
        // 씬에서 임시 오브젝트 제거
        DestroyImmediate(volumeManagerObj);

        Debug.Log("[VolumeManagerSetup] VolumeManager 프리팹 생성 완료: " + prefabPath);
        return prefab;
    }

    void SetupDefaultBGMClips(VolumeManager volumeManager)
    {
        // BGM 클립 데이터 배열 생성 (기본 구조만)
        var bgmClipsField = typeof(VolumeManager).GetField("bgmClips", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (bgmClipsField != null)
        {
            // 기본 BGM 타입들을 위한 빈 배열 생성
            var bgmClipDataType = typeof(VolumeManager).GetNestedType("BGMClipData");
            if (bgmClipDataType != null)
            {
                var bgmArray = System.Array.CreateInstance(bgmClipDataType, 8); // 8개 BGM 타입
                bgmClipsField.SetValue(volumeManager, bgmArray);
            }
        }
    }

    void SetupDefaultSFXClips(VolumeManager volumeManager)
    {
        // SFX 클립 데이터 배열 생성 (기본 구조만)
        var sfxClipsField = typeof(VolumeManager).GetField("sfxClips", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (sfxClipsField != null)
        {
            // 기본 SFX 타입들을 위한 빈 배열 생성
            var sfxClipDataType = typeof(VolumeManager).GetNestedType("SFXClipData");
            if (sfxClipDataType != null)
            {
                var sfxArray = System.Array.CreateInstance(sfxClipDataType, 20); // 20개 SFX 타입
                sfxClipsField.SetValue(volumeManager, sfxArray);
            }
        }
    }

    void PlaceVolumeManagerInScene()
    {
        // 씬에 이미 VolumeManager가 있는지 확인
        VolumeManager existingManager = FindFirstObjectByType<VolumeManager>();
        if (existingManager != null)
        {
            Debug.Log("[VolumeManagerSetup] VolumeManager가 이미 씬에 존재합니다: " + existingManager.name);
            Selection.activeGameObject = existingManager.gameObject;
            return;
        }

        // 프리팹 로드
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VolumeManager.prefab");
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("오류", "VolumeManager 프리팹을 찾을 수 없습니다. 먼저 프리팹을 생성하세요.", "확인");
            return;
        }

        // 씬에 인스턴스 생성
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        instance.name = "VolumeManager";

        // 씬 루트에 배치
        instance.transform.SetParent(null);
        instance.transform.position = Vector3.zero;

        // 선택
        Selection.activeGameObject = instance;

        Debug.Log("[VolumeManagerSetup] VolumeManager를 씬에 배치했습니다.");
    }

    void ShowCurrentStatus()
    {
        EditorGUILayout.LabelField("현재 상태:", EditorStyles.boldLabel);

        // AudioMixer 상태
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/VR_Horror_AudioMixer.mixer");
        string mixerStatus = mixer != null ? "✅ 생성됨" : "❌ 없음";
        EditorGUILayout.LabelField($"AudioMixer: {mixerStatus}");

        // 프리팹 상태
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VolumeManager.prefab");
        string prefabStatus = prefab != null ? "✅ 생성됨" : "❌ 없음";
        EditorGUILayout.LabelField($"VolumeManager 프리팹: {prefabStatus}");

        // 씬 상태
        VolumeManager sceneManager = FindFirstObjectByType<VolumeManager>();
        string sceneStatus = sceneManager != null ? "✅ 배치됨" : "❌ 없음";
        EditorGUILayout.LabelField($"씬의 VolumeManager: {sceneStatus}");

        if (sceneManager != null)
        {
            GUILayout.Space(5);
            if (GUILayout.Button("씬의 VolumeManager 선택"))
            {
                Selection.activeGameObject = sceneManager.gameObject;
            }
        }

        GUILayout.Space(10);

        // 기존 스크립트 상태 확인
        ShowExistingScriptsStatus();
    }

    void ShowExistingScriptsStatus()
    {
        EditorGUILayout.LabelField("기존 스크립트 상태:", EditorStyles.boldLabel);

        // Boss 확인
        BossAI boss = FindFirstObjectByType<BossAI>();
        string bossStatus = boss != null ? (boss.useVolumeManager ? "✅ VolumeManager 사용" : "⚠️ 기존 방식") : "❌ 없음";
        EditorGUILayout.LabelField($"Boss AI: {bossStatus}");

        // Enemy 확인
        EnemyAttackSystem[] enemies = FindObjectsByType<EnemyAttackSystem>(FindObjectsSortMode.None);
        string enemyStatus = enemies.Length > 0 ? 
            (enemies[0].useVolumeManager ? $"✅ {enemies.Length}개 VolumeManager 사용" : $"⚠️ {enemies.Length}개 기존 방식") : 
            "❌ 없음";
        EditorGUILayout.LabelField($"Enemy Systems: {enemyStatus}");

        // Weapon 확인
        AxeWeapon weapon = FindFirstObjectByType<AxeWeapon>();
        string weaponStatus = weapon != null ? (weapon.useVolumeManager ? "✅ VolumeManager 사용" : "⚠️ 기존 방식") : "❌ 없음";
        EditorGUILayout.LabelField($"Axe Weapon: {weaponStatus}");

        GUILayout.Space(5);

        if (GUILayout.Button("모든 스크립트를 VolumeManager 사용으로 전환"))
        {
            ConvertAllScriptsToVolumeManager();
        }
    }

    void ConvertAllScriptsToVolumeManager()
    {
        int convertedCount = 0;

        // Boss 전환
        BossAI boss = FindFirstObjectByType<BossAI>();
        if (boss != null && !boss.useVolumeManager)
        {
            boss.useVolumeManager = true;
            EditorUtility.SetDirty(boss);
            convertedCount++;
        }

        // Enemy 전환
        EnemyAttackSystem[] enemies = FindObjectsByType<EnemyAttackSystem>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (!enemy.useVolumeManager)
            {
                enemy.useVolumeManager = true;
                EditorUtility.SetDirty(enemy);
                convertedCount++;
            }
        }

        // Weapon 전환
        AxeWeapon weapon = FindFirstObjectByType<AxeWeapon>();
        if (weapon != null && !weapon.useVolumeManager)
        {
            weapon.useVolumeManager = true;
            EditorUtility.SetDirty(weapon);
            convertedCount++;
        }

        EditorUtility.DisplayDialog(
            "전환 완료",
            $"{convertedCount}개의 스크립트가 VolumeManager 사용으로 전환되었습니다!",
            "확인");

        Debug.Log($"[VolumeManagerSetup] {convertedCount}개 스크립트를 VolumeManager 사용으로 전환했습니다.");
    }

    void ShowAudioMixerCreationGuide()
    {
        // Audio 폴더 생성
        string audioFolder = "Assets/Audio";
        if (!AssetDatabase.IsValidFolder(audioFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Audio");
        }

        // Audio 폴더 선택
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(audioFolder);

        EditorUtility.DisplayDialog(
            "AudioMixer 수동 생성 가이드",
            "AudioMixer를 수동으로 생성해주세요:\n\n" +
            "1. Project 창에서 Assets/Audio 폴더가 선택되었습니다\n" +
            "2. 우클릭 → Create → Audio Mixer\n" +
            "3. 이름을 'VR_Horror_AudioMixer'로 변경\n" +
            "4. AudioMixer를 더블클릭하여 열기\n" +
            "5. Groups에서 Master 그룹 하위에 다음 그룹들을 추가:\n" +
            "   - BGM\n" +
            "   - SFX\n" +
            "   - SpatialSFX\n\n" +
            "완료 후 다시 'Complete Volume Manager Setup'을 클릭하세요!",
            "확인");

        Debug.Log("[VolumeManagerSetup] AudioMixer 생성 가이드를 표시했습니다. Assets/Audio 폴더에서 AudioMixer를 생성하세요.");
    }
} 