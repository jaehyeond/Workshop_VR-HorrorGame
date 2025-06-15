using UnityEngine;

/// <summary>
/// 딸 구출 트리거 - 플레이어가 딸에게 접근하면 GameProgressManager에 알림
/// </summary>
public class DaughterRescueTrigger : MonoBehaviour
{
    [Header("=== 구출 설정 ===")]
    [SerializeField] private float triggerRadius = 2f;
    [SerializeField] private bool requiresBossDefeated = true;
    [SerializeField] private string playerTag = "Player";
    
    [Header("=== 시각적 피드백 ===")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.cyan;
    [SerializeField] private GameObject rescueEffect;
    
    [Header("=== 디버그 ===")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 상태
    private bool hasBeenRescued = false;
    private Collider triggerCollider;
    
    // 이벤트
    public System.Action OnDaughterRescued;
    
    void Start()
    {
        SetupTrigger();
        CheckGameState();
    }
    
    void SetupTrigger()
    {
        // Collider 설정
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<SphereCollider>();
        }
        
        triggerCollider.isTrigger = true;
        
        // SphereCollider인 경우 반지름 설정
        if (triggerCollider is SphereCollider sphereCollider)
        {
            sphereCollider.radius = triggerRadius;
        }
        
        DebugLog("딸 구출 트리거 설정 완료");
    }
    
    void CheckGameState()
    {
        // 보스가 처치되지 않았다면 트리거 비활성화
        if (requiresBossDefeated && GameProgressManager.Instance != null)
        {
            if (!GameProgressManager.Instance.IsBossDefeated)
            {
                triggerCollider.enabled = false;
                DebugLog("보스가 아직 처치되지 않아 구출 트리거 비활성화");
                
                // 보스 처치 이벤트 구독
                GameProgressManager.Instance.OnBossDefeated += OnBossDefeated;
            }
        }
    }
    
    void OnBossDefeated()
    {
        // 보스 처치 후 트리거 활성화
        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
            DebugLog("보스 처치됨! 딸 구출 트리거 활성화");
        }
        
        // 이벤트 구독 해제
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnBossDefeated -= OnBossDefeated;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (hasBeenRescued) return;
        
        if (IsValidPlayer(other))
        {
            OnPlayerEnter();
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (IsValidPlayer(other))
        {
            OnPlayerExit();
        }
    }
    
    bool IsValidPlayer(Collider other)
    {
        return other.CompareTag(playerTag);
    }
    
    void OnPlayerEnter()
    {
        DebugLog("플레이어가 딸 구출 범위에 진입");
        
        // 즉시 구출 처리
        RescueDaughter();
    }
    
    void OnPlayerExit()
    {
        DebugLog("플레이어가 딸 구출 범위에서 벗어남");
    }
    
    void RescueDaughter()
    {
        if (hasBeenRescued) return;
        
        hasBeenRescued = true;
        
        DebugLog("딸 구출 성공!");
        
        // 구출 효과 재생
        PlayRescueEffect();
        
        // GameProgressManager에 알림
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.NotifyDaughterRescued();
        }
        
        // 이벤트 호출
        OnDaughterRescued?.Invoke();
        
        // VolumeManager로 구출 사운드 재생
        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.PlaySFX(VolumeManager.SFXType.ItemPickup, transform.position);
        }
        
        // 트리거 비활성화
        triggerCollider.enabled = false;
    }
    
    void PlayRescueEffect()
    {
        if (rescueEffect != null)
        {
            GameObject effect = Instantiate(rescueEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
    }
    
    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[DaughterRescueTrigger] {message}");
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        // 트리거 범위 표시
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        
        // 상태에 따른 색상 변경
        if (hasBeenRescued)
        {
            Gizmos.color = Color.green;
        }
        else if (requiresBossDefeated && GameProgressManager.Instance != null && !GameProgressManager.Instance.IsBossDefeated)
        {
            Gizmos.color = Color.red;
        }
        else
        {
            Gizmos.color = Color.yellow;
        }
        
        Gizmos.DrawSphere(transform.position, 0.2f);
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnBossDefeated -= OnBossDefeated;
        }
    }
} 