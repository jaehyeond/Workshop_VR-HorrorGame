using UnityEngine;

/// <summary>
/// VR Horror Game - 영상 트리거 시스템
/// 플레이어가 특정 지점에 도달하면 영상을 재생하는 트리거
/// </summary>
public class CinematicTrigger : MonoBehaviour
{
    [Header("=== Trigger Settings ===")]
    public CinematicManager.CinematicType cinematicType = CinematicManager.CinematicType.BossIntro;
    [SerializeField] private bool isOneTimeUse = true;
    [SerializeField] private bool requiresBossDefeated = false; // 딸 구출용
    [SerializeField] private float triggerDelay = 0.5f;

    [Header("=== Visual Feedback ===")]
    [SerializeField] private bool showTriggerArea = true;
    [SerializeField] private Color triggerColor = Color.yellow;
    [SerializeField] private GameObject visualIndicator;

    [Header("=== Player Detection ===")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask playerLayerMask = -1;

    // 상태
    private bool hasTriggered = false;
    private bool isPlayerInRange = false;
    private Collider triggerCollider;

    // Events
    public System.Action<CinematicManager.CinematicType> OnTriggerActivated;

    #region Unity Lifecycle

    void Awake()
    {
        InitializeTrigger();
    }

    void Start()
    {
        SetupVisualIndicator();
        CheckInitialGameState();
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsValidPlayer(other))
        {
            OnPlayerEnter(other);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (IsValidPlayer(other))
        {
            OnPlayerExit(other);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (showTriggerArea)
        {
            DrawTriggerGizmos();
        }
    }

    #endregion

    #region Initialization

    void InitializeTrigger()
    {
        // Collider 설정 확인
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            // BoxCollider 자동 추가
            triggerCollider = gameObject.AddComponent<BoxCollider>();
            BoxCollider boxCollider = triggerCollider as BoxCollider;
            boxCollider.size = new Vector3(3f, 3f, 1f);
        }

        // Trigger 설정 확인
        if (!triggerCollider.isTrigger)
        {
            triggerCollider.isTrigger = true;
        }

        Debug.Log($"[CinematicTrigger] {cinematicType} 트리거 초기화 완료");
    }

    void CheckInitialGameState()
    {
        // 게임 진행 상황에 따라 트리거 활성화 여부 결정
        if (GameProgressManager.Instance == null) return;

        bool shouldActivate = ShouldActivateTrigger();
        gameObject.SetActive(shouldActivate);

        if (shouldActivate)
        {
            Debug.Log($"[CinematicTrigger] {cinematicType} 트리거 활성화됨");
        }
        else
        {
            Debug.Log($"[CinematicTrigger] {cinematicType} 트리거 비활성화됨 (조건 미충족)");
        }
    }

    bool ShouldActivateTrigger()
    {
        var progressManager = GameProgressManager.Instance;
        
        switch (cinematicType)
        {
            case CinematicManager.CinematicType.BossIntro:
                // 보스 인트로는 메인 탐험 상태일 때만 활성화
                return progressManager.CurrentState == GameProgressManager.GameState.MainExploration &&
                       !progressManager.HasSeenBossIntro;
                       
            case CinematicManager.CinematicType.Ending:
                // 엔딩은 보스가 처치되었을 때만 활성화
                return progressManager.IsBossDefeated && 
                       !progressManager.HasSeenEnding;
                       
            default:
                return true;
        }
    }

    #endregion

    #region Player Detection

    bool IsValidPlayer(Collider other)
    {
        // 태그 확인
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
        {
            // VR 플레이어는 보통 OVRCameraRig나 하위 컴포넌트에 있음
            Transform parent = other.transform.parent;
            while (parent != null)
            {
                if (parent.CompareTag(playerTag))
                {
                    return true;
                }
                parent = parent.parent;
            }
            
            // 일반적인 VR 컴포넌트 확인
            if (other.GetComponent<OVRCameraRig>() != null ||
                other.GetComponentInParent<OVRCameraRig>() != null)
            {
                return true;
            }
            
            return false;
        }

        // 레이어 확인
        int layerMask = 1 << other.gameObject.layer;
        if ((playerLayerMask.value & layerMask) == 0)
        {
            return false;
        }

        return true;
    }

    void OnPlayerEnter(Collider player)
    {
        Debug.Log($"[CinematicTrigger] 플레이어가 {cinematicType} 트리거 영역에 진입");
        
        isPlayerInRange = true;

        // 조건 확인 후 트리거 실행
        if (CanTrigger())
        {
            ExecuteTrigger();
        }
    }

    void OnPlayerExit(Collider player)
    {
        Debug.Log($"[CinematicTrigger] 플레이어가 {cinematicType} 트리거 영역에서 벗어남");
        
        isPlayerInRange = false;
    }

    #endregion

    #region Trigger Execution

    bool CanTrigger()
    {
        // 이미 트리거되었는지 확인
        if (hasTriggered && isOneTimeUse)
        {
            Debug.LogWarning($"[CinematicTrigger] {cinematicType} 트리거가 이미 실행되었습니다");
            return false;
        }

        // 현재 영상 재생 중인지 확인
        if (CinematicManager.Instance != null && CinematicManager.Instance.IsPlayingVideo)
        {
            Debug.LogWarning("[CinematicTrigger] 현재 영상이 재생 중입니다");
            return false;
        }

        // 특별 조건 확인
        if (requiresBossDefeated)
        {
            if (GameProgressManager.Instance == null || !GameProgressManager.Instance.IsBossDefeated)
            {
                Debug.LogWarning($"[CinematicTrigger] {cinematicType} 트리거를 위해서는 보스가 처치되어야 합니다");
                return false;
            }
        }

        return true;
    }

    void ExecuteTrigger()
    {
        Debug.Log($"[CinematicTrigger] {cinematicType} 트리거 실행!");

        hasTriggered = true;

        // 트리거 지연 처리
        if (triggerDelay > 0f)
        {
            Invoke(nameof(TriggerCinematic), triggerDelay);
        }
        else
        {
            TriggerCinematic();
        }

        // 이벤트 호출
        OnTriggerActivated?.Invoke(cinematicType);

        // 일회용 트리거라면 비활성화
        if (isOneTimeUse)
        {
            DisableTrigger();
        }
    }

    void TriggerCinematic()
    {
        // CinematicManager에 영상 재생 요청
        if (CinematicManager.Instance != null)
        {
            CinematicManager.Instance.PlayCinematic(cinematicType);
        }
        else
        {
            Debug.LogError("[CinematicTrigger] CinematicManager를 찾을 수 없습니다!");
        }

        // GameProgressManager에 상태 변경 요청
        if (GameProgressManager.Instance != null)
        {
            UpdateGameState();
        }
    }

    void UpdateGameState()
    {
        switch (cinematicType)
        {
            case CinematicManager.CinematicType.BossIntro:
                GameProgressManager.Instance.SetGameState(GameProgressManager.GameState.BossIntroVideo);
                break;
                
            case CinematicManager.CinematicType.Ending:
                GameProgressManager.Instance.NotifyDaughterRescued();
                break;
        }
    }

    void DisableTrigger()
    {
        // 트리거 비활성화
        triggerCollider.enabled = false;
        
        // 시각적 표시 제거
        if (visualIndicator != null)
        {
            visualIndicator.SetActive(false);
        }

        Debug.Log($"[CinematicTrigger] {cinematicType} 트리거 비활성화됨");
    }

    #endregion

    #region Visual Feedback

    void SetupVisualIndicator()
    {
        if (!showTriggerArea) return;

        // 시각적 표시기 생성 (개발용)
        if (visualIndicator == null)
        {
            CreateVisualIndicator();
        }

        // 색상 설정
        UpdateIndicatorColor();
    }

    void CreateVisualIndicator()
    {
        // 개발용 시각적 표시기 생성
        visualIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualIndicator.name = $"{cinematicType}_TriggerIndicator";
        visualIndicator.transform.SetParent(transform);
        visualIndicator.transform.localPosition = Vector3.zero;
        
        // 반투명 재질 적용
        Renderer renderer = visualIndicator.GetComponent<Renderer>();
        Material indicatorMaterial = new Material(Shader.Find("Standard"));
        indicatorMaterial.SetFloat("_Mode", 3); // Transparent mode
        indicatorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        indicatorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        indicatorMaterial.SetInt("_ZWrite", 0);
        indicatorMaterial.DisableKeyword("_ALPHATEST_ON");
        indicatorMaterial.EnableKeyword("_ALPHABLEND_ON");
        indicatorMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        indicatorMaterial.renderQueue = 3000;
        
        Color color = triggerColor;
        color.a = 0.3f;
        indicatorMaterial.color = color;
        
        renderer.material = indicatorMaterial;

        // 물리 충돌 제거
        Collider indicatorCollider = visualIndicator.GetComponent<Collider>();
        if (indicatorCollider != null)
        {
            Destroy(indicatorCollider);
        }

        // 빌드 시에는 제거되도록 설정
        #if !UNITY_EDITOR
        visualIndicator.SetActive(false);
        #endif
    }

    void UpdateIndicatorColor()
    {
        if (visualIndicator == null) return;

        Renderer renderer = visualIndicator.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            Color color = triggerColor;
            color.a = hasTriggered ? 0.1f : 0.3f;
            renderer.material.color = color;
        }
    }

    void DrawTriggerGizmos()
    {
        Gizmos.color = hasTriggered ? Color.gray : triggerColor;
        
        if (triggerCollider is BoxCollider boxCollider)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            Gizmos.matrix = oldMatrix;
        }
        else if (triggerCollider is SphereCollider sphereCollider)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
            Gizmos.matrix = oldMatrix;
        }
    }

    #endregion

    #region Public Methods

    public void ResetTrigger()
    {
        hasTriggered = false;
        triggerCollider.enabled = true;
        
        if (visualIndicator != null)
        {
            visualIndicator.SetActive(true);
        }
        
        UpdateIndicatorColor();
        
        Debug.Log($"[CinematicTrigger] {cinematicType} 트리거 리셋됨");
    }

    public void ForceActivate()
    {
        if (CanTrigger())
        {
            ExecuteTrigger();
        }
    }

    #endregion

    #region Public Properties

    public bool HasTriggered => hasTriggered;
    public bool IsPlayerInRange => isPlayerInRange;
    public CinematicManager.CinematicType CinematicType => cinematicType;

    #endregion
} 