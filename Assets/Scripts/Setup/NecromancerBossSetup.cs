#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.Collections.Generic;
using UnityEditor.Animations;

/// <summary>
/// 네크로맨서 보스 설정 도구
/// Unity Editor에서 원클릭으로 모든 컴포넌트 설정
/// </summary>
public class NecromancerBossSetup : EditorWindow
{
    private GameObject necromancerPrefab;
    private GameObject targetObject;
    private bool showAdvancedOptions = false;
    
    // 설정 옵션들
    private float maxHealth = 400f;
    private float walkSpeed = 2f;
    private float runSpeed = 4f;
    private float detectionRange = 12f;
    private float attackRange = 3f;
    private float spellRange = 8f;
    private bool useExistingAnimatorController = true; // 기존 애니메이션 컨트롤러 사용
    
    [MenuItem("VR Horror Game/Necromancer Boss/Setup Tool")]
    public static void ShowWindow()
    {
        NecromancerBossSetup window = GetWindow<NecromancerBossSetup>("Necromancer Boss Setup");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }
    
    void OnGUI()
    {
        GUILayout.Label("네크로맨서 보스 설정 도구", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        DrawPrefabSelection();
        GUILayout.Space(10);
        
        DrawTargetSelection();
        GUILayout.Space(10);
        
        DrawSetupOptions();
        GUILayout.Space(10);
        
        DrawSetupButtons();
        GUILayout.Space(10);
        
        DrawStatusInfo();
    }
    
    void DrawPrefabSelection()
    {
        GUILayout.Label("1. 네크로맨서 프리팹 선택", EditorStyles.boldLabel);
        
        necromancerPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Necromancer Prefab", 
            necromancerPrefab, 
            typeof(GameObject), 
            false
        );
        
        if (necromancerPrefab == null)
        {
            if (GUILayout.Button("자동으로 네크로맨서 프리팹 찾기"))
            {
                FindNecromancerPrefab();
            }
        }
        
        if (necromancerPrefab != null)
        {
            EditorGUILayout.HelpBox("✓ 네크로맨서 프리팹이 선택되었습니다.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("네크로맨서 프리팹을 선택해주세요.", MessageType.Warning);
        }
    }
    
    void DrawTargetSelection()
    {
        GUILayout.Label("2. 설정할 오브젝트 선택", EditorStyles.boldLabel);
        
        targetObject = (GameObject)EditorGUILayout.ObjectField(
            "Target Object", 
            targetObject, 
            typeof(GameObject), 
            true
        );
        
        if (targetObject == null && Selection.activeGameObject != null)
        {
            if (GUILayout.Button("현재 선택된 오브젝트 사용"))
            {
                targetObject = Selection.activeGameObject;
            }
        }
        
        if (targetObject != null)
        {
            EditorGUILayout.HelpBox($"✓ 설정 대상: {targetObject.name}", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("설정할 오브젝트를 선택해주세요.", MessageType.Warning);
        }
    }
    
    void DrawSetupOptions()
    {
        GUILayout.Label("3. 설정 옵션", EditorStyles.boldLabel);
        
        showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "고급 설정");
        
        if (showAdvancedOptions)
        {
            EditorGUI.indentLevel++;
            
            GUILayout.Label("체력 설정", EditorStyles.miniBoldLabel);
            maxHealth = EditorGUILayout.FloatField("최대 체력", maxHealth);
            
            GUILayout.Space(5);
            GUILayout.Label("이동 설정", EditorStyles.miniBoldLabel);
            walkSpeed = EditorGUILayout.FloatField("걷기 속도", walkSpeed);
            runSpeed = EditorGUILayout.FloatField("달리기 속도", runSpeed);
            
            GUILayout.Space(5);
            GUILayout.Label("전투 설정", EditorStyles.miniBoldLabel);
            detectionRange = EditorGUILayout.FloatField("감지 범위", detectionRange);
            attackRange = EditorGUILayout.FloatField("공격 범위", attackRange);
            spellRange = EditorGUILayout.FloatField("마법 범위", spellRange);
            
            GUILayout.Space(5);
            GUILayout.Label("애니메이션 설정", EditorStyles.miniBoldLabel);
            useExistingAnimatorController = EditorGUILayout.Toggle("기존 애니메이션 컨트롤러 사용", useExistingAnimatorController);
            
            if (useExistingAnimatorController)
            {
                EditorGUILayout.HelpBox("기존 네크로맨서 애니메이션 컨트롤러를 그대로 사용합니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("새로운 애니메이션 컨트롤러를 생성합니다.", MessageType.Warning);
            }
            
            EditorGUI.indentLevel--;
        }
    }
    
    void DrawSetupButtons()
    {
        GUILayout.Label("4. 설정 실행", EditorStyles.boldLabel);
        
        EditorGUI.BeginDisabledGroup(!CanSetup());
        
        if (GUILayout.Button("완전한 네크로맨서 보스 설정", GUILayout.Height(30)))
        {
            try
            {
                SetupCompleteBoss();
                Repaint();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"보스 설정 중 오류: {e.Message}");
            }
        }
        
        GUILayout.Space(5);
        
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("기본 컴포넌트만"))
        {
            try
            {
                SetupBasicComponents();
                Repaint();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"컴포넌트 설정 중 오류: {e.Message}");
            }
        }
        
        if (GUILayout.Button("애니메이션 설정"))
        {
            try
            {
                SetupAnimationController();
                Repaint();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"애니메이션 설정 중 오류: {e.Message}");
            }
        }
        
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("NavMesh 설정"))
        {
            try
            {
                SetupNavMeshAgent();
                Repaint();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"NavMesh 설정 중 오류: {e.Message}");
            }
        }
        
        if (GUILayout.Button("충돌체 설정"))
        {
            try
            {
                SetupColliders();
                Repaint();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"충돌체 설정 중 오류: {e.Message}");
            }
        }
        
        GUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        
        // 애니메이션 이벤트 생성 버튼
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("애니메이션 이벤트 생성 (WithEvent 파일)", GUILayout.Height(25)))
        {
            try
            {
                AddNecromancerAnimationEvents();
                Repaint();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"애니메이션 이벤트 생성 중 오류: {e.Message}");
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUI.EndDisabledGroup();
    }
    
    void DrawStatusInfo()
    {
        GUILayout.Label("5. 상태 정보", EditorStyles.boldLabel);
        
        if (targetObject != null)
        {
            var components = GetBossComponents(targetObject);
            
            DrawComponentStatus("NecromancerBoss", components.boss != null);
            DrawComponentStatus("NecromancerAnimationController", components.animController != null);
            DrawComponentStatus("NecromancerCombatSystem", components.combatSystem != null);
            DrawComponentStatus("NecromancerMovement", components.movement != null);
            DrawComponentStatus("NecromancerHealth", components.health != null);
            DrawComponentStatus("NecromancerVFXController", components.vfx != null);
            DrawComponentStatus("Animator", components.animator != null);
            DrawComponentStatus("NavMeshAgent", components.navAgent != null);
            DrawComponentStatus("Collider", components.collider != null);
        }
    }
    
    void DrawComponentStatus(string componentName, bool exists)
    {
        GUILayout.BeginHorizontal();
        
        if (exists)
        {
            GUILayout.Label("✓", GUILayout.Width(20));
            GUI.color = Color.green;
        }
        else
        {
            GUILayout.Label("✗", GUILayout.Width(20));
            GUI.color = Color.red;
        }
        
        GUILayout.Label(componentName);
        GUI.color = Color.white;
        
        GUILayout.EndHorizontal();
    }
    
    bool CanSetup()
    {
        try
        {
            return necromancerPrefab != null && 
                   targetObject != null && 
                   !EditorApplication.isPlaying &&
                   !EditorApplication.isCompiling;
        }
        catch
        {
            return false;
        }
    }
    
    void FindNecromancerPrefab()
    {
        string[] guids = AssetDatabase.FindAssets("Necromanser Prefab t:GameObject");
        
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            necromancerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            Debug.Log($"네크로맨서 프리팹을 찾았습니다: {path}");
        }
        else
        {
            Debug.LogWarning("네크로맨서 프리팹을 찾을 수 없습니다.");
        }
    }
    
    void SetupCompleteBoss()
    {
        if (!CanSetup()) return;
        
        try
        {
            Undo.RecordObject(targetObject, "Setup Necromancer Boss");
            
            // 1. 기본 컴포넌트 설정
            SetupBasicComponents();
            
            // 2. 애니메이션 설정
            SetupAnimationController();
            
            // 3. NavMesh 설정
            SetupNavMeshAgent();
            
            // 4. 충돌체 설정
            SetupColliders();
            
                    // 5. 애니메이션 이벤트 추가
        AddNecromancerAnimationEvents();
        
        // 6. 설정값 적용
        ApplySettings();
        
        // 7. 변경사항 저장
        EditorUtility.SetDirty(targetObject);
        AssetDatabase.SaveAssets();
            
            Debug.Log($"네크로맨서 보스 설정 완료: {targetObject.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"네크로맨서 보스 설정 중 오류 발생: {e.Message}");
            Debug.LogError($"스택 트레이스: {e.StackTrace}");
        }
    }
    
    void SetupBasicComponents()
    {
        if (!CanSetup()) return;
        
        // 메인 보스 컨트롤러
        if (targetObject.GetComponent<NecromancerBoss>() == null)
        {
            targetObject.AddComponent<NecromancerBoss>();
        }
        
        // 애니메이션 컨트롤러
        if (targetObject.GetComponent<NecromancerAnimationController>() == null)
        {
            targetObject.AddComponent<NecromancerAnimationController>();
        }
        
        // 전투 시스템
        if (targetObject.GetComponent<NecromancerCombatSystem>() == null)
        {
            targetObject.AddComponent<NecromancerCombatSystem>();
        }
        
        // 이동 시스템
        if (targetObject.GetComponent<NecromancerMovement>() == null)
        {
            targetObject.AddComponent<NecromancerMovement>();
        }
        
        // 체력 시스템
        if (targetObject.GetComponent<NecromancerHealth>() == null)
        {
            targetObject.AddComponent<NecromancerHealth>();
        }
        
        // VFX 컨트롤러
        if (targetObject.GetComponent<NecromancerVFXController>() == null)
        {
            targetObject.AddComponent<NecromancerVFXController>();
        }
        
        Debug.Log("기본 컴포넌트 설정 완료");
    }
    
    void SetupAnimationController()
    {
        if (!CanSetup()) return;
        
        // Animator 컴포넌트 추가
        Animator animator = targetObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = targetObject.AddComponent<Animator>();
        }
        
        // 기존 애니메이션 컨트롤러 사용 여부에 따른 처리
        if (useExistingAnimatorController)
        {
            // 기존 컨트롤러가 이미 있으면 유지
            if (animator.runtimeAnimatorController != null)
            {
                Debug.Log($"기존 애니메이션 컨트롤러 사용: {animator.runtimeAnimatorController.name}");
            }
            else
            {
                // 네크로맨서 프리팹에서 컨트롤러 복사
                if (necromancerPrefab != null)
                {
                    Animator prefabAnimator = necromancerPrefab.GetComponent<Animator>();
                    if (prefabAnimator != null && prefabAnimator.runtimeAnimatorController != null)
                    {
                        animator.runtimeAnimatorController = prefabAnimator.runtimeAnimatorController;
                        Debug.Log($"프리팹에서 애니메이션 컨트롤러 복사: {prefabAnimator.runtimeAnimatorController.name}");
                    }
                    else
                    {
                        Debug.LogWarning("네크로맨서 프리팹에 애니메이션 컨트롤러가 없습니다.");
                    }
                }
                else
                {
                    Debug.LogWarning("네크로맨서 프리팹이 선택되지 않았습니다.");
                }
            }
        }
        else
        {
            // 새 컨트롤러 찾기 (향후 구현)
            string[] controllerGuids = AssetDatabase.FindAssets("Necromanser Animator Controller t:AnimatorController");
            if (controllerGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(controllerGuids[0]);
                RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                animator.runtimeAnimatorController = controller;
                
                Debug.Log($"새 애니메이션 컨트롤러 설정: {path}");
            }
            else
            {
                Debug.LogWarning("새 네크로맨서 애니메이션 컨트롤러를 찾을 수 없습니다.");
            }
        }
        
        // NecromancerAnimationController의 호환 모드 설정
        var animController = targetObject.GetComponent<NecromancerAnimationController>();
        if (animController != null)
        {
            // 리플렉션을 사용해서 useDirectAnimationPlay 설정
            var field = typeof(NecromancerAnimationController).GetField("useDirectAnimationPlay", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(animController, useExistingAnimatorController);
                Debug.Log($"애니메이션 호환 모드 설정: {useExistingAnimatorController}");
            }
        }
    }
    
    void SetupNavMeshAgent()
    {
        if (!CanSetup()) return;
        
        NavMeshAgent agent = targetObject.GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = targetObject.AddComponent<NavMeshAgent>();
        }
        
        // NavMeshAgent 기본 설정
        agent.speed = walkSpeed;
        agent.angularSpeed = 300f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 2f;
        agent.radius = 0.5f;
        agent.height = 2f;
        
        Debug.Log("NavMeshAgent 설정 완료");
    }
    
    void SetupColliders()
    {
        if (!CanSetup()) return;
        
        // 메인 충돌체 (캡슐)
        CapsuleCollider mainCollider = targetObject.GetComponent<CapsuleCollider>();
        if (mainCollider == null)
        {
            mainCollider = targetObject.AddComponent<CapsuleCollider>();
        }
        
        mainCollider.center = new Vector3(0, 1f, 0);
        mainCollider.radius = 0.5f;
        mainCollider.height = 2f;
        mainCollider.isTrigger = false;
        
        // 공격 감지용 트리거 (구체)
        GameObject triggerObj = new GameObject("AttackTrigger");
        triggerObj.transform.SetParent(targetObject.transform);
        triggerObj.transform.localPosition = Vector3.zero;
        
        SphereCollider triggerCollider = triggerObj.AddComponent<SphereCollider>();
        triggerCollider.radius = attackRange;
        triggerCollider.isTrigger = true;
        
        // NecromancerCombatSystem은 메인 오브젝트에 유지 (Enemy와 동일한 구조)
        // Animation Event가 Animator와 같은 오브젝트의 컴포넌트에서만 호출되기 때문
        NecromancerCombatSystem combatSystem = targetObject.GetComponent<NecromancerCombatSystem>();
        if (combatSystem == null)
        {
            // 메인 오브젝트에 추가
            targetObject.AddComponent<NecromancerCombatSystem>();
        }
        
        Debug.Log("충돌체 설정 완료");
    }
    
    void ApplySettings()
    {
        if (!CanSetup()) return;
        
        var components = GetBossComponents(targetObject);
        
        // 보스 설정
        if (components.boss != null)
        {
            components.boss.maxHealth = maxHealth;
            components.boss.detectionRange = detectionRange;
            components.boss.attackRange = attackRange;
            components.boss.specialAttackRange = spellRange;
        }
        
        // 이동 설정
        if (components.movement != null)
        {
            components.movement.walkSpeed = walkSpeed;
            components.movement.runSpeed = runSpeed;
        }
        
        // 체력 설정
        if (components.health != null)
        {
            components.health.maxHealth = maxHealth;
        }
        
        Debug.Log("설정값 적용 완료");
        
        // Necromancer 애니메이션 이벤트 추가
        AddNecromancerAnimationEvents();
    }
    
    void AddNecromancerAnimationEvents()
    {
        try
        {
            Debug.Log("네크로맨서 애니메이션 이벤트 추가 시작...");
            
            // FBX 파일에서 WithEvent 애니메이션 파일 생성
            CreateAnimationWithEvents("atack1", 1.2f);
            CreateAnimationWithEvents("atack2", 1.0f);
            
            Debug.Log("네크로맨서 애니메이션 이벤트 추가 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"애니메이션 이벤트 추가 중 오류: {e.Message}");
        }
    }
    
    void CreateAnimationWithEvents(string animationName, float eventTime)
    {
        try
        {
            // 1. 네크로맨서 FBX 파일 찾기
            Debug.Log($"'{animationName}' 애니메이션 검색 시작...");
            
            // 먼저 모든 네크로맨서 관련 파일 검색
            string[] allGuids = AssetDatabase.FindAssets("Necromancer t:Object", new[] { "Assets/Necromanser" });
            AnimationClip originalClip = null;
            string fbxPath = "";
            
            foreach (string guid in allGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".fbx") && path.Contains(animationName))
                {
                    fbxPath = path;
                    Debug.Log($"FBX 파일 확인: {path}");
                    
                    // FBX 파일 내부의 모든 에셋 로드
                    Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    Debug.Log($"FBX 내부 에셋 개수: {assets.Length}");
                    
                    foreach (Object asset in assets)
                    {
                        if (asset is AnimationClip clip)
                        {
                            Debug.Log($"발견된 애니메이션 클립: {clip.name}");
                            if (clip.name == animationName)
                            {
                                originalClip = clip;
                                Debug.Log($"✓ 매칭된 애니메이션: {clip.name} (경로: {path})");
                                break;
                            }
                        }
                    }
                    if (originalClip != null) break;
                }
            }
            
            if (originalClip == null)
            {
                Debug.LogWarning($"{animationName} 애니메이션을 찾을 수 없습니다. FBX 파일을 확인해주세요.");
                return;
            }
            
            // 2. 새로운 애니메이션 파일 경로
            string newPath = $"Assets/Animations/BossAttacks/{animationName}_WithEvent.anim";
            
            // 3. 디렉토리 생성
            string directory = System.IO.Path.GetDirectoryName(newPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            // 4. 기존 파일이 있으면 삭제
            if (System.IO.File.Exists(newPath))
            {
                AssetDatabase.DeleteAsset(newPath);
            }
            
            // 5. 새로운 애니메이션 클립 생성
            AnimationClip newClip = new AnimationClip();
            newClip.name = $"{animationName}_WithEvent";
            
            // 6. 원본 클립의 설정 복사
            EditorUtility.CopySerialized(originalClip, newClip);
            
            // 7. Animation Event 추가
            AnimationEvent attackEvent = new AnimationEvent();
            attackEvent.time = eventTime;
            attackEvent.functionName = "OnAttack1Hit";
            
            AnimationEvent completeEvent = new AnimationEvent();
            completeEvent.time = originalClip.length * 0.9f;
            completeEvent.functionName = "OnAttackComplete";
            
            // 8. 이벤트 배열 설정
            AnimationEvent[] events = { attackEvent, completeEvent };
            AnimationUtility.SetAnimationEvents(newClip, events);
            
            // 9. 파일로 저장
            AssetDatabase.CreateAsset(newClip, newPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"{animationName}_WithEvent.anim 생성 완료: {newPath}");
            
            // 10. 애니메이션 컨트롤러 업데이트
            UpdateAnimatorController(animationName, newClip);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{animationName} WithEvent 생성 중 오류: {e.Message}");
        }
    }
    
    void UpdateAnimatorController(string originalAnimationName, AnimationClip newClip)
    {
        try
        {
            // 네크로맨서 애니메이션 컨트롤러 찾기
            string[] controllerGuids = AssetDatabase.FindAssets("Necromanser Animator Controller t:AnimatorController");
            if (controllerGuids.Length == 0)
            {
                Debug.LogWarning("네크로맨서 애니메이션 컨트롤러를 찾을 수 없습니다.");
                return;
            }
            
            string controllerPath = AssetDatabase.GUIDToAssetPath(controllerGuids[0]);
            UnityEditor.Animations.AnimatorController controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
            
            if (controller == null)
            {
                Debug.LogWarning("애니메이션 컨트롤러를 로드할 수 없습니다.");
                return;
            }
            
            // 모든 레이어의 상태를 확인하고 업데이트
            foreach (var layer in controller.layers)
            {
                UpdateStatesInStateMachine(layer.stateMachine, originalAnimationName, newClip);
            }
            
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"애니메이션 컨트롤러에서 {originalAnimationName} → {newClip.name} 업데이트 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"애니메이션 컨트롤러 업데이트 중 오류: {e.Message}");
        }
    }
    
    void UpdateStatesInStateMachine(UnityEditor.Animations.AnimatorStateMachine stateMachine, string originalAnimationName, AnimationClip newClip)
    {
        // 현재 상태 머신의 모든 상태 확인
        foreach (var state in stateMachine.states)
        {
            if (state.state.motion != null && 
                (state.state.motion.name == originalAnimationName || 
                 state.state.motion.name.Contains(originalAnimationName) ||
                 state.state.name.ToLower().Contains(originalAnimationName.ToLower())))
            {
                state.state.motion = newClip;
                Debug.Log($"상태 '{state.state.name}'의 애니메이션을 {newClip.name}으로 업데이트");
            }
        }
        
        // 하위 상태 머신도 재귀적으로 확인
        foreach (var childStateMachine in stateMachine.stateMachines)
        {
            UpdateStatesInStateMachine(childStateMachine.stateMachine, originalAnimationName, newClip);
        }
    }
    
    (NecromancerBoss boss, 
     NecromancerAnimationController animController,
     NecromancerCombatSystem combatSystem,
     NecromancerMovement movement,
     NecromancerHealth health,
     NecromancerVFXController vfx,
     Animator animator,
     NavMeshAgent navAgent,
     Collider collider) GetBossComponents(GameObject obj)
    {
        return (
            obj.GetComponent<NecromancerBoss>(),
            obj.GetComponent<NecromancerAnimationController>(),
            obj.GetComponent<NecromancerCombatSystem>(),
            obj.GetComponent<NecromancerMovement>(),
            obj.GetComponent<NecromancerHealth>(),
            obj.GetComponent<NecromancerVFXController>(),
            obj.GetComponent<Animator>(),
            obj.GetComponent<NavMeshAgent>(),
            obj.GetComponent<Collider>()
        );
    }
}
#endif 