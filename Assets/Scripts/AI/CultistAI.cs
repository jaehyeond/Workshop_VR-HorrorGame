using UnityEngine;
using UnityEngine.AI;

public class CultistAI : MonoBehaviour
{
    [Header("AI 설정")]
    public float detectionRange = 10f;
    public float attackRange = 1.5f;
    
    [Header("이동 설정")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 4f;
    
    [Header("체력 설정")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float hitStunDuration = 1f; // 맞았을 때 경직 시간
    
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
    
    // 데미지 관련
    private bool isStunned = false;
    private float stunEndTime = 0f;
    private bool isDead = false;
    
    // 성능 최적화
    private float lastVisibilityCheck = 0f;
    private float visibilityCheckInterval = 0.2f; // 기본 간격
    private bool lastVisibilityResult = false;
    
    void Start()
    {
        InitializeComponents();
        RegisterWithManager();
        SetupInitialState();
        
        // 체력 초기화
        currentHealth = maxHealth;
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
        
        // NavMeshAgent 초기 설정 - 더 가까이 접근하도록
        agent.speed = walkSpeed;
        agent.stoppingDistance = 0.5f; // 0.1f에서 0.5f로 변경 (너무 가까이 가지 않도록)
        agent.radius = 0.3f; // 반지름 명시적 설정
        agent.height = 1.8f; // 높이 명시적 설정
        
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
        // 죽었거나 스턴 상태면 AI 로직 중단
        if (isDead) return;
        
        // 스턴 상태 체크
        if (isStunned)
        {
            if (Time.time >= stunEndTime)
            {
                isStunned = false;
                animator.SetBool("IsStunned", false);
                DebugLog("스턴 상태 해제");
            }
            else
            {
                return; // 스턴 중이면 AI 로직 실행하지 않음
            }
        }
        
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
    
    // 데미지 받기
    public void TakeDamage(float damage, Vector3 attackPosition)
    {
        
   
        // ✅ 체력 감소 - 이게 빠져있었음!
        currentHealth -= damage;
    
       
        // 체력이 0 이하면 사망
        if (currentHealth <= 0)
        {
            Debug.Log($"[{name}] 체력 0 이하! Die() 호출!");
            Die();
            return;
        }
        
        // 스턴 상태로 전환
        ApplyStun();
        
        // 공격받은 방향으로 약간 밀려남
        Vector3 knockbackDirection = (transform.position - attackPosition).normalized;
        knockbackDirection.y = 0; // Y축 제거
        
        if (agent.isOnNavMesh)
        {
            agent.Move(knockbackDirection * 0.5f);
        }
        
        // 플레이어를 발견한 상태로 전환 (공격받았으니까)
        if (player != null)
        {
            lastKnownPlayerPosition = player.position;
            stateMachine.StartObserving();
        }
    }
    
    private void ApplyStun()
    {
        isStunned = true;
        stunEndTime = Time.time + hitStunDuration;
        
        // 애니메이터에 스턴 상태 전달
        if (animator != null)
        {
            animator.SetBool("IsStunned", true);
            animator.SetTrigger("Hit");
        }
        
        // NavMeshAgent 일시 정지
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        
        DebugLog($"스턴 상태 적용 ({hitStunDuration}초)");
        
        // 스턴 해제 시 NavMeshAgent 재개
        Invoke(nameof(ResumeMovement), hitStunDuration);
    }
    
    private void ResumeMovement()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh && !isDead)
        {
            agent.isStopped = false;
        }
        
        // ✅ 스턴 상태 해제 (이게 빠져있었음!)
        isStunned = false;
        if (animator != null)
        {
            animator.SetBool("IsStunned", false);
        }
        Debug.Log($"[{name}] 스턴 해제! 정상 동작 재개");
    }
    
    private void Die()
    {
        isDead = true;
        currentHealth = 0;
        
        DebugLog("사망!");
        
        // 사망 애니메이션 트리거 추가
        if (animator != null)
        {
            Debug.Log($"[{name}] Die 애니메이션 트리거 호출!");
            animator.SetBool("IsDead", true);
            animator.SetTrigger("Die");
            
            // 애니메이터 상태 확인
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[{name}] 사망 시 애니메이터 상태: {currentState.fullPathHash}");
            
            // Parameters 확인
            Debug.Log($"[{name}] IsDead 파라미터: {animator.GetBool("IsDead")}");
        }
        else
        {
            Debug.LogError($"[{name}] Die - Animator가 null입니다!");
        }
        
        // NavMeshAgent 완전히 정리 (에러 방지)
        if (agent != null)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath(); // 경로 초기화
            }
            agent.enabled = false;
        }
        
        // Collider 설정 변경 - 더 이상 타격받지 않도록
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                col.isTrigger = true; // 물리 충돌 제거
            }
        }
        
        // Rigidbody가 있다면 kinematic으로 변경
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        // 매니저에서 제거
        if (CultistManager.Instance != null)
        {
            CultistManager.Instance.UnregisterCultist(this);
        }
        
        // 5초 후 오브젝트 제거
        Destroy(gameObject, 5f);
    }
    
    // 체력 회복 (필요시 사용)
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        DebugLog($"체력 회복 +{amount:F1}! 현재 체력: {currentHealth:F1}/{maxHealth}");
    }
    
    // 상태 확인 프로퍼티들
    public bool IsDead => isDead;
    public bool IsStunned => isStunned;
    public float HealthPercentage => currentHealth / maxHealth;
    
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
        
        // 체력 바 표시
        if (Application.isPlaying && !isDead)
        {
            Vector3 healthBarPos = transform.position + Vector3.up * 2.5f;
            float healthPercentage = currentHealth / maxHealth;
            
            Gizmos.color = Color.red;
            Gizmos.DrawLine(healthBarPos - Vector3.right * 0.5f, healthBarPos + Vector3.right * 0.5f);
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(healthBarPos - Vector3.right * 0.5f, 
                           healthBarPos + Vector3.right * (healthPercentage - 0.5f));
        }
    }
}