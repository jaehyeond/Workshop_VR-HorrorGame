using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// GP 게임의 적 군인 AI 컴포넌트
/// </summary>
public class GPEnemySoldier : MonoBehaviour
{
    [Header("기본 속성")]
    [SerializeField] private string enemyName = "군인";
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float runSpeed = 7.0f;
    [SerializeField] private float searchRadius = 10f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private float detectionTime = 1.0f;
    [SerializeField] private float losePlayerTime = 5.0f;
    
    [Header("참조")]
    [SerializeField] private Transform headTransform;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private AudioSource audioSource;
    
    [Header("사운드")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip[] detectionSounds;
    [SerializeField] private AudioClip[] searchSounds;
    
    [Header("NavMesh 설정")]
    [SerializeField] private float pathUpdateRate = 0.2f; // 경로 업데이트 주기
    [SerializeField] private bool avoidObstacles = true; // 장애물 회피
    [SerializeField] private int avoidancePriority = 50; // 회피 우선순위
    [SerializeField] private int obstacleAvoidanceLevel = 3; // 장애물 회피 수준 (0-3)
    
    [Header("디버그")]
    [SerializeField] private bool showDebugRays = true;
    [SerializeField] private Color viewConeColor = new Color(1, 0, 0, 0.3f);
    
    // 내부 상태
    private Transform playerTransform;
    private float playerDetectionTimer = 0f;
    private float playerLostTimer = 0f;
    private int currentPatrolIndex = 0;
    private Vector3 lastKnownPlayerPos;
    private bool isInitialized = false;
    private float pathUpdateTimer = 0f;
    
    // 상태 머신
    public enum EnemyState { Patrol, Alert, Chase, Search, Returning }
    private EnemyState currentState = EnemyState.Patrol;
    
    private void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        if (isInitialized) return;
        
        // NavMeshAgent 참조 확인
        if (navAgent == null)
        {
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                navAgent = gameObject.AddComponent<NavMeshAgent>();
            }
        }
        
        // 플레이어 참조 가져오기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("Player 태그를 가진 오브젝트를 찾을 수 없습니다.");
        }
        
        // 헤드 트랜스폼 참조 확인
        if (headTransform == null)
        {
            headTransform = transform;
            Debug.LogWarning("헤드 트랜스폼이 할당되지 않았습니다. 기본 트랜스폼을 사용합니다.");
        }
        
        // 애니메이터 참조 확인
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        // NavMeshAgent 최적화 설정
        navAgent.speed = moveSpeed;
        navAgent.angularSpeed = 120;
        navAgent.acceleration = 8;
        navAgent.stoppingDistance = 1f;
        
        // 장애물 회피 설정
        if (avoidObstacles)
        {
            // 회피 수준 설정 (0: 낮음, 3: 높음)
            navAgent.obstacleAvoidanceType = GetObstacleAvoidanceType(obstacleAvoidanceLevel);
        }
        else
        {
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }
        
        navAgent.avoidancePriority = avoidancePriority;
        navAgent.updateRotation = true;
        navAgent.autoRepath = true;
        
        // 첫 번째 순찰 지점으로 이동 시작
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            SetDestination(patrolPoints[0].position);
        }
        
        isInitialized = true;
    }
    
    // 장애물 회피 수준을 ObstacleAvoidanceType으로 변환
    private ObstacleAvoidanceType GetObstacleAvoidanceType(int level)
    {
        switch (level)
        {
            case 0:
                return ObstacleAvoidanceType.NoObstacleAvoidance;
            case 1:
                return ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            case 2:
                return ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            case 3:
            default:
                return ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }
    }
    
    private void Update()
    {
        if (!isInitialized || playerTransform == null)
        {
            Initialize();
            return;
        }
        
        // 현재 상태에 따른 행동 실행
        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrolState();
                break;
                
            case EnemyState.Alert:
                UpdateAlertState();
                break;
                
            case EnemyState.Chase:
                UpdateChaseState();
                break;
                
            case EnemyState.Search:
                UpdateSearchState();
                break;
                
            case EnemyState.Returning:
                UpdateReturningState();
                break;
        }
        
        // 애니메이터 파라미터 업데이트
        UpdateAnimator();
        
        // 항상 플레이어 탐지 시도
        CheckForPlayerDetection();
    }
    
    #region 상태 업데이트 메서드
    
    private void UpdatePatrolState()
    {
        // 목적지에 도착했는지 확인
        if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            // 다음 순찰 지점으로 이동
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                SetDestination(patrolPoints[currentPatrolIndex].position);
            }
            else
            {
                // 순찰 지점이 없으면 제자리에 서있음
                navAgent.isStopped = true;
            }
        }
    }
    
    private void UpdateAlertState()
    {
        // 경계 상태: 의심 위치를 향해 회전
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        directionToPlayer.y = 0;
        
        // 천천히 플레이어 방향으로 회전
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 2f * Time.deltaTime);
        }
        
        // 플레이어가 보이면 추격 상태로 전환
        if (CanSeePlayer())
        {
            playerDetectionTimer += Time.deltaTime;
            
            if (playerDetectionTimer >= detectionTime)
            {
                SetState(EnemyState.Chase);
            }
        }
        else
        {
            playerDetectionTimer = 0;
            // 잠시 기다린 후 수색 상태로 전환
            StartCoroutine(DelayedStateChange(EnemyState.Search, 2f));
        }
    }
    
    private void UpdateChaseState()
    {
        // 추격 상태: 플레이어를 쫓음
        if (playerTransform != null)
        {
            // 플레이어가 은신 중인지 확인
            VRLocomotion playerLocomotion = playerTransform.GetComponent<VRLocomotion>();
            bool playerIsHiding = (playerLocomotion != null && playerLocomotion.IsHiding());
            
            if (!playerIsHiding && CanSeePlayer())
            {
                // 경로 업데이트 타이머 증가
                pathUpdateTimer += Time.deltaTime;
                
                // 일정 주기마다 경로 업데이트
                if (pathUpdateTimer >= pathUpdateRate)
                {
                    pathUpdateTimer = 0f;
                    
                    // 플레이어가 보이고 숨지 않았으면 계속 추격
                    lastKnownPlayerPos = playerTransform.position;
                    SetDestination(lastKnownPlayerPos);
                }
                
                playerLostTimer = 0f;
            }
            else
            {
                // 플레이어가 숨었거나 안 보이면 잃어버린 시간 증가
                playerLostTimer += Time.deltaTime;
                
                if (playerLostTimer >= losePlayerTime)
                {
                    // 일정 시간 후 수색 상태로 전환
                    SetState(EnemyState.Search);
                }
            }
        }
    }
    
    private void UpdateSearchState()
    {
        // 수색 상태: 마지막으로 플레이어를 본 위치 주변을 수색
        if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            // 현재 수색 지점에 도착했으면 새로운 수색 지점 설정
            Vector3 randomOffset = new Vector3(
                Random.Range(-searchRadius, searchRadius),
                0,
                Random.Range(-searchRadius, searchRadius)
            );
            
            Vector3 searchPoint = lastKnownPlayerPos + randomOffset;
            
            // NavMesh 위의 유효한 위치로 변환
            NavMeshHit hit;
            if (NavMesh.SamplePosition(searchPoint, out hit, searchRadius, NavMesh.AllAreas))
            {
                SetDestination(hit.position);
            }
            
            // 일정 확률로 수색 사운드 재생
            if (Random.value < 0.3f && audioSource != null && searchSounds.Length > 0)
            {
                audioSource.PlayOneShot(searchSounds[Random.Range(0, searchSounds.Length)]);
            }
        }
        
        // 일정 시간 수색 후 순찰 상태로 돌아감
        if (Random.value < 0.001f) // 약 10초 후 확률적으로 전환
        {
            SetState(EnemyState.Returning);
        }
        
        // 플레이어가 보이면 다시 추격
        if (CanSeePlayer())
        {
            playerDetectionTimer += Time.deltaTime;
            
            if (playerDetectionTimer >= detectionTime * 0.5f) // 수색 중에는 더 빨리 감지
            {
                SetState(EnemyState.Chase);
            }
        }
        else
        {
            playerDetectionTimer = 0;
        }
    }
    
    private void UpdateReturningState()
    {
        // 순찰 경로로 돌아가는 상태
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            // 가장 가까운 순찰 지점 찾기
            float closestDist = float.MaxValue;
            int closestIndex = 0;
            
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                float dist = Vector3.Distance(transform.position, patrolPoints[i].position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestIndex = i;
                }
            }
            
            // 가장 가까운 순찰 지점으로 이동
            SetDestination(patrolPoints[closestIndex].position);
            
            // 순찰 지점에 도착하면 순찰 상태로 전환
            if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
            {
                currentPatrolIndex = closestIndex;
                SetState(EnemyState.Patrol);
            }
        }
        else
        {
            // 순찰 지점이 없으면 바로 순찰 상태로 전환
            SetState(EnemyState.Patrol);
        }
    }
    
    #endregion
    
    #region 헬퍼 메서드
    
    private void CheckForPlayerDetection()
    {
        // 추격 또는 경계 상태가 아닐 때만 플레이어 감지 시도
        if (currentState != EnemyState.Chase && currentState != EnemyState.Alert)
        {
            if (CanSeePlayer())
            {
                playerDetectionTimer += Time.deltaTime;
                
                if (playerDetectionTimer >= detectionTime)
                {
                    // 플레이어 발견, 경계 상태로 전환
                    SetState(EnemyState.Alert);
                    
                    // 게임 매니저에 알림
                    if (GPGameManager.Instance != null)
                    {
                        GPGameManager.Instance.PlayerDetected();
                    }
                }
            }
            else
            {
                playerDetectionTimer = 0;
            }
        }
    }
    
    private bool CanSeePlayer()
    {
        if (playerTransform == null) return false;
        
        // 플레이어가 은신 중인지 확인
        VRLocomotion playerLocomotion = playerTransform.GetComponent<VRLocomotion>();
        if (playerLocomotion != null && playerLocomotion.IsHiding())
        {
            return false; // 플레이어가 은신 중이면 보이지 않음
        }
        
        Vector3 directionToPlayer = playerTransform.position - headTransform.position;
        float distanceToPlayer = directionToPlayer.magnitude;
        
        // 시야 거리 밖이면 보이지 않음
        if (distanceToPlayer > searchRadius)
        {
            return false;
        }
        
        // 시야각 계산
        float angle = Vector3.Angle(headTransform.forward, directionToPlayer);
        if (angle > viewAngle * 0.5f)
        {
            return false;
        }
        
        // 레이캐스트로 장애물 확인
        RaycastHit hit;
        if (Physics.Raycast(headTransform.position, directionToPlayer.normalized, out hit, distanceToPlayer))
        {
            if (!hit.transform.CompareTag("Player"))
            {
                return false; // 플레이어가 아닌 다른 물체에 가려짐
            }
        }
        
        // 디버그 레이 표시
        if (showDebugRays)
        {
            Debug.DrawRay(headTransform.position, directionToPlayer, Color.green);
        }
        
        return true;
    }
    
    private void SetDestination(Vector3 destination)
    {
        if (navAgent != null && navAgent.isActiveAndEnabled)
        {
            // NavMesh 위의 유효한 위치인지 확인
            NavMeshHit hit;
            if (NavMesh.SamplePosition(destination, out hit, 5f, NavMesh.AllAreas))
            {
                navAgent.isStopped = false;
                navAgent.SetDestination(hit.position);
            }
            else
            {
                Debug.LogWarning("NavMesh에 유효한 위치가 아닙니다: " + destination);
            }
        }
    }
    
    private void SetState(EnemyState newState)
    {
        // 이전 상태에서 나갈 때 처리
        switch (currentState)
        {
            case EnemyState.Chase:
                navAgent.speed = moveSpeed; // 일반 속도로 복귀
                break;
        }
        
        // 새 상태로 진입할 때 처리
        switch (newState)
        {
            case EnemyState.Alert:
                navAgent.isStopped = true;
                // 경계 사운드 재생
                if (audioSource != null && detectionSounds.Length > 0)
                {
                    audioSource.PlayOneShot(detectionSounds[Random.Range(0, detectionSounds.Length)]);
                }
                break;
                
            case EnemyState.Chase:
                navAgent.speed = runSpeed; // 추격 속도로 변경
                // 발견 사운드 재생
                if (audioSource != null && detectionSounds.Length > 0)
                {
                    audioSource.PlayOneShot(detectionSounds[Random.Range(0, detectionSounds.Length)]);
                }
                break;
                
            case EnemyState.Search:
                lastKnownPlayerPos = (playerTransform != null) ? playerTransform.position : transform.position;
                break;
        }
        
        currentState = newState;
    }
    
    private void UpdateAnimator()
    {
        if (animator != null)
        {
            // 이동 속도에 따른 애니메이션 파라미터 설정
            float currentSpeed = navAgent.velocity.magnitude;
            animator.SetFloat("Speed", currentSpeed);
            
            // 상태에 따른 애니메이션 파라미터 설정
            animator.SetBool("IsChasing", currentState == EnemyState.Chase);
            animator.SetBool("IsSearching", currentState == EnemyState.Search || currentState == EnemyState.Alert);
        }
    }
    
    private IEnumerator DelayedStateChange(EnemyState targetState, float delay)
    {
        yield return new WaitForSeconds(delay);
        SetState(targetState);
    }
    
    #endregion
    
    #region 디버그 시각화
    
    private void OnDrawGizmos()
    {
        if (!showDebugRays) return;
        
        // 현재 목적지 표시
        if (navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(navAgent.destination, 0.3f);
            
            // 경로 표시
            Gizmos.color = Color.blue;
            Vector3 previousCorner = transform.position;
            
            if (navAgent.path != null && navAgent.path.corners.Length > 0)
            {
                foreach (Vector3 corner in navAgent.path.corners)
                {
                    Gizmos.DrawLine(previousCorner, corner);
                    previousCorner = corner;
                }
            }
        }
        
        // 시야 시각화
        if (headTransform != null)
        {
            Gizmos.color = viewConeColor;
            
            float halfAngle = viewAngle * 0.5f;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * headTransform.forward;
            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * headTransform.forward;
            
            Gizmos.DrawRay(headTransform.position, rightDir * searchRadius);
            Gizmos.DrawRay(headTransform.position, leftDir * searchRadius);
            
            // 원호 그리기 (간소화된 버전)
            int segments = 20;
            Vector3 prevPos = headTransform.position + rightDir * searchRadius;
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = halfAngle - (i * viewAngle / segments);
                Vector3 dir = Quaternion.Euler(0, angle, 0) * headTransform.forward;
                Vector3 pos = headTransform.position + dir * searchRadius;
                
                Gizmos.DrawLine(prevPos, pos);
                prevPos = pos;
            }
        }
        
        // 상태 표시
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"{enemyName}: {currentState}");
        #endif
    }
    
    #endregion
    
    #region 공개 메서드
    
    /// <summary>
    /// 강제로 특정 위치를 인식하도록 함
    /// </summary>
    public void ForceDetection(Vector3 position)
    {
        lastKnownPlayerPos = position;
        SetState(EnemyState.Alert);
    }
    
    /// <summary>
    /// 현재 적의 상태를 반환
    /// </summary>
    public EnemyState GetCurrentState()
    {
        return currentState;
    }
    
    /// <summary>
    /// 적이 플레이어를 추격 중인지 여부를 반환
    /// </summary>
    public bool IsChasing()
    {
        return currentState == EnemyState.Chase;
    }
    
    #endregion
} 