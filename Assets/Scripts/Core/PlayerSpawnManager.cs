using UnityEngine;

/// <summary>
/// 플레이어 스폰 포인트 관리 및 리스폰 시스템
/// VR 플레이어의 시작 위치를 기록하고 사망 시 원래 위치로 복귀시킵니다
/// </summary>
public class PlayerSpawnManager : MonoBehaviour
{
    [Header("스폰 설정")]
    [Tooltip("플레이어 컨트롤러 Transform")]
    public Transform playerController;
    
    [Tooltip("시작 위치 자동 기록 여부")]
    public bool autoRecordStartPosition = true;
    
    [Tooltip("리스폰 시 페이드 효과 사용")]
    public bool useFadeEffect = true;
    
    [Header("스폰 포인트")]
    [Tooltip("수동으로 설정된 스폰 포인트 (옵션)")]
    public Transform customSpawnPoint;
    
    // 기록된 스폰 데이터
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private bool spawnDataRecorded = false;
    
    // 싱글톤 패턴
    public static PlayerSpawnManager Instance { get; private set; }
    
    // 이벤트
    public System.Action OnPlayerRespawned;
    
    void Awake()
    {
        // 싱글톤 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        InitializeSpawnManager();
    }
    
    /// <summary>
    /// 스폰 매니저 초기화
    /// </summary>
    void InitializeSpawnManager()
    {
        // 플레이어 컨트롤러 자동 찾기
        if (playerController == null)
        {
            FindPlayerController();
        }
        
        if (playerController == null)
        {
            Debug.LogError("[PlayerSpawnManager] 플레이어 컨트롤러를 찾을 수 없습니다!");
            return;
        }
        
        // 스폰 포인트 설정
        if (autoRecordStartPosition)
        {
            RecordCurrentPositionAsSpawn();
        }
        else if (customSpawnPoint != null)
        {
            SetCustomSpawnPoint(customSpawnPoint);
        }
        
        Debug.Log($"[PlayerSpawnManager] 초기화 완료! 스폰 위치: {spawnPosition}");
    }
    
    /// <summary>
    /// 플레이어 컨트롤러 자동 찾기
    /// </summary>
    void FindPlayerController()
    {
        // OVRPlayerController 찾기
        OVRPlayerController ovrPlayer = FindAnyObjectByType<OVRPlayerController>();
        if (ovrPlayer != null)
        {
            playerController = ovrPlayer.transform;
            Debug.Log($"[PlayerSpawnManager] OVRPlayerController 찾음: {playerController.name}");
            return;
        }
        
        // Player 태그로 찾기
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerController = playerObj.transform;
            Debug.Log($"[PlayerSpawnManager] Player 태그로 찾음: {playerController.name}");
            return;
        }
        
        // OVRCameraRig 찾기
        OVRCameraRig cameraRig = FindAnyObjectByType<OVRCameraRig>();
        if (cameraRig != null)
        {
            playerController = cameraRig.transform;
            Debug.Log($"[PlayerSpawnManager] OVRCameraRig 찾음: {playerController.name}");
            return;
        }
        
        Debug.LogWarning("[PlayerSpawnManager] 플레이어 컨트롤러를 찾을 수 없습니다!");
    }
    
    /// <summary>
    /// 현재 위치를 스폰 포인트로 기록
    /// </summary>
    public void RecordCurrentPositionAsSpawn()
    {
        if (playerController == null)
        {
            Debug.LogError("[PlayerSpawnManager] 플레이어 컨트롤러가 없어서 위치를 기록할 수 없습니다!");
            return;
        }
        
        spawnPosition = playerController.position;
        spawnRotation = playerController.rotation;
        spawnDataRecorded = true;
        
        Debug.Log($"[PlayerSpawnManager] 스폰 위치 기록됨: {spawnPosition} (회전: {spawnRotation.eulerAngles})");
    }
    
    /// <summary>
    /// 커스텀 스폰 포인트 설정
    /// </summary>
    public void SetCustomSpawnPoint(Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("[PlayerSpawnManager] 스폰 포인트가 null입니다!");
            return;
        }
        
        customSpawnPoint = spawnPoint;
        spawnPosition = spawnPoint.position;
        spawnRotation = spawnPoint.rotation;
        spawnDataRecorded = true;
        
        Debug.Log($"[PlayerSpawnManager] 커스텀 스폰 포인트 설정됨: {spawnPosition}");
    }
    
    /// <summary>
    /// 플레이어를 스폰 위치로 리스폰
    /// </summary>
    public void RespawnPlayer()
    {
        if (!spawnDataRecorded)
        {
            Debug.LogError("[PlayerSpawnManager] 스폰 데이터가 기록되지 않았습니다!");
            return;
        }
        
        if (playerController == null)
        {
            Debug.LogError("[PlayerSpawnManager] 플레이어 컨트롤러가 없습니다!");
            return;
        }
        
        Debug.Log($"[PlayerSpawnManager] 플레이어 리스폰 시작! 목표 위치: {spawnPosition}");
        
        // 위치 리셋
        ResetPlayerPosition();
        
        // 이벤트 발생
        OnPlayerRespawned?.Invoke();
        
        Debug.Log("[PlayerSpawnManager] 플레이어 리스폰 완료!");
    }
    
    /// <summary>
    /// 플레이어 위치 즉시 리셋
    /// </summary>
    void ResetPlayerPosition()
    {
        // VR에서는 CharacterController가 있을 수 있으므로 체크
        CharacterController characterController = playerController.GetComponent<CharacterController>();
        
        if (characterController != null)
        {
            // CharacterController 비활성화 → 위치 변경 → 재활성화
            characterController.enabled = false;
            playerController.position = spawnPosition;
            playerController.rotation = spawnRotation;
            characterController.enabled = true;
            
            Debug.Log("[PlayerSpawnManager] CharacterController를 통한 위치 리셋 완료");
        }
        else
        {
            // 일반 Transform 위치 변경
            playerController.position = spawnPosition;
            playerController.rotation = spawnRotation;
            
            Debug.Log("[PlayerSpawnManager] Transform을 통한 위치 리셋 완료");
        }
    }
    
    /// <summary>
    /// 스폰 위치 가져오기
    /// </summary>
    public Vector3 GetSpawnPosition()
    {
        return spawnPosition;
    }
    
    /// <summary>
    /// 스폰 회전 가져오기
    /// </summary>
    public Quaternion GetSpawnRotation()
    {
        return spawnRotation;
    }
    
    /// <summary>
    /// 스폰 데이터 기록 여부 확인
    /// </summary>
    public bool IsSpawnDataRecorded()
    {
        return spawnDataRecorded;
    }
    
    /// <summary>
    /// 디버그용 기즈모
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (spawnDataRecorded)
        {
            // 스폰 위치 표시
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPosition, 0.5f);
            Gizmos.DrawLine(spawnPosition, spawnPosition + Vector3.up * 2f);
            
            // 스폰 방향 표시
            Gizmos.color = Color.blue;
            Vector3 forward = spawnRotation * Vector3.forward;
            Gizmos.DrawRay(spawnPosition + Vector3.up * 1f, forward * 2f);
        }
        
        if (customSpawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(customSpawnPoint.position, Vector3.one * 0.3f);
        }
    }
} 