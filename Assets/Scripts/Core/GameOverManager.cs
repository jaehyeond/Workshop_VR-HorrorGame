using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// VR Game Over 시스템 관리
/// Game Over UI 표시, 리스폰 처리, 자동 회복 시스템 제어
/// </summary>
public class GameOverManager : MonoBehaviour
{
    [Header("Game Over UI")]
    [Tooltip("Game Over UI Canvas (World Space)")]
    public Canvas gameOverCanvas;
    
    [Tooltip("Game Over 제목 텍스트")]
    public TextMeshProUGUI gameOverTitle;
    
    [Tooltip("계속하기 버튼 (빠른 부활)")]
    public Button continueButton;
    
    [Tooltip("포기하기 버튼 (처음부터 다시)")]
    public Button giveUpButton;
    
    [Header("타이밍 설정")]
    [Tooltip("Game Over UI 표시까지의 딜레이 (초)")]
    public float gameOverDelay = 2f;
    
    [Tooltip("자동 리스폰까지의 시간 (0이면 비활성화)")]
    public float autoRespawnTime = 10f;
    
    [Tooltip("Game Over UI 페이드 시간")]
    public float fadeTime = 1f;
    
    [Header("VR 설정")]
    [Tooltip("UI가 플레이어 시선을 따라갈지 여부")]
    public bool followPlayerGaze = true;
    
    [Tooltip("플레이어로부터의 UI 거리")]
    public float uiDistance = 3f;
    
    [Tooltip("UI 높이 오프셋")]
    public float uiHeightOffset = 0.5f;
    
    // 참조
    private VRPlayerHealth playerHealth;
    private PlayerSpawnManager spawnManager;
    private Camera playerCamera;
    private CanvasGroup canvasGroup;
    private VRUIRayController vrRayController;
    
    // 상태
    private bool isGameOver = false;
    private Coroutine autoRespawnCoroutine;
    
    // 싱글톤 패턴
    public static GameOverManager Instance { get; private set; }
    
    // 이벤트
    public System.Action OnGameOverStarted;
    public System.Action OnGameOverEnded;
    
    void Awake()
    {
        // 싱글톤 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Start()
    {
        InitializeGameOverManager();
    }
    
    void Update()
    {
        // VR에서 UI가 플레이어를 따라가도록 설정
        if (isGameOver && followPlayerGaze && gameOverCanvas != null && playerCamera != null)
        {
            UpdateUIPosition();
        }
    }
    
    /// <summary>
    /// Game Over 매니저 초기화
    /// </summary>
    void InitializeGameOverManager()
    {
        // 컴포넌트 찾기
        FindReferences();
        
        // UI 초기화
        InitializeUI();
        
        // 이벤트 연결
        SetupEvents();
    }
    
    /// <summary>
    /// 필요한 컴포넌트들 찾기
    /// </summary>
    void FindReferences()
    {
        // VRPlayerHealth 찾기
        playerHealth = FindFirstObjectByType<VRPlayerHealth>();
        
        // PlayerSpawnManager 찾기
        spawnManager = PlayerSpawnManager.Instance;
        
        // VR 카메라 찾기
        FindVRCamera();
        
        // Canvas CanvasGroup 컴포넌트 확인
        if (gameOverCanvas != null)
        {
            canvasGroup = gameOverCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameOverCanvas.gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // VR UI Ray Controller 찾기
        FindVRRayController();
    }
    
    /// <summary>
    /// VR 카메라 찾기
    /// </summary>
    void FindVRCamera()
    {
        // OVR Center Eye Camera 찾기
        Camera centerEyeCamera = Camera.main;
        if (centerEyeCamera != null && centerEyeCamera.name.Contains("Center"))
        {
            playerCamera = centerEyeCamera;
            Debug.Log($"[GameOverManager] VR 카메라 찾음: {playerCamera.name}");
            return;
        }
        
        // CenterEyeAnchor로 찾기
        Transform centerEye = GameObject.Find("CenterEyeAnchor")?.transform;
        if (centerEye != null)
        {
            playerCamera = centerEye.GetComponent<Camera>();
            if (playerCamera != null)
            {
                Debug.Log($"[GameOverManager] CenterEyeAnchor 카메라 찾음: {playerCamera.name}");
                return;
            }
        }
        
        // 일반 Main Camera 사용
        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            Debug.Log($"[GameOverManager] Main Camera 사용: {playerCamera.name}");
        }
        else
        {
            Debug.LogWarning("[GameOverManager] 플레이어 카메라를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// VR UI Ray Controller 찾기 및 설정
    /// </summary>
    void FindVRRayController()
    {
        // 기존 VRUIRayController 찾기
        vrRayController = FindFirstObjectByType<VRUIRayController>();
        
        if (vrRayController == null)
        {
            // 자동으로 생성하기 (왼손 컨트롤러에 추가)
            GameObject leftHandAnchor = GameObject.Find("LeftHandAnchor");
            if (leftHandAnchor != null)
            {
                vrRayController = leftHandAnchor.AddComponent<VRUIRayController>();
                Debug.Log("[GameOverManager] VRUIRayController를 LeftHandAnchor에 자동 추가했습니다!");
            }
            else
            {
                // 빈 GameObject에 생성
                GameObject rayControllerObject = new GameObject("VR UI Ray Controller");
                vrRayController = rayControllerObject.AddComponent<VRUIRayController>();
                Debug.LogWarning("[GameOverManager] LeftHandAnchor를 찾을 수 없어 새 GameObject에 VRUIRayController를 생성했습니다!");
            }
        }
        
        // 초기에는 비활성화 (Game Over 상황에서만 활성화)
        if (vrRayController != null)
        {
            vrRayController.SetRayEnabled(false);
            Debug.Log("[GameOverManager] VRUIRayController 초기화 완료 (비활성화 상태)");
        }
    }
    
    /// <summary>
    /// UI 초기화
    /// </summary>
    void InitializeUI()
    {
        if (gameOverCanvas == null)
        {
            Debug.LogError("[GameOverManager] Game Over Canvas가 설정되지 않았습니다!");
            return;
        }
        
        // 초기에는 UI 숨김
        gameOverCanvas.gameObject.SetActive(false);
        
        // 버튼 이벤트 연결
        SetupButtonEvents();
        
        Debug.Log("[GameOverManager] UI 초기화 완료");
    }
    
    /// <summary>
    /// 버튼 이벤트 설정
    /// </summary>
    void SetupButtonEvents()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }
        
        if (giveUpButton != null)
        {
            giveUpButton.onClick.AddListener(OnGiveUpButtonClicked);
        }
    }
    
    /// <summary>
    /// 이벤트 연결
    /// </summary>
    void SetupEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath += OnPlayerDied;
        }
        
        if (spawnManager != null)
        {
            spawnManager.OnPlayerRespawned += OnPlayerRespawned;
        }
    }
    
    /// <summary>
    /// 플레이어 사망 시 호출
    /// </summary>
    void OnPlayerDied()
    {
        Debug.Log("[GameOverManager] 플레이어 사망! Game Over 시작");
        StartCoroutine(ShowGameOverAfterDelay());
    }
    
    /// <summary>
    /// 딜레이 후 Game Over UI 표시
    /// </summary>
    IEnumerator ShowGameOverAfterDelay()
    {
        yield return new WaitForSeconds(gameOverDelay);
        ShowGameOver();
    }
    
    /// <summary>
    /// Game Over UI 표시
    /// </summary>
    public void ShowGameOver()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        
        Debug.Log("[GameOverManager] Game Over UI 표시");
        
        // VR Ray Controller 활성화
        if (vrRayController != null)
        {
            vrRayController.SetRayEnabled(true);
            Debug.Log("[GameOverManager] VR UI Ray Controller 활성화됨");
        }
        
        // UI 활성화
        if (gameOverCanvas != null)
        {
            gameOverCanvas.gameObject.SetActive(true);
            
            // UI 위치 설정
            if (followPlayerGaze)
            {
                UpdateUIPosition();
            }
            
            // 페이드 인 효과
            StartCoroutine(FadeInUI());
        }
        
        // 자동 회복 시스템 비활성화
        DisableAutoRecovery();
        
                 // 자동 계속하기 시작 (설정된 경우)
         if (autoRespawnTime > 0)
         {
             autoRespawnCoroutine = StartCoroutine(AutoContinueCountdown());
         }
        
        // 이벤트 발생
        OnGameOverStarted?.Invoke();
    }
    
    /// <summary>
    /// Game Over UI 숨김
    /// </summary>
    public void HideGameOver()
    {
        if (!isGameOver) return;
        
        isGameOver = false;
        
        Debug.Log("[GameOverManager] Game Over UI 숨김");
        
        // VR Ray Controller 비활성화
        if (vrRayController != null)
        {
            vrRayController.SetRayEnabled(false);
            Debug.Log("[GameOverManager] VR UI Ray Controller 비활성화됨");
        }
        
        // 자동 리스폰 중단
        if (autoRespawnCoroutine != null)
        {
            StopCoroutine(autoRespawnCoroutine);
            autoRespawnCoroutine = null;
        }
        
        // UI 비활성화
        if (gameOverCanvas != null)
        {
            StartCoroutine(FadeOutUI());
        }
        
        // 이벤트 발생
        OnGameOverEnded?.Invoke();
    }
    
    /// <summary>
    /// UI 페이드 인
    /// </summary>
    IEnumerator FadeInUI()
    {
        if (canvasGroup == null) yield break;
        
        canvasGroup.alpha = 0f;
        float elapsed = 0f;
        
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeTime);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    /// <summary>
    /// UI 페이드 아웃
    /// </summary>
    IEnumerator FadeOutUI()
    {
        if (canvasGroup == null) yield break;
        
        canvasGroup.alpha = 1f;
        float elapsed = 0f;
        
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        gameOverCanvas.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// VR에서 UI 위치 업데이트
    /// </summary>
    void UpdateUIPosition()
    {
        if (playerCamera == null || gameOverCanvas == null) return;
        
        // 플레이어 앞 방향으로 UI 배치
        Vector3 forward = playerCamera.transform.forward;
        forward.y = 0; // Y축 고정하여 수평 유지
        forward = forward.normalized; // normalized 프로퍼티 사용
        
        Vector3 targetPosition = playerCamera.transform.position + forward * uiDistance;
        targetPosition.y += uiHeightOffset;
        
        gameOverCanvas.transform.position = targetPosition;
        gameOverCanvas.transform.LookAt(playerCamera.transform.position);
        gameOverCanvas.transform.Rotate(0, 180, 0); // UI가 플레이어를 향하도록
    }
    
    /// <summary>
    /// 자동 회복 시스템 비활성화
    /// </summary>
    void DisableAutoRecovery()
    {
        // VRPlayerHealth의 자동 회복을 일시적으로 비활성화
        // (이것은 VRPlayerHealth에서 GameOver 상태를 체크하도록 수정 필요)
    }
    
    /// <summary>
    /// 자동 계속하기 카운트다운
    /// </summary>
    IEnumerator AutoContinueCountdown()
    {
        yield return new WaitForSeconds(autoRespawnTime);
        
        Debug.Log("[GameOverManager] 자동 계속하기 실행");
        ContinueGame();
    }
    
    /// <summary>
    /// 계속하기 버튼 클릭 (빠른 부활)
    /// </summary>
    void OnContinueButtonClicked()
    {
        Debug.Log("[GameOverManager] 계속하기 버튼 클릭됨 - 빠른 부활");
        ContinueGame();
    }
    
    /// <summary>
    /// 포기하기 버튼 클릭 (완전 재시작)
    /// </summary>
    void OnGiveUpButtonClicked()
    {
        Debug.Log("[GameOverManager] 포기하기 버튼 클릭됨 - 완전 재시작");
        GiveUpGame();
    }
    
    /// <summary>
    /// 게임 계속하기 (빠른 부활 - 현재 위치에서)
    /// </summary>
    public void ContinueGame()
    {
        // 현재 위치에서 체력만 회복하고 계속
        if (playerHealth != null)
        {
            playerHealth.RespawnWithFullHealth(); // 리스폰 전용 완전 회복
        }
        
        // Game Over UI 숨김
        HideGameOver();
        
        Debug.Log("[GameOverManager] 게임 계속 - 현재 위치에서 부활!");
    }
    
    /// <summary>
    /// 게임 포기 (처음부터 다시 시작)
    /// </summary>
    public void GiveUpGame()
    {
        if (spawnManager == null)
        {
            Debug.LogError("[GameOverManager] PlayerSpawnManager가 없습니다! 씬 재로드로 대체");
            RestartScene();
            return;
        }
        
        // 플레이어를 시작 위치로 이동
        spawnManager.RespawnPlayer();
        
        // 체력 완전 회복
        if (playerHealth != null)
        {
            playerHealth.RespawnWithFullHealth(); // 리스폰 전용 완전 회복
        }
        
        // 적들 리셋 (옵션 - 필요시 추가)
        ResetEnemies();
        
        // Game Over UI 숨김
        HideGameOver();
        
        Debug.Log("[GameOverManager] 게임 포기 - 처음부터 다시 시작!");
    }
    
    /// <summary>
    /// 씬 재시작 (백업 방법)
    /// </summary>
    public void RestartScene()
    {
        Debug.Log("[GameOverManager] 씬 재시작");
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
    
    /// <summary>
    /// 적들 리셋 (옵션)
    /// </summary>
    void ResetEnemies()
    {
        // CultistManager나 EnemyManager를 통해 적들을 초기 상태로 리셋
        // 필요에 따라 구현
        Debug.Log("[GameOverManager] 적들 상태 리셋");
    }
    
    /// <summary>
    /// 플레이어 리스폰 완료 시 호출
    /// </summary>
    void OnPlayerRespawned()
    {
        Debug.Log("[GameOverManager] 플레이어 리스폰 이벤트 받음");
    }
    
    /// <summary>
    /// Game Over 상태 확인
    /// </summary>
    public bool IsGameOver()
    {
        return isGameOver;
    }
    
    void OnDestroy()
    {
        // 이벤트 해제
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath -= OnPlayerDied;
        }
        
        if (spawnManager != null)
        {
            spawnManager.OnPlayerRespawned -= OnPlayerRespawned;
        }
    }
} 