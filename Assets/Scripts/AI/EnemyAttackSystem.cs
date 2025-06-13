using UnityEngine;

/// <summary>
/// Enemy Attack1 애니메이션에서 호출되는 공격 시스템
/// 애니메이션 이벤트로 호출됩니다
/// </summary>
public class EnemyAttackSystem : MonoBehaviour
{
    [Header("공격 설정")]
    public float attackDamage = 25f;
    public float attackRange = 5.0f;
    public LayerMask playerLayer = -1;
    
    [Header("공격 감지")]
    public Transform attackPoint;
    public bool useHandAsAttackPoint = true;
    
    [Header("이펙트")]
    public AudioClip attackSound;
    public ParticleSystem attackEffect;
    
    [Header("디버그")]
    public bool enableDebug = true;
    public bool skipLineOfSightCheck = false;
    
    // 참조
    private Animator animator;
    private AudioSource audioSource;
    private CultistAI cultistAI;
    
    // 플레이어 탐지
    private VRPlayerHealth playerHealth;
    private Transform player;
    private VRPlayerHitTarget playerHitTarget;
    
    void Start()
    {
        InitializeComponents();
        FindPlayer();
        SetupAttackPoint();
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
        playerHealth = FindAnyObjectByType<VRPlayerHealth>();
        if (playerHealth != null)
        {
            player = playerHealth.transform;
            Debug.Log($"[EnemyAttackSystem] 플레이어 찾음: {player.name} (위치: {player.position})");
            
            // VRPlayerHitTarget 찾기
            playerHitTarget = FindAnyObjectByType<VRPlayerHitTarget>();
            if (playerHitTarget != null)
            {
                Debug.Log($"[EnemyAttackSystem] 플레이어 타격 영역 찾음: {playerHitTarget.name}");
            }
            else
            {
                Debug.LogWarning("[EnemyAttackSystem] VRPlayerHitTarget를 찾을 수 없습니다!");
            }
            
            // 거리 확인
            float distance = Vector3.Distance(transform.position, player.position);
            Debug.Log($"[EnemyAttackSystem] Enemy-Player 거리: {distance:F2}m (공격 범위: {attackRange}m)");
        }
        else
        {
            Debug.LogError("[EnemyAttackSystem] VRPlayerHealth를 찾을 수 없습니다!");
            
            // 대안으로 Player 태그로 찾기
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                
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
    /// 애니메이션 이벤트에서 호출되는 공격 함수
    /// Attack1 애니메이션에서 실제 타격 순간에 호출
    /// </summary>
    public void OnAttack1Hit()
    {
        ProcessInstantAttack();
    }
    
    /// <summary>
    /// 공격 완료 시 호출되는 함수 (Animation Event용)
    /// BossAttackPattern의 OnAttackComplete를 호출
    /// </summary>
    public void OnAttackComplete()
    {
        Debug.Log($"[EnemyAttackSystem] OnAttackComplete 호출됨!");
        
        // BossAttackPattern 컴포넌트 찾아서 호출
        BossAttackPattern bossPattern = GetComponent<BossAttackPattern>();
        if (bossPattern != null)
        {
            bossPattern.OnAttackComplete();
        }
        else
        {
            Debug.LogWarning($"[EnemyAttackSystem] BossAttackPattern 컴포넌트를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 즉시 공격 처리
    /// </summary>
    private void ProcessInstantAttack()
    {
        if (playerHealth == null)
        {
            return;
        }
        
        Vector3 attackPosition = attackPoint != null ? attackPoint.position : transform.position;
        float distanceToPlayer = Vector3.Distance(attackPosition, player.position);
        
        // 거리 체크 후 즉시 데미지
        if (distanceToPlayer <= attackRange)
        {
            // 즉시 데미지 적용
            playerHealth.TakeDamage(attackDamage);
            
            // 이펙트 재생
            PlayAttackEffects(attackPosition);
        }
    }
    
    /// <summary>
    /// VRPlayerHitTarget에서 호출되는 콜백
    /// </summary>
    public void OnPlayerInAttackRange(bool inRange)
    {
        // 물리적 타격 범위 처리 (필요시 추가 로직)
    }
    
    /// <summary>
    /// 플레이어를 타격할 수 있는지 시야선 체크
    /// </summary>
    private bool CanHitPlayer(Vector3 attackPosition)
    {
        Vector3 directionToPlayer = (player.position - attackPosition).normalized;
        
        Vector3[] rayOffsets = {
            Vector3.zero,
            Vector3.up * 0.5f,
            Vector3.down * 0.5f,
            Vector3.left * 0.3f,
            Vector3.right * 0.3f
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
                            if (IsPlayerRelated(hit.transform))
            {
                if (enableDebug)
                {
                    Debug.DrawRay(rayStart, rayDirection * hit.distance, Color.green, 1f);
                }
                return true;
            }
        }
        else
        {
            if (enableDebug)
            {
                Debug.DrawRay(rayStart, rayDirection * rayDistance, Color.green, 1f);
            }
            return true;
        }
    }
    
    // 거리 보정
    float distanceToPlayer = Vector3.Distance(attackPosition, player.position);
    if (distanceToPlayer <= attackRange * 0.8f)
    {
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
        if (target == player) return true;
        if (target.IsChildOf(player)) return true;
        if (player.IsChildOf(target)) return true;
        if (target.CompareTag("Player")) return true;
        
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
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(center + Vector3.up * 0.5f, $"거리: {distance:F1}m");
            #endif
        }
    }
} 