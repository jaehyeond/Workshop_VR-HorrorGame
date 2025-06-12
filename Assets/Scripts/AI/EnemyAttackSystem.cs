using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Enemy Attack1 애니메이션에서 호출되는 공격 시스템
/// 애니메이션 이벤트로 호출됩니다
/// </summary>
public class EnemyAttackSystem : MonoBehaviour
{
    [Header("공격 설정")]
    public float attackDamage = 25f;
    public float attackRange = 5.0f; // 공격 범위 확대
    public LayerMask playerLayer = -1;
    
    [Header("공격 감지")]
    public Transform attackPoint; // 공격 지점 (손 또는 무기)
    public bool useHandAsAttackPoint = true; // 손을 공격 지점으로 사용할지
    
    [Header("이펙트")]
    public AudioClip attackSound;
    public ParticleSystem attackEffect;
    
    [Header("디버그")]
    public bool enableDebug = true;
    public bool skipLineOfSightCheck = false; // VR 환경에서 시야선 체크 건너뛰기
    
    // 참조
    private Animator animator;
    private AudioSource audioSource;
    private CultistAI cultistAI;
    
    // 플레이어 탐지
    private VRPlayerHealth playerHealth;
    private Transform player;
    private VRPlayerHitTarget playerHitTarget;
    
    // 물리적 타격 감지
    private bool playerInAttackRange = false;
    
    // New Input System for testing
    private InputAction forceAttackAction;
    private InputAction immediateAttackAction;
    
    void Start()
    {
        InitializeComponents();
        FindPlayer();
        SetupAttackPoint();
        SetupInputSystem();
    }
    
    void OnEnable()
    {
        forceAttackAction?.Enable();
        immediateAttackAction?.Enable();
    }
    
    void OnDisable()
    {
        forceAttackAction?.Disable();
        immediateAttackAction?.Disable();
    }
    
    void OnDestroy()
    {
        forceAttackAction?.Dispose();
        immediateAttackAction?.Dispose();
    }
    
    void InitializeComponents()
    {
        animator = GetComponent<Animator>();
        cultistAI = GetComponent<CultistAI>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    void FindPlayer()
    {
        // VRPlayerHealth 컴포넌트 찾기
        playerHealth = FindFirstObjectByType<VRPlayerHealth>();
        if (playerHealth != null)
        {
            player = playerHealth.transform;
            Debug.Log($"[EnemyAttackSystem] ✅ 플레이어 찾음: {player.name} (위치: {player.position})");
            
            // VRPlayerHitTarget 찾기
            playerHitTarget = FindFirstObjectByType<VRPlayerHitTarget>();
            if (playerHitTarget != null)
            {
                Debug.Log($"[EnemyAttackSystem] ✅ 플레이어 타격 영역 찾음: {playerHitTarget.name}");
            }
            else
            {
                Debug.LogWarning("[EnemyAttackSystem] ⚠️ VRPlayerHitTarget를 찾을 수 없습니다!");
            }
            
            // 거리 확인
            float distance = Vector3.Distance(transform.position, player.position);
            Debug.Log($"[EnemyAttackSystem] Enemy-Player 거리: {distance:F2}m (공격 범위: {attackRange}m)");
        }
        else
        {
            Debug.LogError("[EnemyAttackSystem] ❌ VRPlayerHealth를 찾을 수 없습니다!");
            
            // 대안으로 Player 태그로 찾기
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                Debug.Log($"[EnemyAttackSystem] Player 태그로 찾음: {playerObj.name}");
                player = playerObj.transform;
            }
        }
    }
    
    void SetupAttackPoint()
    {
        if (attackPoint == null && useHandAsAttackPoint)
        {
            // 손 위치를 자동으로 찾기 (오른손 우선)
            Transform rightHand = FindChildByName(transform, "RightHand");
            if (rightHand == null) rightHand = FindChildByName(transform, "mixamorig:RightHand");
            if (rightHand == null) rightHand = FindChildByName(transform, "R_Hand");
            
            if (rightHand != null)
            {
                attackPoint = rightHand;
                Debug.Log($"[EnemyAttackSystem] 공격 지점을 오른손으로 설정: {rightHand.name}");
            }
            else
            {
                // 손을 찾지 못하면 Enemy 중심점 사용
                attackPoint = transform;
                Debug.LogWarning($"[EnemyAttackSystem] 손을 찾지 못해 중심점 사용: {transform.name}");
            }
        }
    }
    
    /// <summary>
    /// New Input System 설정 (테스트용)
    /// </summary>
    void SetupInputSystem()
    {
        // G키: 애니메이션 이벤트 우회 공격
        forceAttackAction = new InputAction("ForceAttack", InputActionType.Button);
        forceAttackAction.AddBinding("<Keyboard>/g");
        forceAttackAction.performed += OnForceAttackPerformed;
        
        // H키: 즉시 공격 (거리 무시)
        immediateAttackAction = new InputAction("ImmediateAttack", InputActionType.Button);
        immediateAttackAction.AddBinding("<Keyboard>/h");
        immediateAttackAction.performed += OnImmediateAttackPerformed;
        
        forceAttackAction.Enable();
        immediateAttackAction.Enable();
        
        Debug.Log($"[EnemyAttackSystem] ✅ New Input System 설정 완료! G키, H키 활성화 ({gameObject.name})");
    }
    
    /// <summary>
    /// G키 콜백: 실제 Enemy AI 공격 트리거 (거리 체크 포함)
    /// </summary>
    void OnForceAttackPerformed(InputAction.CallbackContext context)
    {
        Debug.Log($"[EnemyAttackSystem] 🔥 G키로 실제 Enemy AI 공격 트리거! ({gameObject.name})");
        
        // 실제 게임에서 사용할 수 있는 방식: Enemy AI에게 공격 명령
        TriggerEnemyAttack();
    }
    
    /// <summary>
    /// 실제 Enemy AI 공격 트리거 (기존 로직과 호환)
    /// </summary>
    public void TriggerEnemyAttack()
    {
        Debug.Log($"[EnemyAttackSystem] 🎯 실제 Enemy AI 공격 트리거 시작!");
        
        // 1. CultistAI StateMachine을 통한 공격 상태 전환
        if (cultistAI != null)
        {
            Debug.Log($"[EnemyAttackSystem] ✅ CultistAI 발견 - 공격 상태로 강제 전환");
            
            // StateMachine 가져오기
            var stateMachine = cultistAI.GetComponent<CultistStateMachine>();
            if (stateMachine != null)
            {
                Debug.Log($"[EnemyAttackSystem] StateMachine을 통한 공격 상태 전환");
                stateMachine.SetState(CultistStateMachine.AIState.Attacking);
            }
            
            // 플레이어 방향으로 회전
            if (player != null)
            {
                cultistAI.RotateTowards(player.position);
            }
        }
        
        // 2. Animator 트리거 (Attack1 애니메이션 실행)
        if (animator != null)
        {
            Debug.Log($"[EnemyAttackSystem] Animator Attack1 트리거 실행");
            animator.SetBool("InAttackRange", true);
            animator.SetTrigger("Attack1");
        }
        
        // 3. 거리 체크 및 공격 실행
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            Debug.Log($"[EnemyAttackSystem] 현재 거리: {distance:F2}m, 공격 범위: {attackRange}m");
            
            if (distance <= attackRange)
            {
                Debug.Log($"[EnemyAttackSystem] ✅ 공격 범위 내 - 즉시 공격!");
                // 0.5초 후 실제 타격 (애니메이션 타이밍 맞춤)
                Invoke(nameof(DelayedAttack), 0.5f);
            }
            else
            {
                Debug.Log($"[EnemyAttackSystem] 🏃 공격 범위 밖 - Enemy AI가 접근 중...");
                // CultistAI가 자동으로 접근하도록 Chasing 상태로 전환
                var stateMachine = cultistAI?.GetComponent<CultistStateMachine>();
                if (stateMachine != null)
                {
                    stateMachine.SetState(CultistStateMachine.AIState.Chasing);
                    Debug.Log($"[EnemyAttackSystem] Chasing 상태로 전환 - AI가 자동 접근");
                }
            }
        }
    }
    
    /// <summary>
    /// 지연된 공격 실행 (애니메이션 타이밍 맞춤)
    /// </summary>
    private void DelayedAttack()
    {
        Debug.Log($"[EnemyAttackSystem] ⚔️ 지연된 공격 실행!");
        OnAttack1Hit();
    }
    

    
    /// <summary>
    /// H키 콜백: 즉시 공격 (거리 무시)
    /// </summary>
    void OnImmediateAttackPerformed(InputAction.CallbackContext context)
    {
        Debug.Log($"[EnemyAttackSystem] 🔥 H키로 즉시 공격! ({gameObject.name})");
        if (playerHealth != null)
        {
            Debug.Log($"[EnemyAttackSystem] ✅ 플레이어에게 즉시 데미지! (데미지: {attackDamage})");
            playerHealth.TakeDamage(attackDamage);
        }
        else
        {
            Debug.LogError($"[EnemyAttackSystem] ❌ VRPlayerHealth를 찾을 수 없음! ({gameObject.name})");
        }
    }
    
    /// <summary>
    /// 애니메이션 이벤트에서 호출되는 공격 함수
    /// Attack1 애니메이션에서 실제 타격 순간에 호출
    /// </summary>
    public void OnAttack1Hit()
    {
        Debug.Log($"[EnemyAttackSystem] 🗡️ Attack1 타격 실행! (Enemy: {gameObject.name})");
        
        // 즉시 데미지 처리 (물리적 감지 상관없이)
        if (playerHealth != null)
        {
            Vector3 attackPosition = attackPoint != null ? attackPoint.position : transform.position;
            float distanceToPlayer = Vector3.Distance(attackPosition, player.position);
            
            Debug.Log($"[EnemyAttackSystem] 공격 거리: {distanceToPlayer:F2}m (최대: {attackRange}m)");
            
            // 거리 체크만 하고 즉시 데미지
            if (distanceToPlayer <= attackRange)
            {
                Debug.Log("[EnemyAttackSystem] ✅ 즉시 데미지 처리!");
                playerHealth.TakeDamage(attackDamage);
                PlayAttackEffects(attackPosition);
            }
            else
            {
                Debug.Log($"[EnemyAttackSystem] ❌ 공격 범위 밖: {distanceToPlayer:F2}m > {attackRange}m");
            }
        }
        else
        {
            Debug.LogError("[EnemyAttackSystem] ❌ VRPlayerHealth를 찾을 수 없음!");
        }
    }
    
    /// <summary>
    /// VRPlayerHitTarget에서 호출되는 콜백 (플레이어가 공격 범위에 들어왔을 때)
    /// </summary>
    public void OnPlayerInAttackRange(bool inRange)
    {
        playerInAttackRange = inRange;
        Debug.Log($"[EnemyAttackSystem] 플레이어 물리적 타격 범위: {(inRange ? "IN" : "OUT")}");
    }
    
    /// <summary>
    /// 물리적 타격 감지 기반 공격
    /// </summary>
    private void PerformPhysicalAttack()
    {
        if (playerHitTarget == null || playerHealth == null)
        {
            Debug.LogWarning("[EnemyAttackSystem] 물리적 타격 실패 - 대상 없음");
            return;
        }
        
        Vector3 attackPosition = attackPoint != null ? attackPoint.position : transform.position;
        
        // 직접 타격 처리
        playerHitTarget.TakeDamageFromEnemy(attackDamage, attackPosition);
        
        Debug.Log($"[EnemyAttackSystem] ✅ 물리적 타격 성공! 데미지: {attackDamage}");
        
        // 공격 이펙트
        PlayAttackEffects(attackPosition);
    }
    
    /// <summary>
    /// 실제 공격 수행
    /// </summary>
    private void PerformAttack()
    {
        if (player == null || playerHealth == null)
        {
            if (enableDebug) Debug.Log("[EnemyAttackSystem] 플레이어를 찾을 수 없어 공격 취소");
            return;
        }
        
        Vector3 attackPosition = attackPoint != null ? attackPoint.position : transform.position;
        float distanceToPlayer = Vector3.Distance(attackPosition, player.position);
        
        Debug.Log($"[EnemyAttackSystem] 공격 거리 체크: {distanceToPlayer:F2}m (최대: {attackRange}m)");
        Debug.Log($"[EnemyAttackSystem] 플레이어 위치: {player.position}, Enemy 위치: {attackPosition}");
        
        // 거리 체크
        if (distanceToPlayer <= attackRange)
        {
            // 시야선 체크 (VR 환경에서는 선택적)
            if (skipLineOfSightCheck || CanHitPlayer(attackPosition))
            {
                // 플레이어에게 데미지!
                playerHealth.TakeDamage(attackDamage);
                
                if (enableDebug)
                {
                    string method = skipLineOfSightCheck ? "(시야선 체크 건너뜀)" : "(시야선 체크 통과)";
                    Debug.Log($"[EnemyAttackSystem] ✅ 플레이어 타격 성공! 데미지: {attackDamage} {method}");
                }
                
                // 공격 이펙트
                PlayAttackEffects(attackPosition);
            }
            else
            {
                if (enableDebug) Debug.Log("[EnemyAttackSystem] 시야선이 막혀 공격 실패");
            }
        }
        else
        {
            if (enableDebug) Debug.Log("[EnemyAttackSystem] 공격 범위 밖이므로 공격 실패");
        }
    }
    
    /// <summary>
    /// 플레이어를 타격할 수 있는지 시야선 체크 (VR 환경 최적화)
    /// </summary>
    private bool CanHitPlayer(Vector3 attackPosition)
    {
        // VR 환경에서는 시야선 체크를 더 관대하게 처리
        Vector3 directionToPlayer = (player.position - attackPosition).normalized;
        
        // 다중 레이캐스트로 VR 플레이어의 복잡한 구조 대응
        Vector3[] rayOffsets = {
            Vector3.zero,                    // 중앙
            Vector3.up * 0.5f,              // 위쪽 (머리)
            Vector3.down * 0.5f,            // 아래쪽 (몸통)
            Vector3.left * 0.3f,            // 왼쪽
            Vector3.right * 0.3f            // 오른쪽
        };
        
        foreach (Vector3 offset in rayOffsets)
        {
            Vector3 rayStart = attackPosition + offset;
            Vector3 rayTarget = player.position + offset;
            Vector3 rayDirection = (rayTarget - rayStart).normalized;
            float rayDistance = Vector3.Distance(rayStart, rayTarget);
            
            RaycastHit hit;
            if (Physics.Raycast(rayStart, rayDirection, out hit, rayDistance + 0.5f))
            {
                // 플레이어 관련 오브젝트를 히트했다면 성공
                if (IsPlayerRelated(hit.transform))
                {
                    if (enableDebug)
                    {
                        Debug.Log($"[EnemyAttackSystem] ✅ 시야선 확보! 히트: {hit.transform.name}");
                        Debug.DrawRay(rayStart, rayDirection * hit.distance, Color.green, 1f);
                    }
                    return true;
                }
            }
            else
            {
                // 아무것도 히트하지 않았으면 성공 (장애물 없음)
                if (enableDebug)
                {
                    Debug.Log($"[EnemyAttackSystem] ✅ 시야선 확보! (장애물 없음)");
                    Debug.DrawRay(rayStart, rayDirection * rayDistance, Color.green, 1f);
                }
                return true;
            }
        }
        
        if (enableDebug)
        {
            Debug.Log($"[EnemyAttackSystem] ❌ 모든 시야선이 막힘");
        }
        
        // 모든 레이캐스트가 실패했어도 거리가 가까우면 성공 처리 (VR 환경 보정)
        float distanceToPlayer = Vector3.Distance(attackPosition, player.position);
        if (distanceToPlayer <= attackRange * 0.8f) // 공격 범위의 80% 이내면 성공
        {
            if (enableDebug)
            {
                Debug.Log($"[EnemyAttackSystem] ✅ 거리 보정으로 공격 성공! 거리: {distanceToPlayer:F2}m");
            }
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 플레이어 관련 오브젝트인지 확인
    /// </summary>
    private bool IsPlayerRelated(Transform target)
    {
        if (target == null) return false;
        
        // 직접 플레이어 오브젝트
        if (target == player) return true;
        
        // 플레이어의 자식 오브젝트
        if (target.IsChildOf(player)) return true;
        
        // 플레이어의 부모 오브젝트 (VR 카메라 리그 등)
        if (player.IsChildOf(target)) return true;
        
        // 태그 기반 확인
        if (target.CompareTag("Player")) return true;
        
        // 이름 기반 확인 (VR 관련 오브젝트들)
        string targetName = target.name.ToLower();
        if (targetName.Contains("player") || 
            targetName.Contains("camera") || 
            targetName.Contains("head") || 
            targetName.Contains("hand") ||
            targetName.Contains("ovr") ||
            targetName.Contains("vr"))
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 공격 이펙트 재생
    /// </summary>
    private void PlayAttackEffects(Vector3 position)
    {
        // 사운드 재생
        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound, 0.7f);
        }
        
        // 파티클 이펙트
        if (attackEffect != null)
        {
            attackEffect.transform.position = position;
            attackEffect.Play();
        }
    }
    
    /// <summary>
    /// 재귀적으로 자식에서 이름으로 Transform 찾기
    /// </summary>
    private Transform FindChildByName(Transform parent, string name)
    {
        if (parent.name.Contains(name))
            return parent;
            
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name))
                return child;
                
            Transform found = FindChildByName(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
    
    /// <summary>
    /// 디버그 기즈모 (공격 범위 표시)
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Vector3 center = attackPoint != null ? attackPoint.position : transform.position;
        
        // 공격 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, attackRange);
        
        // 플레이어와의 연결선
        if (Application.isPlaying && player != null)
        {
            float distance = Vector3.Distance(center, player.position);
            Gizmos.color = distance <= attackRange ? Color.green : Color.red;
            Gizmos.DrawLine(center, player.position);
            
            // 거리 텍스트 (Scene 뷰에서만 보임)
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(center + Vector3.up * 0.5f, $"거리: {distance:F1}m");
            #endif
        }
    }
    
    /// <summary>
    /// 디버그용 공개 함수들
    /// </summary>
    [System.Serializable]
    public class DebugFunctions
    {
        [Header("테스트 버튼들")]
        public bool testAttack;
        
        public void TestAttack(EnemyAttackSystem attackSystem)
        {
            if (testAttack)
            {
                testAttack = false;
                attackSystem.OnAttack1Hit();
            }
        }
    }
    
    public DebugFunctions debugFunctions = new DebugFunctions();
    
    void Update()
    {
        // 디버그 테스트
        if (Application.isPlaying)
        {
            debugFunctions.TestAttack(this);
        }
    }
    
    /// <summary>
    /// 거리 무시하고 강제로 공격 테스트
    /// </summary>
    public void ForceAttackTest()
    {
        if (playerHealth == null)
        {
            Debug.LogError("[EnemyAttackSystem] ❌ VRPlayerHealth를 찾을 수 없습니다!");
            return;
        }
        
        Debug.Log("[EnemyAttackSystem] 🔥 강제 공격 실행!");
        playerHealth.TakeDamage(attackDamage);
    }
    
    /// <summary>
    /// 실시간 Enemy-Player 상호작용 상태 표시 (디버그용)
    /// </summary>
    void OnGUI()
    {
        if (!enableDebug || !Application.isPlaying) return;
        
        // Enemy 정보 표시
        GUILayout.BeginArea(new Rect(10, 200, 400, 200));
        GUILayout.Label($"=== {gameObject.name} 상태 ===");
        GUILayout.Label($"VRPlayerHealth: {(playerHealth != null ? "✅ 연결됨" : "❌ 없음")}");
        
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            GUILayout.Label($"플레이어 거리: {distance:F2}m");
            GUILayout.Label($"공격 범위: {attackRange}m");
            GUILayout.Label($"공격 가능: {(distance <= attackRange ? "✅ YES" : "❌ NO")}");
        }
        else
        {
            GUILayout.Label("플레이어: ❌ 찾을 수 없음");
        }
        
        GUILayout.Space(10);
        GUILayout.Label("테스트 키:");
        GUILayout.Label("G키 = 애니메이션 이벤트 우회 공격");
        GUILayout.Label("H키 = 즉시 공격 (거리 무시)");
        GUILayout.Label("T키 = VR 피격 효과 테스트");
        
        if (GUILayout.Button("즉시 공격 테스트"))
        {
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
        
        GUILayout.EndArea();
    }
} 