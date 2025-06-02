using UnityEngine;
using UnityEngine.AI;

public class CultistAI : MonoBehaviour
{
    [Header("AI 설정")]
    public float detectionRange = 10f;
    public float attackRange = 2f;
    
    [Header("이동 설정")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 4f;
    
    [Header("참조")]
    public Transform prayingSpot;
    
    [Header("디버그")]
    public bool enableDebugLogs = true;
    
    // 컴포넌트 참조
    private NavMeshAgent agent;
    private Animator animator;
    private CultistStateMachine stateMachine;
    
    // 플레이어 관련
    private Transform player;
    private Vector3 lastKnownPlayerPosition;
    
    // 성능 최적화
    private float lastVisibilityCheck = 0f;
    private float visibilityCheckInterval = 0.2f; // 기본 간격
    private bool lastVisibilityResult = false;
    
    void Start()
    {
        InitializeComponents();
        RegisterWithManager();
        SetupInitialState();
    }
    
    void InitializeComponents()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        stateMachine = GetComponent<CultistStateMachine>();
        
        if (stateMachine == null)
        {
            stateMachine = gameObject.AddComponent<CultistStateMachine>();
        }
        
        // NavMeshAgent 초기 설정
        agent.speed = walkSpeed;
        agent.stoppingDistance = 0.1f;
        
        DebugLog("컴포넌트 초기화 완료");
    }
    
    void RegisterWithManager()
    {
        // 매니저에 등록
        if (CultistManager.Instance != null)
        {
            CultistManager.Instance.RegisterCultist(this);
            player = CultistManager.Instance.GetPlayer();
            
            // 이벤트 구독
            CultistManager.OnPlayerHidingChanged += OnPlayerHidingChanged;
            CultistManager.OnPlayerPositionChanged += OnPlayerPositionChanged;
            
            DebugLog("매니저에 등록 완료");
        }
        else
        {
            Debug.LogError($"[{name}] CultistManager를 찾을 수 없습니다!");
        }
    }
    
    void SetupInitialState()
    {
        if (prayingSpot != null)
        {
            float distanceToPrayingSpot = Vector3.Distance(transform.position, prayingSpot.position);
            
            if (distanceToPrayingSpot > 1f)
            {
                stateMachine.SetState(CultistStateMachine.AIState.MovingToPrayingSpot);
                agent.SetDestination(prayingSpot.position);
                DebugLog("기도 위치로 이동 시작");
            }
            else
            {
                stateMachine.SetState(CultistStateMachine.AIState.Praying);
                DebugLog("기도 상태 시작");
            }
        }
        else
        {
            stateMachine.SetState(CultistStateMachine.AIState.Praying);
            DebugLog("기도 위치가 없어 즉시 기도 상태로 전환");
        }
    }
    
    void Update()
    {
        // 성능 최적화: 거리 기반 업데이트 간격 조정
        UpdateVisibilityCheckInterval();
        
        // 플레이어 감지 체크
        bool canSeePlayer = CheckPlayerVisibility();
        
        // 애니메이터 파라미터 업데이트
        animator.SetBool("PlayerDetected", canSeePlayer);
        
        // 상태별 AI 로직 처리
        HandleAILogic(canSeePlayer);
        
        // 공격 범위 체크
        HandleAttackRangeCheck(canSeePlayer);
    }
    
    void UpdateVisibilityCheckInterval()
    {
        if (CultistManager.Instance != null && player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            
            // 거리에 따른 체크 간격 조정
            if (distance < 5f) visibilityCheckInterval = 0.1f;      // 가까움 - 빠른 체크
            else if (distance < 10f) visibilityCheckInterval = 0.2f; // 보통 - 기본 체크
            else if (distance < 20f) visibilityCheckInterval = 0.5f; // 멀음 - 느린 체크
            else visibilityCheckInterval = 1f;                      // 매우 멀음 - 매우 느린 체크
        }
    }
    
    bool CheckPlayerVisibility()
    {
        // 성능 최적화: 간격 체크
        if (Time.time - lastVisibilityCheck < visibilityCheckInterval)
        {
            return lastVisibilityResult;
        }
        
        lastVisibilityCheck = Time.time;
        
        // 매니저를 통한 최적화된 감지
        if (CultistManager.Instance != null)
        {
            // 레이캐스트 큐에 요청
            CultistManager.Instance.RequestVisibilityCheck(this);
        }
        
        return lastVisibilityResult;
    }
    
    // 매니저에서 호출되는 실제 가시성 체크
    public void ProcessVisibilityCheck()
    {
        if (player == null) 
        {
            lastVisibilityResult = false;
            return;
        }
        
        // PlayerDetectionSystem을 통한 최적화된 감지
        lastVisibilityResult = PlayerDetectionSystem.CheckPlayerVisibility(
            transform, 
            player, 
            detectionRange, 
            enableDebugLogs
        );
        
        if (lastVisibilityResult)
        {
            lastKnownPlayerPosition = player.position;
            RotateTowards(player.position);
        }
    }
    
    void HandleAILogic(bool canSeePlayer)
    {
        switch (stateMachine.CurrentState)
        {
            case CultistStateMachine.AIState.Praying:
                if (canSeePlayer)
                {
                    DebugLog("기도 중 플레이어 발견!");
                    stateMachine.StartObserving();
                    lastKnownPlayerPosition = player.position;
                }
                break;
                
            case CultistStateMachine.AIState.MovingToPrayingSpot:
                if (canSeePlayer)
                {
                    stateMachine.StartObserving();
                    lastKnownPlayerPosition = player.position;
                }
                break;
                
            case CultistStateMachine.AIState.Observing:
                if (!canSeePlayer)
                {
                    stateMachine.SetState(CultistStateMachine.AIState.Praying);
                }
                else
                {
                    RotateTowards(player.position);
                }
                break;
                
            case CultistStateMachine.AIState.Chasing:
                if (canSeePlayer)
                {
                    lastKnownPlayerPosition = player.position;
                }
                else
                {
                    DebugLog("추격 중 플레이어를 놓침");
                    stateMachine.LoseTarget();
                }
                break;
                
            case CultistStateMachine.AIState.Attacking:
                if (!canSeePlayer)
                {
                    DebugLog("공격 중 플레이어를 놓침");
                    stateMachine.LoseTarget();
                }
                break;
        }
    }
    
    void HandleAttackRangeCheck(bool canSeePlayer)
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool inAttackRange = distanceToPlayer <= attackRange;
        
        // 공격 상태가 아닐 때만 InAttackRange 업데이트
        if (!stateMachine.IsAttacking)
        {
            animator.SetBool("InAttackRange", inAttackRange);
        }
        
        // 추격 중이고 공격 범위에 들어왔다면 공격 상태로 전환
        if (stateMachine.IsChasing && inAttackRange && canSeePlayer)
        {
            stateMachine.SetState(CultistStateMachine.AIState.Attacking);
            DebugLog("공격 범위 진입 - 공격 상태로 전환");
        }
        
        // 공격 중이고 범위를 벗어났다면 추격 재개
        if (stateMachine.IsAttacking && (!inAttackRange || !canSeePlayer))
        {
            // 공격 범위를 조금이라도 벗어나면 즉시 추격 재개
            if (distanceToPlayer > attackRange * 1.2f || !canSeePlayer)
            {
                DebugLog("공격 범위 벗어남 - 추격 재개");
                animator.SetBool("InAttackRange", false);
                stateMachine.SetState(CultistStateMachine.AIState.Chasing);
            }
        }
    }
    
    // 이벤트 핸들러
    void OnPlayerHidingChanged(bool isHiding)
    {
        if (isHiding && (stateMachine.IsChasing || stateMachine.IsAttacking))
        {
            DebugLog("플레이어 은신 - 추적 중단");
            stateMachine.LoseTarget();
        }
    }
    
    void OnPlayerPositionChanged(Vector3 newPosition)
    {
        if (stateMachine.IsChasing)
        {
            lastKnownPlayerPosition = newPosition;
        }
    }
    
    // 유틸리티 메서드
    public void RotateTowards(Vector3 targetPosition)
    {
        targetPosition.y = transform.position.y;
        Vector3 direction = (targetPosition - transform.position).normalized;
        
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 5f * Time.deltaTime);
        }
    }
    
    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{name}] {message}");
        }
    }
    
    void OnDestroy()
    {
        // 매니저에서 해제
        if (CultistManager.Instance != null)
        {
            CultistManager.Instance.UnregisterCultist(this);
        }
        
        // 이벤트 구독 해제
        CultistManager.OnPlayerHidingChanged -= OnPlayerHidingChanged;
        CultistManager.OnPlayerPositionChanged -= OnPlayerPositionChanged;
    }
    
    // 에디터에서 확인용
    void OnDrawGizmosSelected()
    {
        // 감지 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // 공격 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // 기도 위치 연결선
        if (prayingSpot != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, prayingSpot.position);
        }
    }
}