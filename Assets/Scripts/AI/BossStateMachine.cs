using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 보스 전용 상태 머신
/// 일반 적보다 복잡한 상태와 패턴을 관리
/// </summary>
public class BossStateMachine : MonoBehaviour
{
    [Header("상태 타이밍")]
    public float patrolTime = 3f;
    public float observationTime = 2f;
    public float combatStateTime = 1f;
    
    public enum BossState
    {
        Intro,          // 등장 연출
        Patrol,         // 순찰
        Observing,      // 플레이어 관찰
        Approaching,    // 접근
        Combat,         // 전투 (공격 선택)
        Attacking,      // 공격 중
        Retreating,     // 후퇴 (체력 회복)
        PhaseTransition,// 단계 변화
        Stunned,        // 스턴 상태
        Death           // 사망
    }
    
    // 컴포넌트 참조
    private NavMeshAgent agent;
    private Animator animator;
    private BossAI bossAI;
    
    // 상태 관리
    private BossState currentState = BossState.Intro;
    private float stateTimer = 0f;
    private Transform player;
    
    // 상태 플래그
    public BossState CurrentState => currentState;
    
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        bossAI = GetComponent<BossAI>();
        
        // 플레이어 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }
    
    void Update()
    {
        stateTimer += Time.deltaTime;
        UpdateCurrentState();
        UpdateAnimatorParams();
    }
    
    void UpdateCurrentState()
    {
        switch (currentState)
        {
            case BossState.Intro:
                HandleIntroState();
                break;
            case BossState.Patrol:
                HandlePatrolState();
                break;
            case BossState.Observing:
                HandleObservingState();
                break;
            case BossState.Approaching:
                HandleApproachingState();
                break;
            case BossState.Combat:
                HandleCombatState();
                break;
            case BossState.Attacking:
                HandleAttackingState();
                break;
            case BossState.Retreating:
                HandleRetreatingState();
                break;
            case BossState.PhaseTransition:
                HandlePhaseTransitionState();
                break;
            case BossState.Stunned:
                HandleStunnedState();
                break;
            case BossState.Death:
                HandleDeathState();
                break;
        }
    }
    
    public void SetState(BossState newState)
    {
        if (currentState == newState) return;
        
        ExitState(currentState);
        currentState = newState;
        stateTimer = 0f;
        EnterState(newState);
        
        Debug.Log($"[BossStateMachine] 상태 변경: {currentState}");
    }
    
    void ExitState(BossState state)
    {
        // 상태 종료 처리
        switch (state)
        {
            case BossState.Patrol:
                break;
            case BossState.Attacking:
                break;
            case BossState.Retreating:
                break;
        }
    }
    
    void EnterState(BossState state)
    {
        // 상태 진입 처리
        switch (state)
        {
            case BossState.Intro:
                agent.isStopped = true;
                break;
            case BossState.Patrol:
                agent.isStopped = false;
                agent.speed = bossAI ? bossAI.walkSpeed : 2f;
                break;
            case BossState.Observing:
                agent.isStopped = true;
                break;
            case BossState.Approaching:
                agent.isStopped = false;
                break;
            case BossState.Combat:
                agent.isStopped = true;
                break;
            case BossState.Attacking:
                agent.isStopped = true;
                break;
            case BossState.Retreating:
                agent.isStopped = false;
                break;
            case BossState.PhaseTransition:
                agent.isStopped = true;
                break;
            case BossState.Stunned:
                agent.isStopped = true;
                break;
            case BossState.Death:
                agent.isStopped = true;
                break;
        }
    }
    
    void HandleIntroState()
    {
        // 인트로는 BossAI에서 처리
        // 일정 시간 후 자동으로 Patrol로 전환
        if (stateTimer >= 3f)
        {
            SetState(BossState.Patrol);
        }
    }
    
    void HandlePatrolState()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // 플레이어 감지 시 관찰 상태로
        if (distanceToPlayer <= (bossAI ? bossAI.detectionRange : 15f))
        {
            SetState(BossState.Observing);
        }
        else if (stateTimer >= patrolTime)
        {
            // 순찰 지점 이동
            MoveToRandomPatrolPoint();
            stateTimer = 0f;
        }
    }
    
    void HandleObservingState()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // 관찰 시간이 끝나면 접근
        if (stateTimer >= observationTime)
        {
            if (distanceToPlayer <= (bossAI ? bossAI.attackRange : 2.5f))
            {
                SetState(BossState.Combat);
            }
            else
            {
                SetState(BossState.Approaching);
            }
        }
    }
    
    void HandleApproachingState()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // 플레이어에게 접근
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(player.position);
        }
        
        // 공격 범위에 도달하면 전투 상태로
        if (distanceToPlayer <= (bossAI ? bossAI.attackRange : 2.5f))
        {
            SetState(BossState.Combat);
        }
        // 너무 멀어지면 다시 순찰
        else if (distanceToPlayer > (bossAI ? bossAI.detectionRange : 15f))
        {
            SetState(BossState.Patrol);
        }
    }
    
    void HandleCombatState()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // 공격 범위 내에 있으면 공격
        if (distanceToPlayer <= (bossAI ? bossAI.attackRange : 2.5f))
        {
            SetState(BossState.Attacking);
        }
        // 범위를 벗어나면 다시 접근
        else if (distanceToPlayer > (bossAI ? bossAI.attackRange : 2.5f) * 1.5f)
        {
            SetState(BossState.Approaching);
        }
        
        // 체력이 낮으면 후퇴 고려
        if (bossAI != null && bossAI.HealthPercentage < 0.3f && Random.Range(0f, 1f) < 0.3f)
        {
            SetState(BossState.Retreating);
        }
    }
    
    void HandleAttackingState()
    {
        // 공격 중에는 이동 정지
        // 공격 애니메이션이 끝나면 다시 전투 상태로
        
        // 애니메이터 상태 체크로 공격 종료 감지
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            
            // 공격 애니메이션이 끝났는지 체크
            if (stateInfo.IsName("Idle") || stateInfo.IsName("Walk") || stateInfo.normalizedTime >= 0.9f)
            {
                SetState(BossState.Combat);
            }
        }
        else if (stateTimer >= 2f) // 애니메이터가 없으면 시간으로 체크
        {
            SetState(BossState.Combat);
        }
    }
    
    void HandleRetreatingState()
    {
        // 플레이어로부터 멀어지며 체력 회복 시간 확보
        if (player != null && agent.isOnNavMesh)
        {
            Vector3 retreatDirection = (transform.position - player.position).normalized;
            Vector3 retreatPosition = transform.position + retreatDirection * 8f;
            
            // NavMesh 위의 유효한 위치 찾기
            NavMeshHit hit;
            if (NavMesh.SamplePosition(retreatPosition, out hit, 10f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
        
        // 일정 시간 후 다시 전투 복귀
        if (stateTimer >= 3f)
        {
            SetState(BossState.Approaching);
        }
    }
    
    void HandlePhaseTransitionState()
    {
        // 단계 변화 중에는 모든 행동 정지
        // BossAI에서 처리 완료 시 다시 전투 상태로
        
        if (stateTimer >= 3f) // 단계 변화 시간 완료
        {
            SetState(BossState.Combat);
        }
    }
    
    void HandleStunnedState()
    {
        // 스턴 상태 - 모든 행동 정지
        if (stateTimer >= 2f) // 2초 스턴
        {
            SetState(BossState.Combat);
        }
    }
    
    void HandleDeathState()
    {
        // 사망 상태 - 아무것도 하지 않음
        agent.isStopped = true;
    }
    
    void MoveToRandomPatrolPoint()
    {
        if (!agent.isOnNavMesh) return;
        
        // 현재 위치 기준 랜덤 순찰 지점
        Vector3 randomDirection = Random.insideUnitSphere * 10f;
        randomDirection += transform.position;
        randomDirection.y = transform.position.y;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, 10f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
    
    void UpdateAnimatorParams()
    {
        if (animator == null) return;
        
        // 상태별 애니메이터 파라미터 업데이트
        animator.SetBool("IsPatrolling", currentState == BossState.Patrol);
        animator.SetBool("IsObserving", currentState == BossState.Observing);
        animator.SetBool("IsApproaching", currentState == BossState.Approaching);
        animator.SetBool("IsInCombat", currentState == BossState.Combat);
        animator.SetBool("IsAttacking", currentState == BossState.Attacking);
        animator.SetBool("IsRetreating", currentState == BossState.Retreating);
        animator.SetBool("IsStunned", currentState == BossState.Stunned);
        animator.SetBool("IsDead", currentState == BossState.Death);
        
        // 이동 속도
        float speed = agent != null ? agent.velocity.magnitude : 0f;
        animator.SetFloat("Speed", speed);
        
        // 보스 단계
        if (bossAI != null)
        {
            animator.SetInteger("BossPhase", bossAI.CurrentPhase);
        }
    }
    
    // 외부에서 호출할 수 있는 상태 전환 메서드들
    public void TriggerPhaseTransition()
    {
        SetState(BossState.PhaseTransition);
    }
    
    public void TriggerStun()
    {
        SetState(BossState.Stunned);
    }
    
    public void TriggerDeath()
    {
        SetState(BossState.Death);
    }
    
    public void ForceState(BossState state)
    {
        SetState(state);
    }
    
    // 상태 체크 프로퍼티들
    public bool IsInCombat => currentState == BossState.Combat || currentState == BossState.Attacking;
    public bool IsMoving => currentState == BossState.Patrol || currentState == BossState.Approaching || currentState == BossState.Retreating;
    public bool IsVulnerable => currentState != BossState.PhaseTransition && currentState != BossState.Death;
} 