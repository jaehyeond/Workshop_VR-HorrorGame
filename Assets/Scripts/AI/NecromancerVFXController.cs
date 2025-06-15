using UnityEngine;

/// <summary>
/// 네크로맨서 VFX 컨트롤러
/// VR 효과와 시각적 피드백만 담당
/// </summary>
public class NecromancerVFXController : MonoBehaviour
{
    [Header("=== 피격 효과 ===")]
    public GameObject hitEffect;
    public GameObject criticalHitEffect;
    public float hitEffectDuration = 2f;
    
    [Header("=== 공격 효과 ===")]
    public GameObject meleeAttackEffect;
    public GameObject spellCastEffect;
    public GameObject spellProjectileEffect;
    
    [Header("=== 상태 효과 ===")]
    public GameObject deathEffect;
    public GameObject roarEffect;
    public GameObject angerEffect; // 저체력 시
    
    [Header("=== VR 햅틱 피드백 ===")]
    public bool enableHapticFeedback = true;
    public float hitHapticIntensity = 0.8f;
    public float hitHapticDuration = 0.2f;
    
    [Header("=== 화면 효과 ===")]
    public bool enableScreenEffects = true;
    public Color damageScreenTint = Color.red;
    public float screenEffectDuration = 0.3f;
    
    // 사운드는 VolumeManager에서 처리
    
    // 컴포넌트 참조
    private NecromancerBoss bossController;
    private Camera playerCamera;
    
    // VR 컨트롤러 참조 (햅틱 피드백용)
    private OVRInput.Controller leftController = OVRInput.Controller.LTouch;
    private OVRInput.Controller rightController = OVRInput.Controller.RTouch;
    
    // 효과 상태
    private bool isPlayingDeathEffect = false;
    private Coroutine screenEffectCoroutine;
    
    public void Initialize(NecromancerBoss boss)
    {
        bossController = boss;
        
        // 플레이어 카메라 찾기
        FindPlayerCamera();
        
        VFXLog("VFX 컨트롤러 초기화 완료");
    }
    
    void FindPlayerCamera()
    {
        // VR 카메라 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            // OVR 카메라 찾기
            OVRCameraRig cameraRig = playerObj.GetComponentInChildren<OVRCameraRig>();
            if (cameraRig != null)
            {
                playerCamera = cameraRig.centerEyeAnchor.GetComponent<Camera>();
            }
            
            // 일반 카메라 찾기 (백업)
            if (playerCamera == null)
            {
                playerCamera = playerObj.GetComponentInChildren<Camera>();
            }
        }
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }
    
    #region 피격 효과
    
    public void PlayHitEffect(Vector3 hitPosition)
    {
        // 히트 이펙트 생성
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, hitPosition, Quaternion.identity);
            Destroy(effect, hitEffectDuration);
        }
        
        // VolumeManager로 히트 사운드 재생
        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.PlaySFX(VolumeManager.SFXType.BossAttack, transform.position);
        }
        
        // VR 햅틱 피드백
        if (enableHapticFeedback)
        {
            TriggerHapticFeedback(hitHapticIntensity, hitHapticDuration);
        }
        
        // 화면 효과
        if (enableScreenEffects)
        {
            TriggerScreenEffect(damageScreenTint, screenEffectDuration);
        }
        
        VFXLog("피격 효과 재생");
    }
    
    public void PlayCriticalHitEffect(Vector3 hitPosition)
    {
        // 크리티컬 히트 이펙트
        if (criticalHitEffect != null)
        {
            GameObject effect = Instantiate(criticalHitEffect, hitPosition, Quaternion.identity);
            Destroy(effect, hitEffectDuration * 1.5f);
        }
        
        // VolumeManager로 크리티컬 히트 사운드 재생
        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.PlaySFX(VolumeManager.SFXType.BossHeavyAttack, transform.position);
        }
        
        // 강한 햅틱 피드백
        if (enableHapticFeedback)
        {
            TriggerHapticFeedback(1f, hitHapticDuration * 1.5f);
        }
        
        // 강한 화면 효과
        if (enableScreenEffects)
        {
            TriggerScreenEffect(Color.yellow, screenEffectDuration * 1.5f);
        }
        
        VFXLog("크리티컬 히트 효과 재생");
    }
    
    #endregion
    
    #region 공격 효과
    
    public void PlayMeleeAttackEffect(Vector3 attackPosition)
    {
        if (meleeAttackEffect != null)
        {
            GameObject effect = Instantiate(meleeAttackEffect, attackPosition, transform.rotation);
            Destroy(effect, 2f);
        }
        
        VFXLog("근접 공격 효과 재생");
    }
    
    public void PlaySpellCastEffect()
    {
        if (spellCastEffect != null)
        {
            Vector3 castPosition = transform.position + Vector3.up * 1.5f;
            GameObject effect = Instantiate(spellCastEffect, castPosition, transform.rotation);
            Destroy(effect, 3f);
        }
        
        VFXLog("마법 시전 효과 재생");
    }
    
    public void PlaySpellProjectileEffect(Vector3 startPosition, Vector3 targetPosition)
    {
        if (spellProjectileEffect != null)
        {
            Vector3 direction = (targetPosition - startPosition).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction);
            
            GameObject projectile = Instantiate(spellProjectileEffect, startPosition, rotation);
            
            // 투사체 이동 (간단한 구현)
            StartCoroutine(MoveProjectile(projectile, startPosition, targetPosition));
        }
        
        VFXLog("마법 투사체 효과 재생");
    }
    
    private System.Collections.IEnumerator MoveProjectile(GameObject projectile, Vector3 start, Vector3 target)
    {
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration && projectile != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            projectile.transform.position = Vector3.Lerp(start, target, progress);
            
            yield return null;
        }
        
        if (projectile != null)
        {
            Destroy(projectile);
        }
    }
    
    #endregion
    
    #region 상태 효과
    
    public void PlayDeathEffect()
    {
        if (isPlayingDeathEffect) return;
        
        isPlayingDeathEffect = true;
        
        if (deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, transform.rotation);
            Destroy(effect, 5f);
        }
        
        // VolumeManager로 사망 사운드 재생
        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.PlaySFX(VolumeManager.SFXType.BossDeath, transform.position);
        }
        
        VFXLog("사망 효과 재생");
    }
    
    public void PlayRoarEffect()
    {
        if (roarEffect != null)
        {
            Vector3 roarPosition = transform.position + Vector3.up * 1.8f;
            GameObject effect = Instantiate(roarEffect, roarPosition, transform.rotation);
            Destroy(effect, 2f);
        }
        
        // VolumeManager로 포효 사운드 재생
        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.PlaySFX(VolumeManager.SFXType.BossRage, transform.position);
        }
        
        // 포효 시 강한 햅틱 피드백
        if (enableHapticFeedback)
        {
            TriggerHapticFeedback(0.6f, 1f);
        }
        
        VFXLog("포효 효과 재생");
    }
    
    public void PlayAngerEffect()
    {
        if (angerEffect != null)
        {
            GameObject effect = Instantiate(angerEffect, transform.position, transform.rotation);
            effect.transform.SetParent(transform); // 보스를 따라다니도록
            Destroy(effect, 10f);
        }
        
        VFXLog("분노 효과 재생");
    }
    
    #endregion
    
    #region VR 햅틱 피드백
    
    private void TriggerHapticFeedback(float intensity, float duration)
    {
        if (!enableHapticFeedback) return;
        
        // 양손 컨트롤러에 햅틱 피드백
        StartCoroutine(HapticFeedbackCoroutine(intensity, duration));
    }
    
    private System.Collections.IEnumerator HapticFeedbackCoroutine(float intensity, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // OVR 햅틱 피드백
            OVRInput.SetControllerVibration(intensity, intensity, leftController);
            OVRInput.SetControllerVibration(intensity, intensity, rightController);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 햅틱 피드백 정지
        OVRInput.SetControllerVibration(0, 0, leftController);
        OVRInput.SetControllerVibration(0, 0, rightController);
    }
    
    #endregion
    
    #region 화면 효과
    
    private void TriggerScreenEffect(Color tintColor, float duration)
    {
        if (!enableScreenEffects || playerCamera == null) return;
        
        // 기존 화면 효과가 있으면 중지
        if (screenEffectCoroutine != null)
        {
            StopCoroutine(screenEffectCoroutine);
        }
        
        screenEffectCoroutine = StartCoroutine(ScreenEffectCoroutine(tintColor, duration));
    }
    
    private System.Collections.IEnumerator ScreenEffectCoroutine(Color tintColor, float duration)
    {
        // 간단한 화면 틴트 효과 (실제 구현에서는 Post-Processing 사용 권장)
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.3f, 0f, elapsed / duration);
            
            // GUI를 사용한 간단한 오버레이 (실제로는 더 정교한 방법 사용)
            // 실제 구현에서는 Post-Processing Volume이나 UI Canvas 사용
            
            yield return null;
        }
        
        screenEffectCoroutine = null;
    }
    
    #endregion
    
    // 사운드는 VolumeManager에서 처리
    
    #region 유틸리티
    
    private void VFXLog(string message)
    {
        Debug.Log($"[NecromancerVFX] {message}");
    }
    
    // 외부에서 호출 가능한 편의 메서드들
    public void OnBossHit(float damage, Vector3 hitPosition)
    {
        if (damage > 50f) // 크리티컬 히트 기준
        {
            PlayCriticalHitEffect(hitPosition);
        }
        else
        {
            PlayHitEffect(hitPosition);
        }
    }
    
    public void OnBossAttack(Vector3 attackPosition)
    {
        PlayMeleeAttackEffect(attackPosition);
    }
    
    public void OnBossSpellCast(Vector3 targetPosition)
    {
        PlaySpellCastEffect();
        
        // 투사체 효과 (딜레이 후)
        Invoke(nameof(DelayedSpellProjectile), 1f);
    }
    
    private void DelayedSpellProjectile()
    {
        if (bossController != null && bossController.Player != null)
        {
            Vector3 startPos = transform.position + Vector3.up * 1.5f;
            Vector3 targetPos = bossController.Player.position;
            PlaySpellProjectileEffect(startPos, targetPos);
        }
    }
    
    public void OnBossLowHealth()
    {
        PlayAngerEffect();
    }
    
    #endregion
    
    void OnDestroy()
    {
        // 모든 코루틴 정지
        StopAllCoroutines();
        
        // 햅틱 피드백 정지
        if (enableHapticFeedback)
        {
            OVRInput.SetControllerVibration(0, 0, leftController);
            OVRInput.SetControllerVibration(0, 0, rightController);
        }
    }
} 