using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// VR 공격 시스템 실시간 디버깅 도구
/// Enemy 공격 → 플레이어 피격 → VR 효과 체인을 실시간 모니터링
/// </summary>
public class VRAttackDebugger : MonoBehaviour
{
    [Header("디버그 UI")]
    public TextMeshProUGUI debugText;
    public bool enableDebugUI = true;
    
    [Header("강제 테스트")]
    public float testDamageAmount = 25f;
    
    // New Input System
    private InputAction testDamageAction;
    
    // 참조
    private VRPlayerHealth vrPlayerHealth;
    private VRPostProcessingManager postProcessingManager;
    private OVRCameraRig cameraRig;
    
    // 디버그 정보
    private string debugInfo = "";
    private float lastDamageTime;
    private int damageCount = 0;
    
    void Start()
    {
        FindComponents();
        CreateDebugUI();
        SetupInputSystem();
    }
    
    void OnEnable()
    {
        testDamageAction?.Enable();
    }
    
    void OnDisable()
    {
        testDamageAction?.Disable();
    }
    
    void OnDestroy()
    {
        testDamageAction?.Dispose();
    }
    
    void Update()
    {
        UpdateDebugInfo();
        
        if (enableDebugUI && debugText != null)
        {
            debugText.text = debugInfo;
        }
    }
    
    /// <summary>
    /// 필요한 컴포넌트들 찾기
    /// </summary>
    void FindComponents()
    {
        vrPlayerHealth = FindFirstObjectByType<VRPlayerHealth>();
        postProcessingManager = FindFirstObjectByType<VRPostProcessingManager>();
        cameraRig = FindFirstObjectByType<OVRCameraRig>();
        
        Debug.Log($"[VRAttackDebugger] 컴포넌트 찾기 결과:");
        Debug.Log($"  - VRPlayerHealth: {(vrPlayerHealth != null ? "✅" : "❌")}");
        Debug.Log($"  - VRPostProcessingManager: {(postProcessingManager != null ? "✅" : "❌")}");
        Debug.Log($"  - OVRCameraRig: {(cameraRig != null ? "✅" : "❌")}");
    }
    
    /// <summary>
    /// Unity 6 New Input System 설정
    /// </summary>
    void SetupInputSystem()
    {
        // T키로 테스트 데미지 트리거
        testDamageAction = new InputAction("TestDamage", InputActionType.Button);
        testDamageAction.AddBinding("<Keyboard>/t");
        
        testDamageAction.performed += OnTestDamagePerformed;
        testDamageAction.Enable();
        
        Debug.Log("[VRAttackDebugger] ✅ New Input System 설정 완료! [T] 키로 테스트 가능");
    }
    
    /// <summary>
    /// New Input System 콜백
    /// </summary>
    void OnTestDamagePerformed(InputAction.CallbackContext context)
    {
        TestDamageEffect();
    }
    
    /// <summary>
    /// 디버그 UI 생성
    /// </summary>
    void CreateDebugUI()
    {
        if (debugText == null && enableDebugUI)
        {
            // Canvas 찾기 또는 생성
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Debug Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = Camera.main;
                
                // VR에서 보기 좋은 위치 설정
                canvasGO.transform.position = new Vector3(0, 2, 2);
                canvasGO.transform.localScale = Vector3.one * 0.01f;
            }
            
            // 디버그 텍스트 생성
            GameObject textGO = new GameObject("VR Attack Debug Text");
            textGO.transform.SetParent(canvas.transform);
            
            debugText = textGO.AddComponent<TextMeshProUGUI>();
            debugText.text = "VR Attack Debug Loading...";
            debugText.fontSize = 24;
            debugText.color = Color.white;
            
            // RectTransform 설정
            RectTransform rectTransform = debugText.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(800, 600);
        }
    }
    
    /// <summary>
    /// 실시간 디버그 정보 업데이트
    /// </summary>
    void UpdateDebugInfo()
    {
        debugInfo = "=== VR Attack System Debug ===\n\n";
        
        // 1. 컴포넌트 상태
        debugInfo += "[Components Status]\n";
        debugInfo += $"  VRPlayerHealth: {(vrPlayerHealth != null ? "OK" : "MISSING")}\n";
        debugInfo += $"  VRPostProcessingManager: {(postProcessingManager != null ? "OK" : "MISSING")}\n";
        debugInfo += $"  OVRCameraRig: {(cameraRig != null ? "OK" : "MISSING")}\n\n";
        
        // 2. 플레이어 체력 정보
        if (vrPlayerHealth != null)
        {
            debugInfo += "[Player Health]\n";
            debugInfo += $"  Current Health: {vrPlayerHealth.currentHealth:F1}/{vrPlayerHealth.maxHealth}\n";
            debugInfo += $"  Invincible: {(Time.time - lastDamageTime < vrPlayerHealth.invincibilityDuration ? "YES" : "NO")}\n\n";
        }
        
        // 3. Post Processing 상태
        if (postProcessingManager != null)
        {
            debugInfo += "[Post Processing]\n";
            debugInfo += $"  Current State: {postProcessingManager.CurrentState}\n";
            debugInfo += $"  Intensity: {postProcessingManager.CurrentIntensity:F2}\n";
            debugInfo += $"  Effect Active: {(postProcessingManager.IsEffectActive ? "YES" : "NO")}\n";
            debugInfo += $"  Component Enabled: {(postProcessingManager.enabled ? "YES" : "NO")}\n\n";
        }
        
        // 4. 피격 통계
        debugInfo += "[Damage Statistics]\n";
        debugInfo += $"  Total Damage Count: {damageCount}\n";
        debugInfo += $"  Last Damage: {(lastDamageTime > 0 ? $"{Time.time - lastDamageTime:F1}s ago" : "None")}\n\n";
        
        // 5. Scene 설정 확인
        debugInfo += "[Scene Setup Check]\n";
        var enemies = FindObjectsByType<EnemyAttackSystem>(FindObjectsSortMode.None);
        debugInfo += $"  Enemies with AttackSystem: {enemies.Length}\n";
        var globalVolumes = FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None);
        debugInfo += $"  Global Volumes in Scene: {globalVolumes.Length}\n";
        debugInfo += $"  Player GameObject: {(vrPlayerHealth != null ? vrPlayerHealth.gameObject.name : "NONE")}\n\n";
        
        // 6. 테스트 가이드
        debugInfo += "[Test Methods]\n";
        debugInfo += "  [T] Key = Force Damage Test (New Input System)\n";
        debugInfo += "  [G] Key = Enemy Force Attack (Legacy Input)\n";
        debugInfo += "  Inspector 'Test VR Damage Effect' Button\n";
        debugInfo += "  Enemy Approach + Attack1 Animation\n";
    }
    
    /// <summary>
    /// 강제 데미지 테스트 (강화된 디버그)
    /// </summary>
    public void TestDamageEffect()
    {
        Debug.Log("=== VR 피격 효과 테스트 시작 ===");
        
        if (vrPlayerHealth != null)
        {
            Debug.Log($"[VRAttackDebugger] ✅ VRPlayerHealth 발견: {vrPlayerHealth.name}");
            Debug.Log($"[VRAttackDebugger] 🔴 강제 피격 테스트 실행! (데미지: {testDamageAmount})");
            
            // 현재 체력 상태 로그
            Debug.Log($"[VRAttackDebugger] 피격 전 체력: {vrPlayerHealth.currentHealth}/{vrPlayerHealth.maxHealth}");
            
            // 데미지 적용
            vrPlayerHealth.TakeDamage(testDamageAmount);
            lastDamageTime = Time.time;
            damageCount++;
            
            // 피격 후 체력 상태 로그
            Debug.Log($"[VRAttackDebugger] 피격 후 체력: {vrPlayerHealth.currentHealth}/{vrPlayerHealth.maxHealth}");
            
            // Post Processing Manager 상태 확인
            if (postProcessingManager != null)
            {
                Debug.Log($"[VRAttackDebugger] ✅ VRPostProcessingManager 활성: {postProcessingManager.enabled}");
                Debug.Log($"[VRAttackDebugger] 현재 효과 상태: {postProcessingManager.CurrentState}");
            }
            else
            {
                Debug.LogWarning("[VRAttackDebugger] ❌ VRPostProcessingManager를 찾을 수 없음!");
            }
        }
        else
        {
            Debug.LogError("[VRAttackDebugger] ❌ VRPlayerHealth를 찾을 수 없어서 테스트 불가!");
            
            // 대안으로 직접 Post Processing 효과 테스트
            if (postProcessingManager != null)
            {
                Debug.Log("[VRAttackDebugger] 🔄 대안: 직접 VR 피격 효과 호출");
                postProcessingManager.TriggerVRDamageEffect(0.8f, 1.5f);
            }
        }
        
        Debug.Log("=== VR 피격 효과 테스트 완료 ===");
    }
    
    /// <summary>
    /// Enemy 공격 감지 (EnemyAttackSystem에서 호출)
    /// </summary>
    public static void OnEnemyAttackDetected(string enemyName, float damage)
    {
        VRAttackDebugger debugger = FindFirstObjectByType<VRAttackDebugger>();
        if (debugger != null)
        {
            debugger.lastDamageTime = Time.time;
            debugger.damageCount++;
            Debug.Log($"[VRAttackDebugger] {enemyName}의 공격 감지! 데미지: {damage}");
        }
    }
}

#if UNITY_EDITOR
/// <summary>
/// Unity Inspector에서 사용할 수 있는 커스텀 에디터
/// </summary>
[UnityEditor.CustomEditor(typeof(VRAttackDebugger))]
public class VRAttackDebuggerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        UnityEditor.EditorGUILayout.Space(10);
        
        VRAttackDebugger debugger = (VRAttackDebugger)target;
        
        if (GUILayout.Button("Test VR Damage Effect", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                debugger.TestDamageEffect();
            }
            else
            {
                UnityEditor.EditorUtility.DisplayDialog("테스트 불가", 
                    "Play 모드에서만 테스트할 수 있습니다!", "확인");
            }
        }
        
        UnityEditor.EditorGUILayout.Space(5);
        
        if (Application.isPlaying)
        {
            UnityEditor.EditorGUILayout.HelpBox("Play Mode: Use button above to test!", UnityEditor.MessageType.Info);
        }
        else
        {
            UnityEditor.EditorGUILayout.HelpBox("Testing only available in Play Mode.", UnityEditor.MessageType.Warning);
        }
    }
}
#endif 