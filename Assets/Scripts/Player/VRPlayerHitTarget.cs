using UnityEngine;

/// <summary>
/// VR 플레이어의 타격 대상 영역
/// Enemy가 Attack1으로 공격할 때 이 영역에 맞으면 데미지 처리
/// </summary>
public class VRPlayerHitTarget : MonoBehaviour
{
    [Header("타격 영역 설정")]
    public float hitRadius = 0.8f; // 타격 반경
    public LayerMask enemyLayer = -1; // Enemy 레이어
    
    [Header("디버그")]
    public bool showDebugGizmos = true;
    public Color gizmoColor = Color.yellow;
    
    // 참조
    private VRPlayerHealth playerHealth;
    private SphereCollider hitCollider;
    
    void Start()
    {
        InitializeHitTarget();
    }
    
    void InitializeHitTarget()
    {
        // VRPlayerHealth 찾기
        playerHealth = GetComponentInParent<VRPlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<VRPlayerHealth>();
        }
        
        if (playerHealth == null)
        {
            Debug.LogError("[VRPlayerHitTarget] VRPlayerHealth를 찾을 수 없습니다!");
            return;
        }
        
        // SphereCollider 설정
        SetupHitCollider();
        
        // 태그 설정
        if (!gameObject.CompareTag("Player"))
        {
            gameObject.tag = "Player";
        }
        
        Debug.Log($"[VRPlayerHitTarget] 플레이어 타격 영역 초기화 완료 (반경: {hitRadius}m)");
    }
    
    void SetupHitCollider()
    {
        // 기존 Collider 제거
        Collider[] existingColliders = GetComponents<Collider>();
        for (int i = 0; i < existingColliders.Length; i++)
        {
            if (Application.isPlaying)
                Destroy(existingColliders[i]);
            else
                DestroyImmediate(existingColliders[i]);
        }
        
        // SphereCollider 추가
        hitCollider = gameObject.AddComponent<SphereCollider>();
        hitCollider.radius = hitRadius;
        hitCollider.isTrigger = true; // Trigger로 설정
        
        Debug.Log("[VRPlayerHitTarget] SphereCollider 설정 완료");
    }
    
    /// <summary>
    /// Enemy Attack Point가 이 영역에 들어왔을 때
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // Enemy Attack Point 확인
        if (IsEnemyAttackPoint(other))
        {
            Debug.Log($"[VRPlayerHitTarget] Enemy Attack Point 감지: {other.name}");
            
            // EnemyAttackSystem에게 타격 가능 알림
            EnemyAttackSystem attackSystem = other.GetComponentInParent<EnemyAttackSystem>();
            if (attackSystem != null)
            {
                attackSystem.OnPlayerInAttackRange(true);
            }
        }
    }
    
    /// <summary>
    /// Enemy Attack Point가 이 영역에서 나갔을 때
    /// </summary>
    void OnTriggerExit(Collider other)
    {
        // Enemy Attack Point 확인
        if (IsEnemyAttackPoint(other))
        {
            Debug.Log($"[VRPlayerHitTarget] Enemy Attack Point 벗어남: {other.name}");
            
            // EnemyAttackSystem에게 타격 불가 알림
            EnemyAttackSystem attackSystem = other.GetComponentInParent<EnemyAttackSystem>();
            if (attackSystem != null)
            {
                attackSystem.OnPlayerInAttackRange(false);
            }
        }
    }
    
    /// <summary>
    /// Enemy의 Attack Point인지 확인
    /// </summary>
    private bool IsEnemyAttackPoint(Collider other)
    {
        // 레이어 체크
        if (((1 << other.gameObject.layer) & enemyLayer) == 0)
            return false;
        
        // 태그 체크
        if (other.CompareTag("EnemyAttackPoint"))
            return true;
        
        // 이름 체크
        string name = other.name.ToLower();
        if (name.Contains("attack") && name.Contains("point"))
            return true;
        
        // EnemyAttackSystem이 있는 오브젝트의 자식인지 확인
        if (other.GetComponentInParent<EnemyAttackSystem>() != null)
            return true;
        
        return false;
    }
    
    /// <summary>
    /// 직접 데미지 처리 (EnemyAttackSystem에서 호출)
    /// </summary>
    public void TakeDamageFromEnemy(float damage, Vector3 attackPosition)
    {
        if (playerHealth != null)
        {
            Debug.Log($"[VRPlayerHitTarget] ⚔️ Enemy 공격으로 {damage} 데미지!");
            playerHealth.TakeDamage(damage);
        }
    }
    
    /// <summary>
    /// 타격 영역 반경 업데이트
    /// </summary>
    public void UpdateHitRadius(float newRadius)
    {
        hitRadius = newRadius;
        if (hitCollider != null)
        {
            hitCollider.radius = hitRadius;
        }
    }
    
    /// <summary>
    /// 디버그 기즈모
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
        
        // 반투명 구체
        Color transparentColor = gizmoColor;
        transparentColor.a = 0.2f;
        Gizmos.color = transparentColor;
        Gizmos.DrawSphere(transform.position, hitRadius);
    }
    
    void OnDrawGizmosSelected()
    {
        // 선택되었을 때 더 진한 색으로 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
} 