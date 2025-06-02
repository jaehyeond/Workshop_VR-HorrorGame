using UnityEngine;
using System.Collections.Generic;
using System;

public class CultistManager : MonoBehaviour
{
    public static CultistManager Instance { get; private set; }
    
    [Header("전역 설정")]
    public float globalDetectionRange = 10f;
    public float maxRaycastsPerFrame = 5; // 프레임당 최대 레이캐스트 수
    public bool enableGlobalDebug = true;
    
    [Header("성능 최적화")]
    public float updateInterval = 0.1f; // 업데이트 간격
    public int maxActiveCultists = 10; // 동시 활성 광신도 수
    
    // 플레이어 관련
    private Transform player;
    private VRLocomotion playerLocomotion;
    private bool isPlayerHiding = false;
    private Vector3 lastPlayerPosition;
    
    // 광신도 관리
    private List<CultistAI> allCultists = new List<CultistAI>();
    private Queue<CultistAI> raycastQueue = new Queue<CultistAI>();
    private float lastUpdateTime = 0f;
    
    // 이벤트
    public static event Action<bool> OnPlayerHidingChanged;
    public static event Action<Vector3> OnPlayerPositionChanged;
    
    void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeManager()
    {
        // 플레이어 찾기 (한 번만)
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            playerLocomotion = player.GetComponent<VRLocomotion>();
            lastPlayerPosition = player.position;
            
            DebugLog("CultistManager 초기화 완료 - 플레이어 발견");
        }
        else
        {
            Debug.LogError("[CultistManager] Player 태그가 있는 오브젝트를 찾을 수 없습니다!");
        }
    }
    
    void Update()
    {
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;
        
        UpdatePlayerState();
        ProcessRaycastQueue();
    }
    
    void UpdatePlayerState()
    {
        if (player == null) return;
        
        // 플레이어 위치 업데이트
        Vector3 currentPosition = player.position;
        if (Vector3.Distance(lastPlayerPosition, currentPosition) > 0.1f)
        {
            lastPlayerPosition = currentPosition;
            OnPlayerPositionChanged?.Invoke(currentPosition);
        }
        
        // 플레이어 은신 상태 체크 (한 번만)
        bool currentHidingState = playerLocomotion != null && playerLocomotion.IsHiding();
        if (currentHidingState != isPlayerHiding)
        {
            isPlayerHiding = currentHidingState;
            OnPlayerHidingChanged?.Invoke(isPlayerHiding);
            DebugLog($"플레이어 은신 상태 변경: {isPlayerHiding}");
        }
    }
    
    void ProcessRaycastQueue()
    {
        int processedCount = 0;
        int maxProcess = Mathf.Min((int)maxRaycastsPerFrame, raycastQueue.Count);
        
        for (int i = 0; i < maxProcess; i++)
        {
            if (raycastQueue.Count > 0)
            {
                CultistAI cultist = raycastQueue.Dequeue();
                if (cultist != null && cultist.gameObject.activeInHierarchy)
                {
                    // 레이캐스트 실행을 cultist에게 위임
                    cultist.ProcessVisibilityCheck();
                    processedCount++;
                }
            }
        }
        
        if (processedCount > 0)
        {
            DebugLog($"프레임당 레이캐스트 처리: {processedCount}개");
        }
    }
    
    // 광신도 등록/해제
    public void RegisterCultist(CultistAI cultist)
    {
        if (!allCultists.Contains(cultist))
        {
            allCultists.Add(cultist);
            DebugLog($"광신도 등록: {cultist.name} (총 {allCultists.Count}개)");
        }
    }
    
    public void UnregisterCultist(CultistAI cultist)
    {
        if (allCultists.Contains(cultist))
        {
            allCultists.Remove(cultist);
            DebugLog($"광신도 해제: {cultist.name} (총 {allCultists.Count}개)");
        }
    }
    
    // 레이캐스트 큐에 추가
    public void RequestVisibilityCheck(CultistAI cultist)
    {
        if (!raycastQueue.Contains(cultist))
        {
            raycastQueue.Enqueue(cultist);
        }
    }
    
    // 플레이어 정보 제공
    public Transform GetPlayer() => player;
    public bool IsPlayerHiding() => isPlayerHiding;
    public Vector3 GetPlayerPosition() => lastPlayerPosition;
    public float GetGlobalDetectionRange() => globalDetectionRange;
    
    // 거리 기반 우선순위 계산
    public float GetUpdatePriority(Vector3 cultistPosition)
    {
        if (player == null) return 1f;
        
        float distance = Vector3.Distance(cultistPosition, player.position);
        
        // 가까울수록 높은 우선순위 (낮은 값)
        if (distance < 5f) return 0.1f;      // 매우 가까움 - 매 프레임
        if (distance < 10f) return 0.2f;     // 가까움 - 5프레임마다
        if (distance < 20f) return 0.5f;     // 보통 - 30프레임마다
        return 1f;                           // 멀음 - 60프레임마다
    }
    
    void DebugLog(string message)
    {
        if (enableGlobalDebug)
        {
            Debug.Log($"[CultistManager] {message}");
        }
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
} 