using UnityEngine;

/// <summary>
/// 네크로맨서 체력 시스템
/// 체력 관리와 데미지 처리만 담당
/// </summary>
public class NecromancerHealth : MonoBehaviour
{
    [Header("=== 체력 설정 ===")]
    public float maxHealth = 400f;
    public bool isInvulnerable = false;
    public float invulnerabilityDuration = 0.5f; // 피격 후 무적 시간
    
    [Header("=== 데미지 설정 ===")]
    public float damageMultiplier = 1f;
    public float criticalHitMultiplier = 1.5f;
    public float minimumDamage = 1f;
    
    [Header("=== 디버그 ===")]
    public bool enableHealthLogs = false;
    
    // 체력 상태
    private float currentHealth;
    private bool isDead = false;
    private float lastDamageTime = 0f;
    
    // 컴포넌트 참조
    private NecromancerBoss bossController;
    
    // 이벤트
    public System.Action<float, float> OnHealthChanged; // (currentHealth, maxHealth)
    public System.Action<float, Vector3> OnHit; // (damage, hitPosition)
    public System.Action OnDeath;
    public System.Action<float> OnCriticalHit; // (damage)
    
    // 프로퍼티
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercent => currentHealth / maxHealth;
    public bool IsDead => isDead;
    public bool IsInvulnerable => isInvulnerable || (Time.time - lastDamageTime < invulnerabilityDuration);
    
    public void Initialize(NecromancerBoss boss, float health)
    {
        bossController = boss;
        maxHealth = health;
        currentHealth = maxHealth;
        isDead = false;
        
        HealthLog($"체력 시스템 초기화: {currentHealth}/{maxHealth}");
    }
    
    void Start()
    {
        // 초기화가 안 되어 있으면 기본값으로 설정
        if (currentHealth <= 0)
        {
            currentHealth = maxHealth;
        }
    }
    
    #region 데미지 처리
    
    public void TakeDamage(float damage, Vector3 attackPosition)
    {
        if (isDead || IsInvulnerable) return;
        
        // 데미지 계산
        float finalDamage = CalculateFinalDamage(damage, attackPosition);
        
        // 체력 감소
        currentHealth = Mathf.Max(0, currentHealth - finalDamage);
        lastDamageTime = Time.time;
        
        // 이벤트 발생
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHit?.Invoke(finalDamage, attackPosition);
        
        HealthLog($"데미지 받음: {finalDamage:F1} (체력: {currentHealth:F1}/{maxHealth})");
        
        // 크리티컬 히트 체크
        if (IsCriticalHit(attackPosition))
        {
            OnCriticalHit?.Invoke(finalDamage);
            HealthLog("크리티컬 히트!");
        }
        
        // 사망 체크
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }
    
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position);
    }
    
    private float CalculateFinalDamage(float baseDamage, Vector3 attackPosition)
    {
        float finalDamage = baseDamage * damageMultiplier;
        
        // 크리티컬 히트 체크
        if (IsCriticalHit(attackPosition))
        {
            finalDamage *= criticalHitMultiplier;
        }
        
        // 최소 데미지 보장
        finalDamage = Mathf.Max(finalDamage, minimumDamage);
        
        return finalDamage;
    }
    
    private bool IsCriticalHit(Vector3 attackPosition)
    {
        // 머리 부분 히트 체크 (간단한 구현)
        Vector3 headPosition = transform.position + Vector3.up * 1.8f;
        float distanceToHead = Vector3.Distance(attackPosition, headPosition);
        
        return distanceToHead < 0.5f; // 머리 반경 0.5m
    }
    
    #endregion
    
    #region 체력 회복
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        
        if (currentHealth != oldHealth)
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            HealthLog($"체력 회복: +{amount:F1} (체력: {currentHealth:F1}/{maxHealth})");
        }
    }
    
    public void SetHealth(float health)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        HealthLog($"체력 설정: {currentHealth:F1}/{maxHealth}");
    }
    
    public void SetMaxHealth(float newMaxHealth)
    {
        float healthPercent = HealthPercent;
        maxHealth = newMaxHealth;
        currentHealth = maxHealth * healthPercent;
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        HealthLog($"최대 체력 변경: {maxHealth}");
    }
    
    #endregion
    
    #region 상태 제어
    
    public void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable = invulnerable;
        HealthLog($"무적 상태: {invulnerable}");
    }
    
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        isInvulnerable = false;
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        HealthLog("체력 초기화");
    }
    
    #endregion
    
    #region 사망 처리
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        currentHealth = 0;
        
        OnDeath?.Invoke();
        HealthLog("사망!");
        
        // 사망 후 처리 (필요시)
        StartCoroutine(DeathSequence());
    }
    
    private System.Collections.IEnumerator DeathSequence()
    {
        // 사망 애니메이션 대기
        yield return new WaitForSeconds(3f);
        
        // 추가 사망 처리 (필요시)
        HealthLog("사망 시퀀스 완료");
    }
    
    #endregion
    
    // UI 제거됨 - VolumeManager로 사운드만 처리
    
    #region 디버그 및 유틸리티
    
    private void HealthLog(string message)
    {
        if (enableHealthLogs)
            Debug.Log($"[NecromancerHealth] {message}");
    }
    
    // 디버그용 기즈모
    void OnDrawGizmosSelected()
    {
        // 크리티컬 히트 영역 (머리)
        Gizmos.color = Color.yellow;
        Vector3 headPosition = transform.position + Vector3.up * 1.8f;
        Gizmos.DrawWireSphere(headPosition, 0.5f);
        
        // 체력 상태에 따른 색상
        if (isDead)
        {
            Gizmos.color = Color.black;
        }
        else if (HealthPercent < 0.3f)
        {
            Gizmos.color = Color.red;
        }
        else if (HealthPercent < 0.7f)
        {
            Gizmos.color = Color.yellow;
        }
        else
        {
            Gizmos.color = Color.green;
        }
        
        Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, Vector3.one * 0.5f);
    }
    
    // 외부에서 체력 정보 조회
    public string GetHealthInfo()
    {
        return $"체력: {currentHealth:F1}/{maxHealth} ({HealthPercent:P0})";
    }
    
    #endregion
} 