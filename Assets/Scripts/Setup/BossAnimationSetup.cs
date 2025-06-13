#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// 보스 애니메이션 자동 설정 도구
/// Mixamo 애니메이션을 활용한 보스 전용 애니메이터 컨트롤러 생성
/// </summary>
public class BossAnimationSetup : EditorWindow
{
    [MenuItem("Window/VR Horror Game/Boss Animation Setup")]
    public static void ShowWindow()
    {
        GetWindow<BossAnimationSetup>("Boss Animation Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("보스 애니메이션 설정", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "보스 애니메이션을 자동으로 설정합니다:\n\n" +
            "1. Mixamo 애니메이션 기반 애니메이터 컨트롤러 생성\n" +
            "2. 단계별 애니메이션 상태 설정\n" +
            "3. 공격 패턴별 애니메이션 연결\n" +
            "4. VR 최적화 파라미터 설정", 
            MessageType.Info);

        GUILayout.Space(15);

        if (GUILayout.Button("🎭 보스 애니메이터 컨트롤러 생성", GUILayout.Height(40)))
        {
            CreateBossAnimatorController();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("📋 보스 애니메이션 가이드 출력", GUILayout.Height(30)))
        {
            PrintBossAnimationGuide();
        }

        GUILayout.Space(20);

        EditorGUILayout.LabelField("Mixamo 권장 애니메이션:", EditorStyles.boldLabel);
        
        string[] recommendedAnimations = {
            "🚶 기본 이동:",
            "- Walking (기본 걸음)",
            "- Running (달리기)",
            "- Idle (대기)",
            "",
            "⚔️ 공격 애니메이션:",
            "- Punching (기본 공격)",
            "- Heavy Attack (강공격)",
            "- Sword And Shield Slash (무기 공격)",
            "- Standing React Death Backward (사망)",
            "",
            "🔥 특수 패턴:",
            "- Charge (돌진)",
            "- AOE Attack (범위 공격)",
            "- Roaring (분노/위협)",
            ""
        };

        foreach (string line in recommendedAnimations)
        {
            if (line.StartsWith("🎬") || line.StartsWith("🚶") || line.StartsWith("⚔️") || line.StartsWith("🔥"))
            {
                GUILayout.Label(line, EditorStyles.boldLabel);
            }
            else if (!string.IsNullOrEmpty(line))
            {
                GUILayout.Label(line);
            }
            else
            {
                GUILayout.Space(5);
            }
        }
    }

    static void CreateBossAnimatorController()
    {
        // 애니메이터 컨트롤러 생성
        string path = "Assets/Animations/BossAnimatorController.controller";
        
        // 디렉토리 생성
        System.IO.Directory.CreateDirectory("Assets/Animations");
        
        // 애니메이터 컨트롤러 생성
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        
        // 파라미터 추가
        AddBossAnimatorParameters(controller);
        
        // 상태 생성
        CreateBossAnimatorStates(controller);
        
        // 트랜지션 설정
        SetupBossAnimatorTransitions(controller);
        
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        
        Debug.Log("[BossAnimationSetup] 보스 애니메이터 컨트롤러 생성 완료: " + path);
        
        EditorUtility.DisplayDialog("보스 애니메이션 설정 완료", 
            "보스 애니메이터 컨트롤러가 생성되었습니다!\n\n" +
            "경로: " + path + "\n\n" +
            "이제 다음 단계를 진행하세요:\n" +
            "1. Mixamo에서 권장 애니메이션 다운로드\n" +
            "2. 보스 모델에 애니메이터 컨트롤러 연결\n" +
            "3. 애니메이션 클립을 각 상태에 할당", 
            "확인");
    }

    static void AddBossAnimatorParameters(AnimatorController controller)
    {
        // Float 파라미터
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        
        // Int 파라미터
        controller.AddParameter("BossPhase", AnimatorControllerParameterType.Int);
        
        // Bool 파라미터 - 상태 관련
        controller.AddParameter("IsPatrolling", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsObserving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsApproaching", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsInCombat", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsRetreating", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsStunned", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsRaging", AnimatorControllerParameterType.Bool);
        
        // Trigger 파라미터 - 액션 관련
        controller.AddParameter("BasicAttack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("HeavyAttack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("HeavyAttackCharge", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("ChargeStart", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("ChargeAttack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AreaAttack", AnimatorControllerParameterType.Trigger);

        controller.AddParameter("Rage", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        
        // Phase Transition 트리거들
        controller.AddParameter("PhaseTransition1", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("PhaseTransition2", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("PhaseTransition3", AnimatorControllerParameterType.Trigger);
        
        Debug.Log("[BossAnimationSetup] 애니메이터 파라미터 추가 완료");
    }

    static void CreateBossAnimatorStates(AnimatorController controller)
    {
        // Base Layer 가져오기
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        
        // 상태 생성
        AnimatorState idleState = stateMachine.AddState("Idle");
        AnimatorState walkState = stateMachine.AddState("Walk");
        AnimatorState runState = stateMachine.AddState("Run");
        
        // 공격 상태들
        AnimatorState basicAttackState = stateMachine.AddState("Basic Attack");
        AnimatorState heavyAttackChargeState = stateMachine.AddState("Heavy Attack Charge");
        AnimatorState heavyAttackState = stateMachine.AddState("Heavy Attack");
        AnimatorState chargeStartState = stateMachine.AddState("Charge Start");
        AnimatorState chargeAttackState = stateMachine.AddState("Charge Attack");
        AnimatorState areaAttackState = stateMachine.AddState("Area Attack");

        AnimatorState rageState = stateMachine.AddState("Rage");
        
        // 피격/사망
        AnimatorState hitState = stateMachine.AddState("Hit");
        AnimatorState deathState = stateMachine.AddState("Death");
        
        // 단계 변화
        AnimatorState phaseTransition1State = stateMachine.AddState("Phase Transition 1");
        AnimatorState phaseTransition2State = stateMachine.AddState("Phase Transition 2");
        AnimatorState phaseTransition3State = stateMachine.AddState("Phase Transition 3");
        
        // 기본 상태 설정
        stateMachine.defaultState = idleState;
        
        Debug.Log("[BossAnimationSetup] 애니메이터 상태 생성 완료");
    }

    static void SetupBossAnimatorTransitions(AnimatorController controller)
    {
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        
        // 상태 찾기 (수정된 버전)
        ChildAnimatorState[] childStates = stateMachine.states;
        AnimatorState idleState = null;
        AnimatorState walkState = null;
        AnimatorState runState = null;
        AnimatorState basicAttackState = null;
        AnimatorState hitState = null;
        AnimatorState deathState = null;
        
        foreach (var stateInfo in childStates)
        {
            switch (stateInfo.state.name)
            {
                case "Idle": idleState = stateInfo.state; break;
                case "Walk": walkState = stateInfo.state; break;
                case "Run": runState = stateInfo.state; break;
                case "Basic Attack": basicAttackState = stateInfo.state; break;
                case "Hit": hitState = stateInfo.state; break;
                case "Death": deathState = stateInfo.state; break;
            }
        }
        
        // 기본 이동 트랜지션 (예시)
        if (idleState != null && walkState != null)
        {
            var transition = idleState.AddTransition(walkState);
            transition.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            transition.duration = 0.2f;
            
            var backTransition = walkState.AddTransition(idleState);
            backTransition.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            backTransition.duration = 0.2f;
        }
        
        // Any State에서 특정 상태로의 트랜지션
        if (hitState != null)
        {
            var hitTransition = stateMachine.AddAnyStateTransition(hitState);
            hitTransition.AddCondition(AnimatorConditionMode.If, 0, "Hit");
            hitTransition.duration = 0.1f;
        }
        
        if (deathState != null)
        {
            var deathTransition = stateMachine.AddAnyStateTransition(deathState);
            deathTransition.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            deathTransition.duration = 0.3f;
            deathTransition.canTransitionToSelf = false;
        }
        
        Debug.Log("[BossAnimationSetup] 애니메이터 트랜지션 설정 완료");
    }

    static void PrintBossAnimationGuide()
    {
        string guide = @"
=== 🏆 VR 호러 게임 보스 애니메이션 가이드 ===

📋 필수 애니메이션 클립 (Mixamo 권장):

🎬 등장/연출:
- Phase Transition 1~3: Roaring 또는 Power Up 애니메이션

🚶 기본 이동:
- Idle: Standing Idle 또는 Breathing Idle
- Walk: Walking 또는 Sneaking Walk
- Run: Running 또는 Sprint

⚔️ 기본 공격:
- Basic Attack: Punching 또는 Sword Slash
- Heavy Attack Charge: Sword And Shield Idle (준비 동작)
- Heavy Attack: Heavy Attack 또는 Strong Punch

🔥 특수 공격 (2-3단계):
- Charge Start: Running Start 모션
- Charge Attack: Shoulder Bash 또는 Bull Rush
- Area Attack: AOE Slam 또는 Ground Pound

- Rage: Roaring 또는 Intimidating

💥 피격/사망:
- Hit: Hit Reaction 또는 Flinch
- Death: Standing React Death Backward 또는 Dramatic Death

🎮 애니메이터 설정 팁:

1. 트리거 타이밍:
   - 공격 애니메이션에서 실제 데미지 타이밍 맞추기
   - Animation Events로 공격 포인트 설정

2. 트랜지션 최적화:
   - Has Exit Time 체크 해제 (즉시 반응)
   - Transition Duration: 0.1-0.3초로 설정

3. VR 최적화:
   - 애니메이션 품질: Optimal (메모리 절약)
   - Compression: Keyframe Reduction

4. 단계별 차별화:
   - 1단계: 느린 공격 (1-2초 간격)
   - 2단계: 빠른 공격 + 돌진
   - 3단계: 연속 공격 + 특수 패턴

🔧 설정 순서:
1. Mixamo에서 애니메이션 다운로드
2. Unity로 임포트 (Humanoid Rig)
3. 생성된 애니메이터 컨트롤러에 할당
4. Animation Events 설정 (MixamoAnimationEventFixer 사용)
5. BossAI 스크립트와 연동 테스트

💡 VR 고려사항:
- 공격 예고 동작을 명확하게 (플레이어가 회피할 수 있도록)
- 과도한 화면 흔들림 방지 (멀미 유발 가능)
- 햅틱 피드백과 애니메이션 타이밍 동기화
";

        Debug.Log(guide);
        
        EditorUtility.DisplayDialog("보스 애니메이션 가이드", 
            "보스 애니메이션 가이드가 Console 창에 출력되었습니다!\n\n" +
            "Console 창을 확인하여 상세한 설정 방법을 확인하세요.", 
            "확인");
    }
}
#endif 