using UnityEngine;
using UnityEngine.AI;

public class CultistAI : MonoBehaviour
{
    [Header("AI 설정")]
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float idleObservationTime = 2.5f;
    
    [Header("이동 설정")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 4f;
    public float pathRecalculationTime = 0.5f; // 경로 재계산 주기
    
    [Header("NavMesh 설정")]
    public bool useNavMeshObstacles = true;   // 동적 장애물 사용
    public bool adaptivePathfinding = true;   // 적응형 경로 탐색
    
    [Header("참조")]
    public Transform prayingSpot;
    
    // 내부 변수
    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    
    private float idleTimer = 0f;
    private float pathUpdateTimer = 0f;
    private bool isChasing = false;
    private bool isObserving = false;
    private Vector3 lastKnownPlayerPosition;
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        
        // NavMeshAgent 초기 설정
        agent.speed = walkSpeed;
        agent.stoppingDistance = attackRange * 0.8f;
        
        // 초기 상태 설정
        if(prayingSpot != null && Vector3.Distance(transform.position, prayingSpot.position) > 0.5f)
        {
            agent.SetDestination(prayingSpot.position);
        }
    }
    
    void Update()
    {
        // 플레이어 감지 체크
        bool canSeePlayer = CheckPlayerVisibility();
        
        // 애니메이터 파라미터 업데이트
        animator.SetBool("PlayerDetected", canSeePlayer);
        
        // 플레이어를 처음 발견했을 때
        if(canSeePlayer && !isChasing && !isObserving)
        {
            StartObservingPlayer();
            
            // 마지막 알려진 위치 업데이트
            lastKnownPlayerPosition = player.position;
        }
        
        // 추적 중이고 플레이어가 보인다면 위치 업데이트
        if(isChasing && canSeePlayer)
        {
            lastKnownPlayerPosition = player.position;
        }
        
        // 응시 상태 처리
        if(isObserving)
        {
            HandleObservation();
        }
        
        // 추격 상태 처리
        if(isChasing)
        {
            HandleChase();
        }
        
        // 공격 범위 체크
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        animator.SetBool("InAttackRange", distanceToPlayer <= attackRange);
        
        // 경로 디버깅 (개발 중에만 사용)
        DebugPath();
    }
    
    // 플레이어 감지 로직
    bool CheckPlayerVisibility()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // 감지 범위 밖이면 바로 false
        if(distanceToPlayer > detectionRange)
            return false;
            
        // VR 은신 상태 체크
        VRLocomotion playerLocomotion = player.GetComponent<VRLocomotion>();
        if(playerLocomotion != null && playerLocomotion.IsHiding())
            return false;
            
        // 시야선 체크 (장애물 고려)
        RaycastHit hit;
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        
        if(Physics.Raycast(transform.position + Vector3.up * 1.6f, directionToPlayer, out hit, detectionRange))
        {
            if(hit.transform == player)
            {
                // 플레이어 방향으로 회전
                RotateTowards(player.position);
                return true;
            }
        }
        
        return false;
    }
    
    // 플레이어 관찰 시작
    void StartObservingPlayer()
    {
        // 관찰 상태 시작
        isObserving = true;
        idleTimer = 0f;
        
        // 에이전트 정지
        agent.isStopped = true;
        
        // 플레이어 방향으로 회전
        RotateTowards(player.position);
        
        // 애니메이터 파라미터 설정
        animator.SetBool("StartChase", false);
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
            // 추격 상태로 전환
            isObserving = false;
            isChasing = true;
            
            // 에이전트 재시작 및 속도 조정
            agent.isStopped = false;
            agent.speed = runSpeed;
            
            // 애니메이터 파라미터 설정
            animator.SetBool("StartChase", true);
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
        // 추적 중단
        isChasing = false;
        animator.SetBool("LostPlayer", true);
        
        // 기도 위치로 돌아가기
        if(prayingSpot != null)
        {
            agent.speed = walkSpeed;
            agent.SetDestination(prayingSpot.position);
            
            // 충분히 가까워지면 기도 상태로 전환
            if(Vector3.Distance(transform.position, prayingSpot.position) < 0.5f)
            {
                animator.SetBool("ReturnToPraying", true);
            }
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