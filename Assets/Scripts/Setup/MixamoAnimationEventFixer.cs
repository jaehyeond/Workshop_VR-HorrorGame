using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
/// <summary>
/// Mixamo FBX 애니메이션에 Animation Event를 추가하기 위한 도구
/// FBX는 Read-Only이므로 Animation Override Controller를 사용합니다
/// </summary>
public class MixamoAnimationEventFixer : EditorWindow
{
    [MenuItem("Window/VR Horror Game/Fix Mixamo Animation Events")]
    public static void ShowWindow()
    {
        GetWindow<MixamoAnimationEventFixer>("Mixamo Animation Fix");
    }

    void OnGUI()
    {
        GUILayout.Label("Mixamo Animation Event 수정 도구", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Mixamo FBX 파일은 Read-Only라서 직접 수정할 수 없습니다.\n" +
            "각 보스별 개별 애니메이션을 복사하여 Animation Event를 추가합니다.", 
            MessageType.Info);

        GUILayout.Space(15);

        if (GUILayout.Button("🎯 모든 보스 공격 애니메이션 복사 및 이벤트 추가", GUILayout.Height(40)))
        {
            CreateAllBossAttackAnimations();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("📋 모든 보스 Animator Controller 업데이트", GUILayout.Height(30)))
        {
            UpdateAllBossAnimatorControllers();
        }

        GUILayout.Space(15);

        EditorGUILayout.HelpBox(
            "🎮 새로운 기능:\n" +
            "1. 각 보스별 개별 Attack1, Attack2 애니메이션 복사\n" +
            "2. OnAttack1Hit (데미지) + OnAttackComplete (패턴 전환) 이벤트 추가\n" +
            "3. 보스별 고유 애니메이션 유지하면서 이벤트만 추가\n" +
            "4. Attack1 ↔ Attack2 순환 패턴 지원", 
            MessageType.None);
    }

    /// <summary>
    /// 모든 보스의 공격 애니메이션을 복사하고 Animation Event 추가
    /// 현재 애니메이터에 설정된 실제 애니메이션을 추출하여 사용
    /// </summary>
    private void CreateAllBossAttackAnimations()
    {
        // 보스 애니메이터 컨트롤러 경로
        var bossControllers = new Dictionary<string, string>()
        {
            ["HollyPrist"] = "Assets/Jaehyeon/Animations/HollyPrist.controller",
            ["HollyHuman"] = "Assets/Jaehyeon/Animations/HollyHuman.controller",
            ["HollyBoss"] = "Assets/Jaehyeon/Animations/HollyBoss.controller"
        };

        // 새 디렉토리 생성
        string newDir = "Assets/Animations/BossAttacks";
        if (!Directory.Exists(newDir))
        {
            Directory.CreateDirectory(newDir);
        }

        int createdCount = 0;
        string createdList = "";

        foreach (var boss in bossControllers)
        {
            string bossName = boss.Key;
            string controllerPath = boss.Value;
            
            // 애니메이터 컨트롤러 로드
            UnityEditor.Animations.AnimatorController controller = 
                AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
                
            if (controller == null)
            {
                Debug.LogWarning($"[MixamoAnimationEventFixer] {controllerPath}를 찾을 수 없습니다.");
                continue;
            }

            // Attack1, Attack2 상태에서 애니메이션 추출
            string[] attackStates = { "Attack1", "Attack2" };
            
            foreach (string attackName in attackStates)
            {
                AnimationClip originalClip = GetAnimationFromController(controller, attackName);
                
                if (originalClip == null)
                {
                    Debug.LogWarning($"[MixamoAnimationEventFixer] {bossName}의 {attackName} 애니메이션을 찾을 수 없습니다.");
                    continue;
                }

                // 새 애니메이션 클립 생성
                AnimationClip newClip = new AnimationClip();
                newClip.name = $"{bossName}_{attackName}_WithEvent";
                
                // 원본 애니메이션의 모든 커브를 복사
                EditorUtility.CopySerialized(originalClip, newClip);
                
                // Animation Event 추가
                AddAnimationEventsToClip(newClip, attackName);
                
                // 애니메이션 파일 저장
                string newPath = $"{newDir}/{bossName}_{attackName}_WithEvent.anim";
                AssetDatabase.CreateAsset(newClip, newPath);
                
                createdCount++;
                createdList += $"• {bossName}_{attackName}_WithEvent.anim (원본: {originalClip.name})\n";
                
                Debug.Log($"[MixamoAnimationEventFixer] 새 애니메이션 생성: {newPath} (원본: {originalClip.name})");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("완료!", 
            $"🎯 {createdCount}개의 보스 공격 애니메이션이 생성되었습니다!\n\n" +
            $"생성된 파일들:\n{createdList}\n" +
            "이제 '모든 보스 Animator Controller 업데이트'를 클릭하세요.", "확인");
    }

    /// <summary>
    /// 애니메이터 컨트롤러에서 특정 상태의 애니메이션 클립 추출
    /// </summary>
    private AnimationClip GetAnimationFromController(UnityEditor.Animations.AnimatorController controller, string stateName)
    {
        foreach (var layer in controller.layers)
        {
            foreach (var state in layer.stateMachine.states)
            {
                if (state.state.name == stateName)
                {
                    return state.state.motion as AnimationClip;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// FBX 파일에서 첫 번째 AnimationClip 추출
    /// </summary>
    private AnimationClip GetAnimationClipFromFBX(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip)
            {
                return clip;
            }
        }
        
        return null;
    }

    /// <summary>
    /// 애니메이션 클립에 적절한 Animation Event 추가
    /// </summary>
    private void AddAnimationEventsToClip(AnimationClip clip, string attackType)
    {
        var events = new System.Collections.Generic.List<AnimationEvent>(clip.events);
        
        // OnAttack1Hit 이벤트 (데미지용) - 60% 지점
        AnimationEvent hitEvent = new AnimationEvent();
        hitEvent.time = clip.length * 0.6f;
        hitEvent.functionName = "OnAttack1Hit";
        events.Add(hitEvent);
        
        // OnAttackComplete 이벤트 (패턴 전환용) - 90% 지점
        AnimationEvent completeEvent = new AnimationEvent();
        completeEvent.time = clip.length * 0.9f;
        completeEvent.functionName = "OnAttackComplete";
        events.Add(completeEvent);
        
        // Unity 2023+ API 사용
        try
        {
            AnimationUtility.SetAnimationEvents(clip, events.ToArray());
            Debug.Log($"[MixamoAnimationEventFixer] {clip.name}에 Animation Event 추가 성공 (새 API)");
        }
        catch (System.Exception)
        {
            // 구버전 호환성
            clip.events = events.ToArray();
            Debug.Log($"[MixamoAnimationEventFixer] {clip.name}에 Animation Event 추가 (구 API)");
        }
    }

    /// <summary>
    /// 모든 보스 Animator Controller를 새 애니메이션으로 업데이트
    /// </summary>
    private void UpdateAllBossAnimatorControllers()
    {
        // 보스별 애니메이션 매핑
        var bossAnimationMapping = new Dictionary<string, Dictionary<string, string>>()
        {
            ["HollyPrist"] = new Dictionary<string, string>()
            {
                ["Attack1"] = "Assets/Animations/BossAttacks/HollyPrist_Attack1_WithEvent.anim",
                ["Attack2"] = "Assets/Animations/BossAttacks/HollyPrist_Attack2_WithEvent.anim"
            },
            ["HollyHuman"] = new Dictionary<string, string>()
            {
                ["Attack1"] = "Assets/Animations/BossAttacks/HollyHuman_Attack1_WithEvent.anim",
                ["Attack2"] = "Assets/Animations/BossAttacks/HollyHuman_Attack2_WithEvent.anim"
            },
            ["HollyBoss"] = new Dictionary<string, string>()
            {
                ["Attack1"] = "Assets/Animations/BossAttacks/HollyBoss_Attack1_WithEvent.anim",
                ["Attack2"] = "Assets/Animations/BossAttacks/HollyBoss_Attack2_WithEvent.anim"
            }
        };

        // 모든 Animator Controller 찾기
        string[] controllerGuids = AssetDatabase.FindAssets("t:AnimatorController", new[] { "Assets" });
        int updatedCount = 0;
        string updateLog = "";

        foreach (string guid in controllerGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UnityEditor.Animations.AnimatorController controller = 
                AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path);

            if (controller != null && IsBossController(controller))
            {
                string controllerName = controller.name;
                string bossType = GetBossTypeFromController(controllerName);
                
                if (bossAnimationMapping.ContainsKey(bossType))
                {
                    var animations = bossAnimationMapping[bossType];
                    
                    // Attack1, Attack2 상태 찾아서 업데이트
                    foreach (var layer in controller.layers)
                    {
                        foreach (var state in layer.stateMachine.states)
                        {
                            string stateName = state.state.name;
                            
                            if (animations.ContainsKey(stateName))
                            {
                                string animPath = animations[stateName];
                                AnimationClip newClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
                                
                                if (newClip != null)
                                {
                                    state.state.motion = newClip;
                                    updateLog += $"• {controllerName}.{stateName} → {newClip.name}\n";
                                    Debug.Log($"[MixamoAnimationEventFixer] {controllerName}의 {stateName} 애니메이션 업데이트: {newClip.name}");
                                }
                                else
                                {
                                    Debug.LogWarning($"[MixamoAnimationEventFixer] 애니메이션을 찾을 수 없습니다: {animPath}");
                                }
                            }
                        }
                    }
                    
                    updatedCount++;
                    EditorUtility.SetDirty(controller);
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료!", 
            $"🎯 {updatedCount}개의 보스 Animator Controller가 업데이트되었습니다!\n\n" +
            $"업데이트 내역:\n{updateLog}\n" +
            "이제 각 보스가 고유한 Attack1, Attack2 애니메이션을 사용하며,\n" +
            "OnAttack1Hit + OnAttackComplete 이벤트가 정상 작동합니다!", "확인");
    }

    /// <summary>
    /// 보스용 Animator Controller인지 확인
    /// </summary>
    private bool IsBossController(UnityEditor.Animations.AnimatorController controller)
    {
        string name = controller.name.ToLower();
        return name.Contains("holly") || name.Contains("boss") || name.Contains("priest");
    }

    /// <summary>
    /// 컨트롤러 이름에서 보스 타입 추출
    /// </summary>
    private string GetBossTypeFromController(string controllerName)
    {
        string name = controllerName.ToLower();
        
        if (name.Contains("hollyprist") || name.Contains("prist"))
            return "HollyPrist";
        else if (name.Contains("hollyhuman") || name.Contains("human"))
            return "HollyHuman";
        else if (name.Contains("hollyboss") || name.Contains("boss"))
            return "HollyBoss";
            
        return "HollyHuman"; // 기본값
    }

    /// <summary>
    /// 기존 Enemy용 Animator Controller 업데이트 (하위 호환성)
    /// </summary>
    private void UpdateEnemyAnimatorControllers()
    {
        // 새 애니메이션 클립 로드
        string newAnimPath = "Assets/Animations/EnemyAttacks/Enemy_Attack1_WithEvent.anim";
        AnimationClip newClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(newAnimPath);
        
        if (newClip == null)
        {
            EditorUtility.DisplayDialog("오류", 
                "Enemy_Attack1_WithEvent.anim을 찾을 수 없습니다.\n먼저 애니메이션을 생성하세요.", "확인");
            return;
        }

        // 모든 Animator Controller 찾기
        string[] controllerGuids = AssetDatabase.FindAssets("t:AnimatorController", new[] { "Assets" });
        int updatedCount = 0;

        foreach (string guid in controllerGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UnityEditor.Animations.AnimatorController controller = 
                AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path);

            if (controller != null && IsEnemyController(controller))
            {
                // Attack1 상태 찾기
                foreach (var layer in controller.layers)
                {
                    foreach (var state in layer.stateMachine.states)
                    {
                        if (state.state.name == "Attack1")
                        {
                            // 애니메이션 클립 교체
                            state.state.motion = newClip;
                            updatedCount++;
                            Debug.Log($"[MixamoAnimationEventFixer] {controller.name}의 Attack1 애니메이션 업데이트");
                            break;
                        }
                    }
                }
                
                EditorUtility.SetDirty(controller);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료!", 
            $"🎯 {updatedCount}개의 Enemy Animator Controller가 업데이트되었습니다!\n\n" +
            "이제 Enemy의 Attack1에서 OnAttack1Hit 이벤트가 정상 작동합니다!", "확인");
    }

    /// <summary>
    /// Enemy용 Animator Controller인지 확인
    /// </summary>
    private bool IsEnemyController(UnityEditor.Animations.AnimatorController controller)
    {
        string name = controller.name.ToLower();
        return name.Contains("enemy") || 
               name.Contains("cultist") || 
               name.Contains("fanatic");
    }
}
#endif 