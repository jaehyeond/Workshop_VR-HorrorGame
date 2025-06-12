using UnityEngine;

/// <summary>
/// Enemy Attack1 애니메이션에서 호출되는 공격 시스템
/// 애니메이션 이벤트로 호출됩니다
/// </summary>
public class EnemyAttackSystem : MonoBehaviour
{
    [Header("공격 설정")]
    public float attackDamage = 25f;
    public float attackRange = 2.5f;
    public LayerMask playerLayer = -1;
    
    [Header("공격 감지")]
    public Transform attackPoint; // 공격 지점 (손 또는 무기)
    public bool useHandAsAttackPoint = true; // 손을 공격 지점으로 사용할지
    
    [Header("이펙트")]
    public AudioClip attackSound;
    public ParticleSystem attackEffect;
    
    [Header("디버그")]
    public bool enableDebug = true;
    
    // 참조
    private Animator animator;
    private AudioSource audioSource;
    private CultistAI cultistAI;
    
    // 플레이어 탐지
    private VRPlayerHealth playerHealth;
    private Transform player;
    
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
        playerHealth = FindFirstObjectByType<VRPlayerHealth>();
        if (playerHealth != null)
        {
            player = playerHealth.transform;
            Debug.Log($"[EnemyAttackSystem] 플레이어 찾음: {player.name}");
        }
        else
        {
            Debug.LogWarning("[EnemyAttackSystem] VRPlayerHealth를 찾을 수 없습니다!");
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
        if (enableDebug) Debug.Log($"[EnemyAttackSystem] 🗡️ Attack1 타격 실행!");
        
        PerformAttack();
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
        
        if (enableDebug)
        {
            Debug.Log($"[EnemyAttackSystem] 공격 거리 체크: {distanceToPlayer:F2}m (최대: {attackRange}m)");
        }
        
        // 거리 체크
        if (distanceToPlayer <= attackRange)
        {
            // 시야선 체크 (장애물 확인)
            if (CanHitPlayer(attackPosition))
            {
                // 플레이어에게 데미지!
                playerHealth.TakeDamage(attackDamage);
                
                if (enableDebug)
                {
                    Debug.Log($"[EnemyAttackSystem] ✅ 플레이어 타격 성공! 데미지: {attackDamage}");
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
    /// 플레이어를 타격할 수 있는지 시야선 체크
    /// </summary>
    private bool CanHitPlayer(Vector3 attackPosition)
    {
        Vector3 directionToPlayer = (player.position - attackPosition).normalized;
        
        RaycastHit hit;
        if (Physics.Raycast(attackPosition, directionToPlayer, out hit, attackRange))
        {
            // 플레이어를 직접 히트했거나 플레이어의 자식 오브젝트를 히트
            if (hit.transform == player || hit.transform.IsChildOf(player))
            {
                return true;
            }
            
            if (enableDebug)
            {
                Debug.Log($"[EnemyAttackSystem] 시야선이 {hit.transform.name}에 막힘");
                Debug.DrawRay(attackPosition, directionToPlayer * hit.distance, Color.red, 1f);
            }
            return false;
        }
        
        // 아무것도 히트하지 않았으면 성공
        return true;
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
} 