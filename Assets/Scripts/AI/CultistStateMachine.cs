using UnityEngine;
using UnityEngine.AI;

public class CultistStateMachine : MonoBehaviour
{
    [Header("상태 설정")]
    public float idleObservationTime = 2.5f;
    public float attackCooldown = 2f;
    public float pathRecalculationTime = 0.5f;
    
    // 상태 열거형
    public enum AIState
    {
        Praying,
        MovingToPrayingSpot,
        Observing,
        Chasing,
        Attacking,
        Returning
    }
    
    // 컴포넌트 참조
    private NavMeshAgent agent;
    private Animator animator;
    private CultistAI cultistAI;
    
    // 상태 관련
    private AIState currentState = AIState.Praying;
    private float stateTimer = 0f;
    private float pathUpdateTimer = 0f;
    //private float lastUpdateTime = 0f;
    private float updatePriority = 1f; // 매니저에서 설정
    
    // 상태 플래그
    private bool isPraying = false;
    private bool isObserving = false;
    private bool isChasing = false;
    private bool isAttacking = false;
    
    public AIState CurrentState => currentState;
    public bool IsPraying => isPraying;
    public bool IsObserving => isObserving;
    public bool IsChasing => isChasing;
    public bool IsAttacking => isAttacking;
    
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        cultistAI = GetComponent<CultistAI>();
    }
    
    void Start()
    {
        InitializeAnimatorParameters();
        
        // 매니저에서 업데이트 우선순위 받기
        if (CultistManager.Instance != null)
        {
            updatePriority = CultistManager.Instance.GetUpdatePriority(transform.position);
        }
    }
    
    void Update()
    {
        // 우선순위 기반 업데이트 제거 - 매 프레임 정상 업데이트
        stateTimer += Time.deltaTime;
        UpdateCurrentState();
    }
    
    void UpdateCurrentState()
    {
        switch (currentState)
        {
            case AIState.Praying:
                HandlePrayingState();
                break;
                
            case AIState.MovingToPrayingSpot:
                HandleMovingToPrayingSpotState();
                break;
                
            case AIState.Observing:
                HandleObservingState();
                break;
                
            case AIState.Chasing:
                HandleChasingState();
                break;
                
            case AIState.Attacking:
                HandleAttackingState();
                break;
        }
    }
    
    public void SetState(AIState newState)
    {
        if (currentState == newState) return;
        
        Debug.Log($"[{name}] 상태 변경: {currentState} -> {newState}");
        
        // 이전 상태 종료 처리
        ExitState(currentState);
        
        // 새 상태 진입 처리
        currentState = newState;
        stateTimer = 0f;
        EnterState(newState);
    }
    
    void ExitState(AIState state)
    {
        // 상태별 종료 처리
        switch (state)
        {
            case AIState.Praying:
                isPraying = false;
                break;
            case AIState.Observing:
                isObserving = false;
                break;
            case AIState.Chasing:
                isChasing = false;
                break;
            case AIState.Attacking:
                isAttacking = false;
                break;
        }
    }
    
    void EnterState(AIState state)
    {
        // 상태별 진입 처리
        switch (state)
        {
            case AIState.Praying:
                isPraying = true;
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.ResetPath();
                
                // 애니메이터 파라미터 설정
                animator.SetBool("PlayerDetected", false);
                animator.SetBool("StartChase", false);
                animator.SetBool("InAttackRange", false);
                animator.SetBool("LostPlayer", false);
                break;
                
            case AIState.MovingToPrayingSpot:
                agent.isStopped = false;
                agent.speed = cultistAI.runSpeed;
                agent.stoppingDistance = 0.1f;
                animator.SetBool("InAttackRange", false);
                break;
                
            case AIState.Observing:
                isObserving = true;
                animator.SetBool("ReturnToPraying", false);
                break;
                
            case AIState.Chasing:
                isChasing = true;
                agent.isStopped = false;
                agent.speed = cultistAI.runSpeed;
                pathUpdateTimer = 0f;
                
                // 즉시 플레이어 위치로 목적지 설정
                if (CultistManager.Instance != null && agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    Vector3 playerPos = CultistManager.Instance.GetPlayerPosition();
                    agent.SetDestination(playerPos);
                }
                break;
                
            case AIState.Attacking:
                isAttacking = true;
                agent.isStopped = true;
                break;
        }
    }
    
    void HandlePrayingState()
    {
        // 위치 고정 (Praying 상태에서 이동 방지)
        if (cultistAI.prayingSpot != null)
        {
            Vector3 targetPos = new Vector3(
                cultistAI.prayingSpot.position.x, 
                transform.position.y, 
                cultistAI.prayingSpot.position.z
            );
            
            if (Vector3.Distance(transform.position, targetPos) > 0.1f)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
            }
        }
        
        // 플레이어 감지는 CultistAI에서 처리
    }
    
    void HandleMovingToPrayingSpotState()
    {
        if (cultistAI.prayingSpot == null) return;
        
        float distanceToPrayingSpot = Vector3.Distance(transform.position, cultistAI.prayingSpot.position);
        bool closeEnough = distanceToPrayingSpot <= 1.2f;
        bool agentStopped = !agent.pathPending && agent.remainingDistance <= 0.8f;
        bool velocitySlow = agent.velocity.magnitude < 0.2f;
        
        if (closeEnough || (agentStopped && distanceToPrayingSpot <= 2f) || (velocitySlow && distanceToPrayingSpot <= 1.5f))
        {
            // 정확한 위치로 이동
            transform.position = new Vector3(
                cultistAI.prayingSpot.position.x, 
                transform.position.y, 
                cultistAI.prayingSpot.position.z
            );
            
            animator.SetBool("StartChase", false);
            animator.SetBool("LostPlayer", false);
            animator.SetBool("ReturnToPraying", true);
            
            SetState(AIState.Praying);
        }
    }
    
    void HandleObservingState()
    {
        if (stateTimer >= idleObservationTime)
        {
            // 추격 시작
            SetState(AIState.Chasing);
            
            animator.SetBool("StartChase", true);
            animator.SetBool("ReturnToPraying", false);
            animator.SetBool("LostPlayer", false);
            animator.SetFloat("IdleTimer", idleObservationTime);
            
            // 목적지 설정은 EnterState에서 처리됨
        }
    }
    
    void HandleChasingState()
    {
        // 경로 업데이트
        pathUpdateTimer += Time.deltaTime;
        if (pathUpdateTimer >= pathRecalculationTime)
        {
            pathUpdateTimer = 0f;
            
            if (CultistManager.Instance != null && agent != null && agent.enabled && agent.isOnNavMesh)
            {
                Vector3 playerPos = CultistManager.Instance.GetPlayerPosition();
                agent.SetDestination(playerPos);
            }
        }
    }
    
    void HandleAttackingState()
    {
        // 플레이어 방향으로 회전
        if (CultistManager.Instance != null)
        {
            Vector3 playerPos = CultistManager.Instance.GetPlayerPosition();
            cultistAI.RotateTowards(playerPos);
        }
        
        // 공격 쿨다운 처리
        if (stateTimer >= attackCooldown)
        {
            // 공격 쿨다운 완료 - 다시 공격 가능
            stateTimer = 0f;
            
            // 여전히 공격 범위에 있는지 확인
            if (CultistManager.Instance != null)
            {
                Vector3 playerPos = CultistManager.Instance.GetPlayerPosition();
                float distanceToPlayer = Vector3.Distance(transform.position, playerPos);
                
                if (distanceToPlayer <= cultistAI.attackRange)
                {
                    // 여전히 공격 범위에 있으면 공격 계속
                    animator.SetBool("InAttackRange", true);
                }
                else
                {
                    // 공격 범위를 벗어났으면 추격 재개
                    animator.SetBool("InAttackRange", false);
                    SetState(AIState.Chasing);
                }
            }
        }
    }
    
    public void StartObserving()
    {
        SetState(AIState.Observing);
        
        // 모든 이전 상태 파라미터 리셋
        animator.SetBool("StartChase", false);
        animator.SetBool("ReturnToPraying", false);
        animator.SetBool("LostPlayer", false);
        animator.SetBool("InAttackRange", false);
    }
    
    public void LoseTarget()
    {
        if (cultistAI.prayingSpot != null)
        {
            SetState(AIState.MovingToPrayingSpot);
            
            // NavMeshAgent 설정
            agent.isStopped = false;
            agent.speed = cultistAI.runSpeed;
            agent.stoppingDistance = 0.1f;
            
            // 목적지 설정
            Vector3 targetPosition = new Vector3(
                cultistAI.prayingSpot.position.x, 
                transform.position.y, 
                cultistAI.prayingSpot.position.z
            );
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.SetDestination(targetPosition);
            }
            
            // 애니메이터 파라미터
            animator.SetBool("PlayerDetected", false);
            animator.SetBool("InAttackRange", false);
        }
        else
        {
            SetState(AIState.Praying);
            animator.SetBool("PlayerDetected", false);
            animator.SetBool("StartChase", false);
            animator.SetBool("InAttackRange", false);
            animator.SetBool("LostPlayer", true);
            animator.SetBool("ReturnToPraying", true);
        }
    }
    
    void InitializeAnimatorParameters()
    {
        if (animator == null) return;
        
        animator.SetBool("PlayerDetected", false);
        animator.SetBool("StartChase", false);
        animator.SetBool("InAttackRange", false);
        animator.SetBool("LostPlayer", false);
        animator.SetBool("ReturnToPraying", false);
        
        // 피격/사망 관련 파라미터 초기화 추가
        animator.SetBool("IsDead", false);
        // Trigger는 초기화할 필요 없음 (자동으로 false 상태)
        
        Debug.Log($"[{name}] 애니메이터 파라미터 초기화 완료");
    }
    
    //우선순위 업데이트 (매니저에서 호출)
    public void UpdatePriority(float newPriority)
    {
        updatePriority = newPriority;
    }
} 