using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
/// <summary>
/// Animation Event 자동 설정 도구
/// Enemy의 Attack1 애니메이션에 OnAttack1Hit 이벤트를 자동 추가
/// </summary>
public class AnimationEventSetupTool : EditorWindow
{
    private Vector2 scrollPosition;
    private AnimationClip selectedClip;
    private float eventTime = 0.5f; // 이벤트 발생 시점 (0~1)
    
    [MenuItem("Window/VR Horror Game/Animation Event Setup")]
    public static void ShowWindow()
    {
        GetWindow<AnimationEventSetupTool>("Animation Event Setup");
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("🎬 Animation Event 설정 도구", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Enemy의 Attack1 애니메이션에 OnAttack1Hit 이벤트를 추가합니다.\n" +
            "이벤트가 추가되면 Enemy가 플레이어를 실제로 공격할 수 있게 됩니다!", 
            MessageType.Info);
        
        GUILayout.Space(15);

        // 1. 자동 설정 (권장)
        EditorGUILayout.LabelField("🚀 자동 설정 (권장)", EditorStyles.boldLabel);
        
        if (GUILayout.Button("📦 모든 Enemy 프리팹에 EnemyAttackSystem 추가", GUILayout.Height(35)))
        {
            AddEnemyAttackSystemToAllPrefabs();
        }
        
        if (GUILayout.Button("🎯 모든 Attack1 애니메이션에 Event 자동 추가", GUILayout.Height(35)))
        {
            AutoAddAnimationEvents();
        }

        GUILayout.Space(20);
        
        // 2. 수동 설정
        EditorGUILayout.LabelField("🔧 수동 설정 (고급)", EditorStyles.boldLabel);
        
        selectedClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip:", selectedClip, typeof(AnimationClip), false);
        eventTime = EditorGUILayout.Slider("이벤트 시점 (0~1):", eventTime, 0f, 1f);
        
        EditorGUILayout.HelpBox($"현재 설정: {eventTime:P1} 지점에서 OnAttack1Hit 호출", MessageType.None);
        
        if (selectedClip != null && GUILayout.Button("선택된 애니메이션에 Event 추가", GUILayout.Height(30)))
        {
            AddAnimationEventToClip(selectedClip, eventTime);
        }

        GUILayout.Space(20);
        
        // 3. 검증 도구
        EditorGUILayout.LabelField("✅ 검증 도구", EditorStyles.boldLabel);
        
        if (GUILayout.Button("현재 설정 상태 확인", GUILayout.Height(25)))
        {
            ValidateSetup();
        }

        GUILayout.Space(15);
        
        // 4. 사용법 가이드
        EditorGUILayout.LabelField("📖 사용법 가이드", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(
            "1. '모든 Enemy 프리팹에 EnemyAttackSystem 추가' 클릭\n" +
            "2. '모든 Attack1 애니메이션에 Event 자동 추가' 클릭\n" +
            "3. '현재 설정 상태 확인'으로 검증\n" +
            "4. 플레이 모드에서 Enemy가 플레이어 공격 테스트\n\n" +
            "⚠️ 주의: 이미 이벤트가 있는 애니메이션은 건너뜁니다.", 
            GUILayout.Height(120));

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 모든 Enemy 프리팹에 EnemyAttackSystem 추가
    /// </summary>
    private void AddEnemyAttackSystemToAllPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int addedCount = 0;

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
                    addedCount++;
                    Debug.Log($"[AnimationEventSetup] EnemyAttackSystem 추가: {prefab.name}");
                }
                
                PrefabUtility.UnloadPrefabContents(prefabInstance);
            }
        }

        EditorUtility.DisplayDialog("완료!", 
            $"✅ {addedCount}개의 Enemy 프리팹에 EnemyAttackSystem을 추가했습니다.", "확인");
    }

    /// <summary>
    /// 모든 Attack1 애니메이션에 Animation Event 자동 추가
    /// </summary>
    private void AutoAddAnimationEvents()
    {
        int addedCount = 0;
        List<string> processedAnimations = new List<string>();

        // 1. 일반 AnimationClip 처리
        string[] animGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets" });
        foreach (string guid in animGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

            if (clip != null && IsAttackAnimation(clip))
            {
                if (AddAnimationEventToClip(clip, 0.6f))
                {
                    addedCount++;
                    processedAnimations.Add(clip.name);
                }
            }
        }

        // 2. FBX 파일 내의 AnimationClip 처리 (Standing Melee Attack Downward 등)
        string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
        foreach (string guid in fbxGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".fbx"))
            {
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (Object asset in assets)
                {
                    if (asset is AnimationClip clip && IsAttackAnimation(clip))
                    {
                        if (AddAnimationEventToClip(clip, 0.6f))
                        {
                            addedCount++;
                            processedAnimations.Add($"{clip.name} (FBX)");
                        }
                    }
                }
            }
        }

        string animationList = processedAnimations.Count > 0 
            ? "\n\n처리된 애니메이션:\n• " + string.Join("\n• ", processedAnimations)
            : "";

        EditorUtility.DisplayDialog("완료!", 
            $"🎬 {addedCount}개의 Attack 애니메이션에 OnAttack1Hit 이벤트를 추가했습니다!" + animationList +
            "\n\n이제 Enemy가 플레이어를 실제로 공격할 수 있습니다!", "확인");
    }

    /// <summary>
    /// 특정 애니메이션 클립에 이벤트 추가
    /// </summary>
    private bool AddAnimationEventToClip(AnimationClip clip, float time)
    {
        if (clip == null) return false;

        // 이미 OnAttack1Hit 이벤트가 있는지 확인
        AnimationEvent[] existingEvents = clip.events;
        foreach (var evt in existingEvents)
        {
            if (evt.functionName == "OnAttack1Hit")
            {
                Debug.Log($"[AnimationEventSetup] {clip.name}에 이미 OnAttack1Hit 이벤트가 있습니다.");
                return false;
            }
        }

        // 새 이벤트 생성
        AnimationEvent newEvent = new AnimationEvent();
        newEvent.time = clip.length * time; // 상대적 시간을 절대 시간으로 변환
        newEvent.functionName = "OnAttack1Hit";
        
        // Unity 2022+ 호환: AnimationUtility 사용
        #if UNITY_2022_1_OR_NEWER
        UnityEditor.AnimationUtility.SetAnimationEvents(clip, System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Append(existingEvents, newEvent)));
        #else
        // 기존 이벤트들과 새 이벤트를 합쳐서 다시 설정
        AnimationEvent[] newEvents = new AnimationEvent[existingEvents.Length + 1];
        existingEvents.CopyTo(newEvents, 0);
        newEvents[existingEvents.Length] = newEvent;
        clip.events = newEvents;
        #endif
        
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"[AnimationEventSetup] ✅ {clip.name}에 OnAttack1Hit 이벤트 추가 (시간: {newEvent.time:F2}초)");
        return true;
    }

    /// <summary>
    /// Enemy 프리팹인지 확인
    /// </summary>
    private bool IsEnemyPrefab(GameObject prefab)
    {
        string name = prefab.name.ToLower();
        return name.Contains("enemy") || name.Contains("fanatic") || name.Contains("boss") || 
               name.Contains("priest") || name.Contains("cultist") || 
               prefab.GetComponent<CultistAI>() != null;
    }

    /// <summary>
    /// 공격 애니메이션인지 확인
    /// </summary>
    private bool IsAttackAnimation(AnimationClip clip)
    {
        string name = clip.name.ToLower();
        
        // 더 포괄적인 공격 애니메이션 감지
        bool isAttack = name.Contains("attack") || 
                       name.Contains("melee") || 
                       name.Contains("punch") || 
                       name.Contains("hit") || 
                       name.Contains("strike") || 
                       name.Contains("swing");
                       
        bool isNotIdle = !name.Contains("idle") && 
                        !name.Contains("walk") && 
                        !name.Contains("run") && 
                        !name.Contains("pose");
                        
        return isAttack && isNotIdle;
    }

    /// <summary>
    /// 현재 설정 상태 검증
    /// </summary>
    private void ValidateSetup()
    {
        string report = "🔍 Animation Event 설정 검증 결과:\n\n";
        
        // 1. Enemy 프리팹 체크
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int enemyCount = 0, setupCount = 0;
        
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null && IsEnemyPrefab(prefab))
            {
                enemyCount++;
                if (prefab.GetComponent<EnemyAttackSystem>() != null)
                {
                    setupCount++;
                }
            }
        }
        
        report += $"📦 Enemy 프리팹: {setupCount}/{enemyCount} EnemyAttackSystem 설정됨\n";
        
        // 2. 애니메이션 이벤트 체크
        string[] animGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets" });
        int attackAnimCount = 0, eventCount = 0;
        
        foreach (string guid in animGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

            if (clip != null && IsAttackAnimation(clip))
            {
                attackAnimCount++;
                
                AnimationEvent[] events = clip.events;
                foreach (var evt in events)
                {
                    if (evt.functionName == "OnAttack1Hit")
                    {
                        eventCount++;
                        break;
                    }
                }
            }
        }
        
        report += $"🎬 Attack 애니메이션: {eventCount}/{attackAnimCount} OnAttack1Hit 이벤트 설정됨\n";
        
        // 3. 플레이어 설정 체크
        VRPlayerHealth playerHealth = FindFirstObjectByType<VRPlayerHealth>();
        report += $"🎮 VRPlayerHealth: {(playerHealth != null ? "✅" : "❌")} 설정됨\n";
        
        // 4. Post Processing 체크
        VRPostProcessingManager postProcessing = FindFirstObjectByType<VRPostProcessingManager>();
        report += $"🎨 Post Processing: {(postProcessing != null ? "✅" : "❌")} 활성화됨\n";
        
        report += "\n";
        
        if (setupCount == enemyCount && eventCount > 0 && playerHealth != null && postProcessing != null)
        {
            report += "🎉 모든 설정이 완료되었습니다!\n";
            report += "이제 Enemy가 플레이어를 공격할 때 VR 화면이 빨갛게 변합니다!";
        }
        else
        {
            report += "⚠️ 일부 설정이 누락되었습니다.\n";
            report += "위의 자동 설정 버튼들을 사용해 설정을 완료하세요.";
        }
        
        EditorUtility.DisplayDialog("검증 결과", report, "확인");
        Debug.Log("[AnimationEventSetup] " + report.Replace("\n", " | "));
    }
}
#endif 