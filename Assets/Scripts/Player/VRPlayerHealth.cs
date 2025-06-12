using UnityEngine;
using System.Collections;

/// <summary>
/// VR 플레이어 체력 및 피격 시스템
/// Enemy Attack1 시 화면이 빨갛게 변하는 효과 포함
/// </summary>
public class VRPlayerHealth : MonoBehaviour
{
    [Header("체력 설정")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("피격 효과")]
    public float damageEffectDuration = 1.5f;
    public float damageScreenIntensity = 0.8f;
    public Color damageScreenColor = Color.red;
    
    [Header("무적 시간")]
    public float invincibilityDuration = 1f;
    private bool isInvincible = false;
    
    // 참조
    private VRPostProcessingManager postProcessingManager;
    private OVRCameraRig cameraRig;
    
    // 체력 회복 시스템
    private float lastDamageTime = 0f;
    
    // 이벤트
    public System.Action<float> OnHealthChanged;
    public System.Action OnPlayerDeath;
    public System.Action OnPlayerDamaged;
    
    void Start()
    {
        InitializeHealth();
        FindReferences();
    }
    
    void Update()
    {
        // 체력 회복 시스템 (5초 후 자동 회복)
        if (currentHealth < maxHealth && Time.time - lastDamageTime > 5f)
        {
            float healAmount = maxHealth * 0.1f * Time.deltaTime; // 초당 10% 회복
            currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
            
            // 체력 회복에 따른 효과 업데이트
            float healthPercentage = currentHealth / maxHealth;
            ApplyHealthBasedEffect(healthPercentage);
        }
    }
    
    void InitializeHealth()
    {
        currentHealth = maxHealth;
        Debug.Log($"[VRPlayerHealth] 플레이어 체력 초기화: {currentHealth}/{maxHealth}");
    }
    
    void FindReferences()
    {
        // Post Processing Manager 찾기
        postProcessingManager = FindFirstObjectByType<VRPostProcessingManager>();
        if (postProcessingManager == null)
        {
            Debug.LogWarning("[VRPlayerHealth] VRPostProcessingManager를 찾을 수 없습니다!");
        }
        
        // OVR Camera Rig 찾기
        cameraRig = FindFirstObjectByType<OVRCameraRig>();
        if (cameraRig == null)
        {
            Debug.LogWarning("[VRPlayerHealth] OVRCameraRig를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// Enemy의 Attack1에서 호출되는 데미지 함수
    /// </summary>
    public void TakeDamage(float damage)
    {
        Debug.Log($"[VRPlayerHealth] 🔥 TakeDamage 호출됨! 데미지: {damage}");
        
        if (isInvincible)
        {
            Debug.Log("[VRPlayerHealth] ⚠️ 무적 상태라서 데미지 무시");
            return;
        }
        
        if (currentHealth <= 0)
        {
            Debug.Log("[VRPlayerHealth] ⚠️ 이미 죽은 상태라서 데미지 무시");
            return;
        }
        
        // 데미지 적용
        currentHealth = Mathf.Max(0, currentHealth - damage);
        lastDamageTime = Time.time; // 마지막 피격 시간 기록
        
        Debug.Log($"[VRPlayerHealth] ✅ 플레이어가 {damage} 데미지를 받았습니다! 현재 체력: {currentHealth}/{maxHealth}");
        
        // 이벤트 발생
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        OnPlayerDamaged?.Invoke();
        
        // VR 피격 효과 (즉시 적용)
        Debug.Log("[VRPlayerHealth] 🔴 VR 피격 효과 즉시 적용!");
        ApplyImmediateDamageEffect();
        
        // 햅틱 피드백
        Debug.Log("[VRPlayerHealth] 📳 햅틱 피드백 시작!");
        TriggerDamageHaptics();
        
        // 무적 시간 적용
        StartCoroutine(InvincibilityCoroutine());
        
        // 체력 확인
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // 체력에 따른 Post Processing 효과
            UpdateHealthEffects();
        }
    }
    
    /// <summary>
    /// 즉시 VR 피격 효과 적용 (딜레이 없음)
    /// </summary>
    private void ApplyImmediateDamageEffect()
    {
        if (postProcessingManager == null) 
        {
            Debug.LogError("[VRPlayerHealth] ❌ VRPostProcessingManager를 찾을 수 없음!");
            
            // 다시 찾기 시도
            postProcessingManager = FindFirstObjectByType<VRPostProcessingManager>();
            if (postProcessingManager == null)
            {
                Debug.LogError("[VRPlayerHealth] ❌ VRPostProcessingManager를 다시 찾아도 없음!");
                return;
            }
            else
            {
                Debug.Log("[VRPlayerHealth] ✅ VRPostProcessingManager를 다시 찾았음!");
            }
        }
        
        // 체력 비율 계산
        float healthPercentage = currentHealth / maxHealth;
        
        // 체력별 단계적 효과 적용
        ApplyHealthBasedEffect(healthPercentage);
        
        // 피격 순간 강한 효과 (0.3초 후 체력별 효과로 복구)
        postProcessingManager.TriggerInstantDamageFlash();
        
        // 0.3초 후 체력별 상태로 복구
        StartCoroutine(RestoreToHealthBasedEffect(healthPercentage));
    }
    
    /// <summary>
    /// 체력별 단계적 효과 적용
    /// </summary>
    private void ApplyHealthBasedEffect(float healthPercentage)
    {
        Debug.Log($"[VRPlayerHealth] 체력별 효과 적용: {healthPercentage:P1} ({currentHealth}/{maxHealth})");
        
        if (healthPercentage > 0.75f)
        {
            // 75-100%: 연한 외각 빨강
            Debug.Log("[VRPlayerHealth] 체력 상태: 양호 (연한 외각 빨강)");
            postProcessingManager.SetHealthBasedEffect(VRPostProcessingManager.HealthState.Good);
        }
        else if (healthPercentage > 0.50f)
        {
            // 50-75%: 중간 범위 빨강
            Debug.Log("[VRPlayerHealth] 체력 상태: 주의 (중간 범위 빨강)");
            postProcessingManager.SetHealthBasedEffect(VRPostProcessingManager.HealthState.Caution);
        }
        else if (healthPercentage > 0.25f)
        {
            // 25-50%: 넓은 범위 진한 빨강
            Debug.Log("[VRPlayerHealth] 체력 상태: 위험 (넓은 범위 진한 빨강)");
            postProcessingManager.SetHealthBasedEffect(VRPostProcessingManager.HealthState.Danger);
        }
        else
        {
            // 0-25%: 완전 빨강
            Debug.Log("[VRPlayerHealth] 체력 상태: 치명적 (완전 빨강)");
            postProcessingManager.SetHealthBasedEffect(VRPostProcessingManager.HealthState.Critical);
        }
    }
    
    /// <summary>
    /// 피격 플래시 후 체력별 효과로 복구
    /// </summary>
    private IEnumerator RestoreToHealthBasedEffect(float healthPercentage)
    {
        yield return new WaitForSeconds(0.3f);
        ApplyHealthBasedEffect(healthPercentage);
    }
    
    /// <summary>
    /// 피격 시 햅틱 피드백 (양손 컨트롤러)
    /// </summary>
    private void TriggerDamageHaptics()
    {
        // 양손 컨트롤러에 강한 진동
        OVRInput.SetControllerVibration(0.8f, 0.8f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0.8f, 0.8f, OVRInput.Controller.RTouch);
        
        // 0.5초 후 진동 중지
        StartCoroutine(StopHapticsAfterDelay(0.5f));
    }
    
    private IEnumerator StopHapticsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
    }
    
    /// <summary>
    /// 무적 시간 코루틴
    /// </summary>
    private IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }
    
    /// <summary>
    /// 체력에 따른 Post Processing 효과 업데이트
    /// </summary>
    private void UpdateHealthEffects()
    {
        if (postProcessingManager == null) return;
        
        float healthPercentage = currentHealth / maxHealth;
        
        if (healthPercentage <= 0.2f)
        {
            // 매우 위험한 상태
            postProcessingManager.SetEffectState(VRPostProcessingManager.EffectState.LowHealth);
        }
        else if (healthPercentage <= 0.5f)
        {
            // 주의 상태
            postProcessingManager.SetEffectState(VRPostProcessingManager.EffectState.Scared);
        }
        else
        {
            // 정상 상태
            postProcessingManager.SetEffectState(VRPostProcessingManager.EffectState.Normal);
        }
    }
    
    /// <summary>
    /// 플레이어 사망 처리
    /// </summary>
    private void Die()
    {
        Debug.Log("[VRPlayerHealth] 💀 플레이어 사망!");
        
        // 사망 효과
        if (postProcessingManager != null)
        {
            postProcessingManager.SetEffectState(VRPostProcessingManager.EffectState.Death);
        }
        
        // 사망 이벤트
        OnPlayerDeath?.Invoke();
        
        // 게임 일시 정지 또는 재시작 로직 (필요시 추가)
    }
    
    /// <summary>
    /// 체력 회복
    /// </summary>
    public void Heal(float amount)
    {
        if (currentHealth <= 0) return; // 죽은 상태에서는 회복 불가
        
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        UpdateHealthEffects();
        
        Debug.Log($"[VRPlayerHealth] 체력 회복 +{amount}! 현재 체력: {currentHealth}/{maxHealth}");
    }
    
    /// <summary>
    /// 디버그용 프로퍼티들
    /// </summary>
    public bool IsDead => currentHealth <= 0;
    public bool IsInvincible => isInvincible;
    public float HealthPercentage => currentHealth / maxHealth;
    
    /// <summary>
    /// 디버그 GUI (개발용)
    /// </summary>
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUILayout.Label($"체력: {currentHealth:F1}/{maxHealth}");
        GUILayout.Label($"체력 비율: {HealthPercentage:P1}");
        GUILayout.Label($"무적 상태: {(isInvincible ? "ON" : "OFF")}");
        
        if (GUILayout.Button("테스트 데미지 (20)"))
        {
            TakeDamage(20f);
        }
        
        if (GUILayout.Button("체력 회복 (30)"))
        {
            Heal(30f);
        }
        GUILayout.EndArea();
    }
} 