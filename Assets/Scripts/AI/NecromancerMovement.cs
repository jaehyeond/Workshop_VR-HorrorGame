using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 네크로맨서 이동 시스템
/// NavMeshAgent를 사용한 이동과 회전만 담당
/// </summary>
public class NecromancerMovement : MonoBehaviour
{
    [Header("=== 이동 설정 ===")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float rotationSpeed = 5f;
    public float stoppingDistance = 2f;
    
    [Header("=== NavMesh 설정 ===")]
    public float navMeshSampleDistance = 1f;
    public LayerMask navMeshLayerMask = -1;
    
    [Header("=== 디버그 ===")]
    public bool enableMovementLogs = true;
    public bool showPathGizmos = true;
    
    // 컴포넌트 참조
    private NecromancerBoss bossController;
    private NavMeshAgent navAgent;
    private Animator animator;
    
    // 이동 상태
    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool isChasing = false;
    private float currentSpeed = 0f;
    
    // 경로 추적
    private Vector3 lastPlayerPosition;
    private float pathUpdateInterval = 0.2f;
    private float lastPathUpdateTime = 0f;
    
    public bool IsMoving => isMoving;
    public bool IsChasing => isChasing;
    public float CurrentSpeed => currentSpeed;
    
    public void Initialize(NecromancerBoss boss)
    {
        bossController = boss;
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // NavMeshAgent가 없으면 추가
        if (navAgent == null)
        {
            navAgent = gameObject.AddComponent<NavMeshAgent>();
        }
        
        // NavMeshAgent 설정
        SetupNavMeshAgent();
        
        MovementLog("이동 시스템 초기화 완료");
    }
    
    void SetupNavMeshAgent()
    {
        if (navAgent == null) return;
        
        navAgent.speed = walkSpeed;
        navAgent.angularSpeed = rotationSpeed * 60f; // 도/초를 라디안/초로 변환
        navAgent.acceleration = 8f;
        navAgent.stoppingDistance = stoppingDistance;
        navAgent.autoBraking = true;
        navAgent.autoRepath = true;
        
        // 장애물 회피 설정
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        navAgent.radius = 0.5f;
        navAgent.height = 2f;
        
        MovementLog("NavMeshAgent 설정 완료");
    }
    
    void Update()
    {
        if (bossController == null || bossController.IsDead) return;
        
        UpdateMovementState();
        UpdateAnimationParameters();
    }
    
    void UpdateMovementState()
    {
        if (navAgent == null) return;
        
        // 현재 속도 계산
        currentSpeed = navAgent.velocity.magnitude;
        isMoving = currentSpeed > 0.1f;
        
        // 목적지 도달 체크
        if (navAgent.hasPath && navAgent.remainingDistance < stoppingDistance)
        {
            if (isChasing)
            {
                // 추격 중이면 계속 플레이어 위치 업데이트
                UpdateChaseTarget();
            }
            else
            {
                // 일반 이동이면 정지
                StopMovement();
            }
        }
    }
    
    void UpdateAnimationParameters()
    {
        if (animator == null) return;
        
        // 애니메이션 파라미터 업데이트 (필요시)
        // animator.SetFloat("Speed", currentSpeed);
        // animator.SetBool("IsMoving", isMoving);
    }
    
    #region 이동 명령
    
    public void ChasePlayer(Vector3 playerPosition)
    {
        if (!CanMove()) return;
        
        isChasing = true;
        
        // 경로 업데이트 최적화 (너무 자주 업데이트하지 않음)
        if (Time.time - lastPathUpdateTime >= pathUpdateInterval ||
            Vector3.Distance(lastPlayerPosition, playerPosition) > 2f)
        {
            SetDestination(playerPosition);
            lastPlayerPosition = playerPosition;
            lastPathUpdateTime = Time.time;
        }
        
        // 추격 시 빠른 속도
        SetMovementSpeed(runSpeed);
    }
    
    public void MoveTo(Vector3 destination)
    {
        if (!CanMove()) return;
        
        isChasing = false;
        SetDestination(destination);
        SetMovementSpeed(walkSpeed);
        
        MovementLog($"목적지로 이동: {destination}");
    }
    
    public void StopMovement()
    {
        if (navAgent == null) return;
        
        isMoving = false;
        isChasing = false;
        navAgent.ResetPath();
        
        MovementLog("이동 정지");
    }
    
    public void SetDestination(Vector3 destination)
    {
        if (!CanMove()) return;
        
        // NavMesh 위의 가장 가까운 점 찾기
        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, navMeshSampleDistance, navMeshLayerMask))
        {
            targetPosition = hit.position;
            navAgent.SetDestination(targetPosition);
            isMoving = true;
            
            MovementLog($"경로 설정: {targetPosition}");
        }
        else
        {
            MovementLog($"NavMesh에서 유효한 위치를 찾을 수 없음: {destination}");
        }
    }
    
    #endregion
    
    #region 이동 제어
    
    public void SetMovementSpeed(float speed)
    {
        if (navAgent != null)
        {
            navAgent.speed = speed;
        }
    }
    
    public void EnableMovement()
    {
        if (navAgent != null)
        {
            navAgent.enabled = true;
        }
    }
    
    public void DisableMovement()
    {
        if (navAgent != null)
        {
            navAgent.enabled = false;
        }
        
        isMoving = false;
        isChasing = false;
    }
    
    public bool CanMove()
    {
        return navAgent != null && 
               navAgent.enabled && 
               navAgent.isOnNavMesh &&
               bossController != null && 
               !bossController.IsDead;
    }
    
    #endregion
    
    #region 회전 제어
    
    public void LookAt(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Y축 회전만
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
    
    public void LookAtPlayer()
    {
        if (bossController != null && bossController.Player != null)
        {
            LookAt(bossController.Player.position);
        }
    }
    
    #endregion
    
    #region 경로 정보
    
    public float GetDistanceToDestination()
    {
        if (navAgent == null || !navAgent.hasPath) return float.MaxValue;
        return navAgent.remainingDistance;
    }
    
    public bool HasReachedDestination()
    {
        return GetDistanceToDestination() <= stoppingDistance;
    }
    
    public Vector3[] GetCurrentPath()
    {
        if (navAgent == null || !navAgent.hasPath) return new Vector3[0];
        return navAgent.path.corners;
    }
    
    #endregion
    
    #region 추격 최적화
    
    private void UpdateChaseTarget()
    {
        if (bossController == null || bossController.Player == null) return;
        
        Vector3 playerPosition = bossController.Player.position;
        
        // 플레이어가 많이 움직였으면 경로 재계산
        if (Vector3.Distance(lastPlayerPosition, playerPosition) > 1f)
        {
            SetDestination(playerPosition);
            lastPlayerPosition = playerPosition;
        }
    }
    
    #endregion
    
    #region 디버그 및 유틸리티
    
    private void MovementLog(string message)
    {
        if (enableMovementLogs)
            Debug.Log($"[NecromancerMovement] {message}");
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showPathGizmos) return;
        
        // 목적지 표시
        if (targetPosition != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition, 0.5f);
            Gizmos.DrawLine(transform.position, targetPosition);
        }
        
        // NavMesh 경로 표시
        if (navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.yellow;
            Vector3[] corners = navAgent.path.corners;
            
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
                Gizmos.DrawWireSphere(corners[i], 0.2f);
            }
            
            if (corners.Length > 0)
            {
                Gizmos.DrawWireSphere(corners[corners.Length - 1], 0.2f);
            }
        }
        
        // 정지 거리 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }
    
    #endregion
    
    void OnDestroy()
    {
        // 정리 작업
        if (navAgent != null)
        {
            navAgent.ResetPath();
        }
    }
} 