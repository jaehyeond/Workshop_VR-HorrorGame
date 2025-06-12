using UnityEngine;
using UnityEditor;
using System.IO;

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
            "대신 새로운 AnimationClip을 생성하여 Animation Event를 추가합니다.", 
            MessageType.Info);

        GUILayout.Space(15);

        if (GUILayout.Button("Mixamo 공격 애니메이션을 복사하고 Event 추가", GUILayout.Height(40)))
        {
            CreateEditableAttackAnimations();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("모든 Enemy Animator Controller 업데이트", GUILayout.Height(30)))
        {
            UpdateEnemyAnimatorControllers();
        }

        GUILayout.Space(15);

        EditorGUILayout.HelpBox(
            "이 도구는:\n" +
            "1. Standing Melee Attack Downward를 복사하여 수정 가능한 .anim 파일 생성\n" +
            "2. 새 애니메이션에 OnAttack1Hit Animation Event 추가\n" +
            "3. 모든 Enemy Animator Controller를 새 애니메이션으로 업데이트", 
            MessageType.None);
    }

    /// <summary>
    /// Mixamo 공격 애니메이션을 복사하고 Animation Event 추가
    /// </summary>
    private void CreateEditableAttackAnimations()
    {
        // Mixamo 애니메이션 찾기
        string mixamoPath = "Assets/Jaehyeon/Animations/Animation(Player)/Standing Melee Attack Downward.fbx";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(mixamoPath);
        
        AnimationClip originalClip = null;
        
        // 디버그: FBX 파일 내의 모든 애셋 확인
        Debug.Log($"[MixamoAnimationEventFixer] FBX 파일 내 애셋들:");
        foreach (Object asset in assets)
        {
            Debug.Log($"  - {asset.name} ({asset.GetType().Name})");
            if (asset is AnimationClip clip)
            {
                Debug.Log($"    → AnimationClip 발견: {clip.name}");
                if (originalClip == null) // 첫 번째 AnimationClip 사용
                {
                    originalClip = clip;
                }
            }
        }

        if (originalClip == null)
        {
            string assetList = "";
            foreach (Object asset in assets)
            {
                assetList += $"• {asset.name} ({asset.GetType().Name})\n";
            }
            
            EditorUtility.DisplayDialog("오류", 
                $"Standing Melee Attack Downward.fbx에서 AnimationClip을 찾을 수 없습니다.\n\n" +
                $"FBX 파일 내 애셋들:\n{assetList}", "확인");
            return;
        }
        
        Debug.Log($"[MixamoAnimationEventFixer] 사용할 애니메이션: {originalClip.name}");

        // 새 디렉토리 생성
        string newDir = "Assets/Animations/EnemyAttacks";
        if (!Directory.Exists(newDir))
        {
            Directory.CreateDirectory(newDir);
        }

        // 새 애니메이션 클립 생성
        AnimationClip newClip = new AnimationClip();
        newClip.name = "Enemy_Attack1_WithEvent";
        
        // 원본 애니메이션의 모든 커브를 복사
        EditorUtility.CopySerialized(originalClip, newClip);
        
        // Animation Event 추가 (Unity 2023+ 호환)
        AnimationEvent attackEvent = new AnimationEvent();
        attackEvent.time = originalClip.length * 0.6f; // 60% 지점
        attackEvent.functionName = "OnAttack1Hit";
        
        // Unity 2023+ API 사용
        try
        {
            // 새로운 API 시도
            var events = new System.Collections.Generic.List<AnimationEvent>(newClip.events);
            events.Add(attackEvent);
            AnimationUtility.SetAnimationEvents(newClip, events.ToArray());
            Debug.Log("[MixamoAnimationEventFixer] 새 API로 Animation Event 추가 성공");
        }
        catch (System.Exception)
        {
            // 구버전 호환성
            var events = new System.Collections.Generic.List<AnimationEvent>(newClip.events);
            events.Add(attackEvent);
            newClip.events = events.ToArray();
            Debug.Log("[MixamoAnimationEventFixer] 구 API로 Animation Event 추가");
        }

        // 애니메이션 파일 저장
        string newPath = $"{newDir}/Enemy_Attack1_WithEvent.anim";
        AssetDatabase.CreateAsset(newClip, newPath);

        Debug.Log($"[MixamoAnimationEventFixer] 새 애니메이션 생성: {newPath}");
        
        EditorUtility.DisplayDialog("완료!", 
            "Enemy_Attack1_WithEvent.anim 파일이 생성되었습니다!\n" +
            "이제 '모든 Enemy Animator Controller 업데이트'를 클릭하세요.", "확인");
    }

    /// <summary>
    /// 모든 Enemy Animator Controller를 새 애니메이션으로 업데이트
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
               name.Contains("fanatic") || 
               name.Contains("priest") || 
               name.Contains("boss") ||
               name.Contains("holly"); // HollyHuman.controller도 포함
    }
}
#endif 