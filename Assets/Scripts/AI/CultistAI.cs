using UnityEngine;
using UnityEngine.AI;

public class CultistAI : MonoBehaviour
{
    [Header("AI 설정")]
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float idleObservationTime = 2.5f;
    public float attackCooldown = 2f; // 공격 후 대기 시간
    
    [Header("이동 설정")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 4f;
    public float pathRecalculationTime = 0.5f; // 경로 재계산 주기
    
    [Header("NavMesh 설정")]
    public bool useNavMeshObstacles = true;   // 동적 장애물 사용
    public bool adaptivePathfinding = true;   // 적응형 경로 탐색
    
    [Header("참조")]
    public Transform prayingSpot;
    
    [Header("디버그")]
    public bool enableDebugLogs = true;
    
    // 내부 변수
    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    
    private float idleTimer = 0f;
    private float pathUpdateTimer = 0f;
    private float attackTimer = 0f; // 공격 타이머 추가
    private bool isChasing = false;
    private bool isObserving = false;
    private bool isPraying = false;
    private bool isAttacking = false; // 공격 상태 플래그 추가
    private Vector3 lastKnownPlayerPosition;
    
    // AI 상태 열거형
    private enum AIState
    {
        Praying,
        MovingToPrayingSpot,
        Observing,
        Chasing,
        Attacking,
        Returning
    }
    
    private AIState currentState = AIState.Praying;
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // Player 태그로 플레이어 찾기
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError("Player 태그가 있는 오브젝트를 찾을 수 없습니다!");
            enabled = false;
            return;
        }
        
        // NavMeshAgent 초기 설정
        agent.speed = walkSpeed;
        agent.stoppingDistance = 0.1f; // 기도 위치에 정확히 도달하도록 수정
        
        // 애니메이터 초기 상태 설정
        InitializeAnimatorParameters();
        
        // 초기 상태 결정
        if(prayingSpot != null)
        {
            float distanceToPrayingSpot = Vector3.Distance(transform.position, prayingSpot.position);
            
            if(distanceToPrayingSpot > 1f)
            {
                // 기도 위치로 이동
                SetState(AIState.MovingToPrayingSpot);
                agent.SetDestination(prayingSpot.position);
                DebugLog("기도 위치로 이동 시작");
            }
            else
            {
                // 이미 기도 위치에 있음
                SetState(AIState.Praying);
                DebugLog("기도 상태 시작");
            }
        }
        else
        {
            //Debug.LogWarning("Praying Spot이 설정되지 않았습니다!");
            SetState(AIState.Praying);
        }
    }
    
    void InitializeAnimatorParameters()
    {
        if (animator == null) return;
        
        DebugLog("애니메이터 파라미터 강제 초기화 시작");
        
        // 모든 애니메이터 파라미터를 강제로 초기 상태로 설정
        animator.SetBool("PlayerDetected", false);
        animator.SetBool("StartChase", false);
        animator.SetBool("InAttackRange", false);
        animator.SetBool("LostPlayer", false);
        animator.SetBool("ReturnToPraying", false);
        animator.SetFloat("IdleTimer", 0f);
        
        // 강제로 한 번 더 설정 (Unity 에디터에서 수동 설정된 값 덮어쓰기)
        animator.Update(0f);
        animator.SetBool("PlayerDetected", false);
        animator.SetBool("StartChase", false);
        animator.SetBool("InAttackRange", false);
        animator.SetBool("LostPlayer", false);
        animator.SetBool("ReturnToPraying", false);
        
        DebugLog("애니메이터 파라미터 강제 초기화 완료");
    }
    
    void SetState(AIState newState)
    {
        if (currentState == newState) return;
        
        DebugLog($"상태 변경: {currentState} -> {newState}");
        currentState = newState;
        
        // 상태별 초기 설정
        switch (newState)
        {
            case AIState.Praying:
                isPraying = true;
                isObserving = false;
                isChasing = false;
                isAttacking = false;
                agent.isStopped = true;
                agent.velocity = Vector3.zero; // 완전 정지
                agent.ResetPath(); // 경로 초기화
                
                // 기도 상태로 전환할 때 모든 파라미터 리셋
                animator.SetBool("PlayerDetected", false);
                animator.SetBool("StartChase", false);
                animator.SetBool("InAttackRange", false);
                animator.SetBool("LostPlayer", false);
                // ReturnToPraying은 이미 설정되어 있을 수 있으므로 유지
                break;
                
            case AIState.MovingToPrayingSpot:
                isPraying = false;
                isObserving = false;
                isChasing = false;
                isAttacking = false;
                agent.isStopped = false;
                agent.speed = runSpeed; // Fast_Run 속도로 이동
                agent.stoppingDistance = 0.1f; // 정확한 도착을 위해
                
                // Fast_Run 상태를 유지하기 위해 StartChase는 건드리지 않음
                animator.SetBool("InAttackRange", false);
                break;
                
            case AIState.Observing:
                isPraying = false;
                isObserving = true;
                isChasing = false;
                isAttacking = false;
                agent.isStopped = true;
                idleTimer = 0f;
                
                // 관찰 상태로 전환할 때 기도 관련 파라미터 리셋
                animator.SetBool("ReturnToPraying", false);
                break;
                
            case AIState.Chasing:
                isPraying = false;
                isObserving = false;
                isChasing = true;
                isAttacking = false;
                agent.isStopped = false;
                agent.speed = runSpeed;
                break;
                
            case AIState.Attacking:
                isPraying = false;
                isObserving = false;
                isChasing = false;
                isAttacking = true;
                agent.isStopped = true;
                attackTimer = 0f;
                break;
        }
    }
    
    void Update()
    {
        // 플레이어 감지 체크
        bool canSeePlayer = CheckPlayerVisibility();
        
        // 애니메이터 파라미터 업데이트
        animator.SetBool("PlayerDetected", canSeePlayer);
        
        // 상태별 처리
        switch (currentState)
        {
            case AIState.Praying:
                HandlePrayingState(canSeePlayer);
                break;
                
            case AIState.MovingToPrayingSpot:
                HandleMovingToPrayingSpotState(canSeePlayer);
                break;
                
            case AIState.Observing:
                HandleObservingState(canSeePlayer);
                break;
                
            case AIState.Chasing:
                HandleChasingState(canSeePlayer);
                break;
                
            case AIState.Attacking:
                HandleAttackingState(canSeePlayer);
                break;
        }
        
        // 공격 범위 체크 및 상태 전환 (공격 상태가 아닐 때만)
        if (player != null && currentState != AIState.Attacking)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            bool inAttackRange = distanceToPlayer <= attackRange;
            
            animator.SetBool("InAttackRange", inAttackRange);
            
            // 추격 중이고 공격 범위에 들어왔다면 공격 상태로 전환
            if (currentState == AIState.Chasing && inAttackRange && canSeePlayer)
            {
                SetState(AIState.Attacking);
                DebugLog("공격 범위 진입 - 공격 상태로 전환");
            }
        }
        
        // 경로 디버깅 (개발 중에만 사용)
        DebugPath();
    }
    
    void HandlePrayingState(bool canSeePlayer)
    {
        // 기도 중 플레이어 발견
        if (canSeePlayer)
        {
            DebugLog("기도 중 플레이어 재발견!");
            
            // 기도 관련 파라미터 리셋
            animator.SetBool("ReturnToPraying", false);
            
            SetState(AIState.Observing);
            StartObservingPlayer();
            lastKnownPlayerPosition = player.position;
        }
    }
    
    void HandleMovingToPrayingSpotState(bool canSeePlayer)
    {
        // 이동 중 플레이어 발견
        if (canSeePlayer)
        {
            SetState(AIState.Observing);
            StartObservingPlayer();
            lastKnownPlayerPosition = player.position;
            return;
        }
        
        // 기도 위치에 도착했는지 확인 (개선된 조건)
        if (prayingSpot != null)
        {
            float distanceToPrayingSpot = Vector3.Distance(transform.position, prayingSpot.position);
            bool closeEnough = distanceToPrayingSpot <= 1.2f;
            bool agentStopped = !agent.pathPending && agent.remainingDistance <= 0.8f;
            bool velocitySlow = agent.velocity.magnitude < 0.2f;
            
            // 도착 조건: 거리가 가깝거나, 에이전트가 멈췄거나, 속도가 느림
            if (closeEnough || (agentStopped && distanceToPrayingSpot <= 2f) || (velocitySlow && distanceToPrayingSpot <= 1.5f))
            {
                DebugLog($"기도 위치에 도착 (거리: {distanceToPrayingSpot:F2}m) - 기도 상태로 전환");
                
                // 정확한 위치로 이동 후 애니메이터 파라미터 변경
                transform.position = new Vector3(prayingSpot.position.x, transform.position.y, prayingSpot.position.z);
                
                animator.SetBool("StartChase", false);
                animator.SetBool("LostPlayer", false);
                animator.SetBool("ReturnToPraying", true);
                
                SetState(AIState.Praying);
            }
        }
    }
    
    void HandleObservingState(bool canSeePlayer)
    {
        if (!canSeePlayer)
        {
            // 플레이어를 놓쳤으면 기도 상태로 돌아가기
            SetState(AIState.Praying);
            return;
        }
        
        HandleObservation();
    }
    
    void HandleChasingState(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            lastKnownPlayerPosition = player.position;
        }
        else
        {
            // 플레이어가 보이지 않으면 즉시 추적 중단
            DebugLog("추격 중 플레이어를 놓침");
            LoseTarget();
            return;
        }
        
        HandleChase();
    }
    
    void HandleAttackingState(bool canSeePlayer)
    {
        // 공격 타이머 증가
        attackTimer += Time.deltaTime;
        
        // 플레이어를 향해 회전
        if (player != null)
        {
            RotateTowards(player.position);
        }
        
        // 공격 중에는 0.5초마다만 거리 체크 (애니메이션 끊김 방지)
        if (attackTimer > 0.5f)
        {
            if (player != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                
                // 플레이어가 공격 범위를 많이 벗어났거나 보이지 않는 경우
                if (distanceToPlayer > attackRange * 1.5f || !canSeePlayer)
                {
                    DebugLog("플레이어가 공격 범위를 벗어남 - 추격 재개");
                    animator.SetBool("InAttackRange", false); // 공격 범위 벗어남 표시
                    SetState(AIState.Chasing);
                    return;
                }
            }
        }
        
        // 공격 쿨다운이 끝났다면 다음 행동 결정
        if (attackTimer >= attackCooldown)
        {
            DebugLog("공격 쿨다운 종료");
            
            // 플레이어가 여전히 보이고 공격 범위에 있다면 계속 공격
            if (canSeePlayer && player != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                if (distanceToPlayer <= attackRange)
                {
                    DebugLog("플레이어가 여전히 공격 범위에 있음 - 공격 계속");
                    attackTimer = 0f; // 타이머 리셋하여 다시 공격
                    return;
                }
                else
                {
                    DebugLog("플레이어가 공격 범위를 벗어남 - 추격 재개");
                    animator.SetBool("InAttackRange", false); // 공격 범위 벗어남 표시
                    SetState(AIState.Chasing);
                    return;
                }
            }
            else
            {
                DebugLog("플레이어를 잃음 - 기도 위치로 복귀");
                animator.SetBool("InAttackRange", false); // 공격 범위 벗어남 표시
                LoseTarget();
                return;
            }
        }
    }
    
    // 플레이어 감지 로직
    bool CheckPlayerVisibility()
    {
        if (player == null) 
        {
            //DebugLog("플레이어 참조가 null입니다");
            return false;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        //DebugLog($"플레이어와의 거리: {distanceToPlayer:F2}m (감지범위: {detectionRange}m)");
        
        // 감지 범위 밖이면 바로 false
        if(distanceToPlayer > detectionRange)
        {
            //DebugLog("플레이어가 감지 범위 밖에 있습니다");
            return false;
        }
            
        // VR 은신 상태 체크
        VRLocomotion playerLocomotion = player.GetComponent<VRLocomotion>();
        if(playerLocomotion != null && playerLocomotion.IsHiding())
        {
            DebugLog("플레이어가 은신 중");
            return false;
        }
        
        // 여러 높이에서 레이캐스트 시도
        Vector3[] rayStartHeights = {
            transform.position + Vector3.up * 1.6f,  // 눈 높이
            transform.position + Vector3.up * 1.0f,  // 가슴 높이
            transform.position + Vector3.up * 0.5f   // 허리 높이
        };
        
        Vector3[] playerTargets = {
            player.position + Vector3.up * 1.7f,     // 플레이어 머리
            player.position + Vector3.up * 1.0f,     // 플레이어 가슴
            player.position + Vector3.up * 0.5f      // 플레이어 허리
        };
        
        for (int i = 0; i < rayStartHeights.Length; i++)
        {
            for (int j = 0; j < playerTargets.Length; j++)
            {
                Vector3 rayStart = rayStartHeights[i];
                Vector3 targetPoint = playerTargets[j];
                Vector3 directionToTarget = (targetPoint - rayStart).normalized;
                float rayDistance = Vector3.Distance(rayStart, targetPoint);
                
                DebugLog($"레이캐스트 {i}-{j}: 시작점 {rayStart}, 목표점 {targetPoint}, 방향 {directionToTarget}, 거리 {rayDistance:F2}");
                
                RaycastHit hit;
                if(Physics.Raycast(rayStart, directionToTarget, out hit, rayDistance + 0.1f))
                {
                    DebugLog($"레이캐스트 {i}-{j} 히트: {hit.transform.name} (거리: {hit.distance:F2}m)");
                    
                    // 플레이어나 플레이어의 자식 오브젝트를 히트했는지 확인
                    if(hit.transform == player || hit.transform.IsChildOf(player))
                    {
                        DebugLog($"플레이어 감지 성공! (레이캐스트 {i}-{j})");
                        // 플레이어 방향으로 회전
                        RotateTowards(player.position);
                        
                        // 디버그용 레이 시각화
                        Debug.DrawRay(rayStart, directionToTarget * rayDistance, Color.green, 0.1f);
                        return true;
                    }
                    else
                    {
                        DebugLog($"시야선이 {hit.transform.name}에 의해 차단됨 (레이캐스트 {i}-{j})");
                        // 디버그용 레이 시각화 (차단됨)
                        Debug.DrawRay(rayStart, directionToTarget * hit.distance, Color.red, 0.1f);
                    }
                }
                else
                {
                    DebugLog($"레이캐스트 {i}-{j}가 아무것도 히트하지 않음");
                    // 디버그용 레이 시각화 (히트 없음)
                    Debug.DrawRay(rayStart, directionToTarget * rayDistance, Color.yellow, 0.1f);
                }
            }
        }
        
        return false;
    }
    
    // 플레이어 관찰 시작
    void StartObservingPlayer()
    {
        DebugLog("플레이어 관찰 시작");
        
        // 플레이어 방향으로 회전
        RotateTowards(player.position);
        
        // 모든 이전 상태 파라미터 리셋
        animator.SetBool("StartChase", false);
        animator.SetBool("ReturnToPraying", false);
        animator.SetBool("LostPlayer", false);
        animator.SetBool("InAttackRange", false);
    }
    
    // 플레이어 관찰 처리
    void HandleObservation()
    {
        // 관찰 시간 증가
        idleTimer += Time.deltaTime;
        
        // 계속해서 플레이어 방향으로 회전
        RotateTowards(player.position);
        
        // 설정된 시간 이상 관찰했다면 추격 시작
        if(idleTimer >= idleObservationTime)
        {
            DebugLog("관찰 완료, 추격 시작");
            
            // 추격 상태로 전환
            SetState(AIState.Chasing);
            
            // 추격을 위한 애니메이터 파라미터 설정
            animator.SetBool("StartChase", true);
            animator.SetBool("ReturnToPraying", false);
            animator.SetBool("LostPlayer", false);
            animator.SetFloat("IdleTimer", idleObservationTime);
            
            // 플레이어 위치로 이동 설정
            agent.SetDestination(player.position);
        }
    }
    
    // 추적 처리 로직
    void HandleChase()
    {
        // 경로 업데이트 주기적 처리
        pathUpdateTimer += Time.deltaTime;
        if(pathUpdateTimer >= pathRecalculationTime)
        {
            pathUpdateTimer = 0f;
            
            // 플레이어가 마지막으로 알려진 위치로 이동
            agent.SetDestination(lastKnownPlayerPosition);
        }
        
        // 플레이어가 시야에서 사라짐
        if(!CheckPlayerVisibility() && Vector3.Distance(transform.position, lastKnownPlayerPosition) < 1f)
        {
            // 마지막 알려진 위치에 도달했지만 플레이어가 보이지 않음
            LoseTarget();
        }
        
        // 목적지에 거의 도달했는지 체크
        if(!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            // 도착했는데 플레이어가 없다면
            if(!CheckPlayerVisibility())
            {
                LoseTarget();
            }
        }
    }
    
    // 추적 대상 상실 처리
    void LoseTarget()
    {
        DebugLog("플레이어를 놓침, 기도 위치로 복귀");
        
        // 기도 위치로 돌아가기
        if(prayingSpot != null)
        {
            // Fast_Run 상태를 유지하면서 기도 위치로 이동
            SetState(AIState.MovingToPrayingSpot);
            
            // NavMeshAgent 설정 최적화
            agent.isStopped = false;
            agent.speed = runSpeed;
            agent.stoppingDistance = 0.1f;
            
            // 목적지 설정 (Y축은 현재 높이 유지)
            Vector3 targetPosition = new Vector3(prayingSpot.position.x, transform.position.y, prayingSpot.position.z);
            agent.SetDestination(targetPosition);
            
            DebugLog($"기도 위치로 이동 설정 완료 - 목표: {targetPosition}, 현재: {transform.position}");
            
            // 애니메이터 파라미터 설정 - Fast_Run을 유지
            animator.SetBool("PlayerDetected", false);
            animator.SetBool("InAttackRange", false);
            // StartChase는 유지하여 Fast_Run 상태 지속
            // LostPlayer와 ReturnToPraying은 기도 위치 도착 시 설정
        }
        else
        {
            // 기도 위치가 없으면 바로 기도 상태로
            animator.SetBool("PlayerDetected", false);
            animator.SetBool("StartChase", false);
            animator.SetBool("InAttackRange", false);
            animator.SetBool("LostPlayer", true);
            animator.SetBool("ReturnToPraying", true);
            SetState(AIState.Praying);
        }
    }
    
    // 특정 위치 방향으로 회전
    void RotateTowards(Vector3 targetPosition)
    {
        // Y축 높이는 무시하고 수평 방향으로만 회전
        targetPosition.y = transform.position.y;
        Vector3 direction = (targetPosition - transform.position).normalized;
        
        if(direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 5f * Time.deltaTime);
        }
    }
    
    // 디버그 로그 출력
    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[CultistAI] {message}");
        }
    }
    
    // 경로 시각화 (디버그용)
    void DebugPath()
    {
        if(agent.hasPath)
        {
            // 현재 경로 시각화
            Vector3[] corners = agent.path.corners;
            for(int i = 0; i < corners.Length - 1; i++)
            {
                Debug.DrawLine(corners[i], corners[i+1], Color.blue);
            }
        }
    }
    
    // NavMesh 사용 가능 여부 확인
    bool IsNavMeshAvailable(Vector3 targetPosition)
    {
        NavMeshHit hit;
        if(NavMesh.SamplePosition(targetPosition, out hit, 1.0f, NavMesh.AllAreas))
        {
            return true;
        }
        return false;
    }
}