using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// 체력별 효과 설정 구조체
/// </summary>
[System.Serializable]
public struct HealthEffectSettings
{
    public float vignetteIntensity;
    public Color vignetteColor;
    public float saturation;
    public float hueShift;
    public float contrast;
}

/// <summary>
/// VR 호러 게임을 위한 Post Processing 기반 화면 효과 매니저
/// Universal RP의 Volume 시스템을 사용한 고품질 VR 피격 효과
/// </summary>
public class VRPostProcessingManager : MonoBehaviour
{
    [Header("VR Post Processing 설정")]
    [SerializeField] private bool enableEffects = true;
    
    // Post Processing 컴포넌트들
    private Volume globalVolume;
    private VolumeProfile volumeProfile;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;
    private Bloom bloom;
    
    // 현재 상태
    private EffectState currentState = EffectState.Normal;
    private float currentIntensity = 0f;
    private bool isEffectActive = false;
    
    // Public 접근자들
    public EffectState CurrentState => currentState;
    public float CurrentIntensity => currentIntensity;
    public bool IsEffectActive => isEffectActive;
    
    public enum EffectState
    {
        Normal,
        Scared,
        LowHealth,
        Death,
        Custom
    }
    
    public enum HealthState
    {
        Perfect,    // 100%: 정상
        Good,       // 75-100%: 연한 외각 빨강
        Caution,    // 50-75%: 중간 범위 빨강
        Danger,     // 25-50%: 넓은 범위 진한 빨강
        Critical    // 0-25%: 완전 빨강
    }
    
    /// <summary>
    /// Game Over 효과 완료 이벤트
    /// </summary>
    public System.Action OnGameOverEffectComplete;
    
    // 체력별 효과 설정
    [Header("체력별 단계 효과")]
    [SerializeField] private HealthEffectSettings[] healthEffectSettings;
    
    private void Awake()
    {
        InitializePostProcessing();
    }
    
    private void Start()
    {
        ResetToNormalState();
        InitializeHealthEffectSettings();
        Debug.Log("[VRPostProcessingManager] ✅ Post Processing 기반 VR 효과 시스템 초기화 완료");
    }
    
    /// <summary>
    /// 체력별 효과 설정 초기화
    /// </summary>
    private void InitializeHealthEffectSettings()
    {
        healthEffectSettings = new HealthEffectSettings[5];
        
        // Perfect (100%): 정상
        healthEffectSettings[0] = new HealthEffectSettings
        {
            vignetteIntensity = 0f,
            vignetteColor = Color.black,
            saturation = 0f,
            hueShift = 0f,
            contrast = 0f
        };
        
        // Good (75-100%): 연한 외각 빨강
        healthEffectSettings[1] = new HealthEffectSettings
        {
            vignetteIntensity = 0.25f,
            vignetteColor = new Color(1f, 0.8f, 0.8f, 1f),
            saturation = 10f,
            hueShift = -5f,
            contrast = 5f
        };
        
        // Caution (50-75%): 중간 범위 빨강
        healthEffectSettings[2] = new HealthEffectSettings
        {
            vignetteIntensity = 0.45f,
            vignetteColor = new Color(1f, 0.6f, 0.6f, 1f),
            saturation = 25f,
            hueShift = -10f,
            contrast = 15f
        };
        
        // Danger (25-50%): 넓은 범위 진한 빨강
        healthEffectSettings[3] = new HealthEffectSettings
        {
            vignetteIntensity = 0.7f,
            vignetteColor = new Color(1f, 0.4f, 0.4f, 1f),
            saturation = 50f,
            hueShift = -15f,
            contrast = 30f
        };
        
        // Critical (0-25%): 완전 빨강
        healthEffectSettings[4] = new HealthEffectSettings
        {
            vignetteIntensity = 0.95f,
            vignetteColor = new Color(1f, 0.2f, 0.2f, 1f),
            saturation = 80f,
            hueShift = -20f,
            contrast = 50f
        };
        
        Debug.Log("[VRPostProcessingManager] 체력별 효과 설정 초기화 완료");
    }
    
    /// <summary>
    /// Post Processing 시스템 초기화
    /// </summary>
    private void InitializePostProcessing()
    {
        // 기존 Global Volume들 찾기
        var existingVolumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
        Debug.Log($"[VRPostProcessingManager] 기존 Volume 개수: {existingVolumes.Length}");
        
        // 우리만의 전용 Volume 생성
        GameObject volumeObj = new GameObject("VR Damage Effect Volume");
        globalVolume = volumeObj.AddComponent<Volume>();
        globalVolume.isGlobal = true;
        globalVolume.priority = 100; // 최고 우선순위
        
        // 기존 Volume들 우선순위 낮추기
        foreach (var vol in existingVolumes)
        {
            vol.priority = 0;
            Debug.Log($"[VRPostProcessingManager] {vol.name} 우선순위를 0으로 설정");
        }
        
        // 새로운 Volume Profile 생성
        volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        globalVolume.profile = volumeProfile;
        
        // Post Processing 효과들 설정
        SetupPostProcessingEffects();
        
        Debug.Log("[VRPostProcessingManager] ✅ 전용 VR Volume 생성 완료 (우선순위: 100)");
    }
    
    /// <summary>
    /// Post Processing 효과들 설정
    /// </summary>
    private void SetupPostProcessingEffects()
    {
        // Vignette 설정
        if (!volumeProfile.TryGet<Vignette>(out vignette))
        {
            vignette = volumeProfile.Add<Vignette>(false);
        }
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0f;
        vignette.color.overrideState = true;
        vignette.color.value = Color.black;
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = 0.4f;
        
        // Color Adjustments 설정
        if (!volumeProfile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            colorAdjustments = volumeProfile.Add<ColorAdjustments>(false);
        }
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.saturation.value = 0f;
        colorAdjustments.hueShift.overrideState = true;
        colorAdjustments.hueShift.value = 0f;
        colorAdjustments.contrast.overrideState = true;
        colorAdjustments.contrast.value = 0f;
        colorAdjustments.colorFilter.overrideState = true;
        colorAdjustments.colorFilter.value = Color.white;
        
        // Bloom 설정
        if (!volumeProfile.TryGet<Bloom>(out bloom))
        {
            bloom = volumeProfile.Add<Bloom>(false);
        }
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 0f;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 1.3f;
        
        Debug.Log("[VRPostProcessingManager] Post Processing 효과 설정 완료");
    }
    
    /// <summary>
    /// 정상 상태로 초기화
    /// </summary>
    private void ResetToNormalState()
    {
        currentState = EffectState.Normal;
        currentIntensity = 0f;
        isEffectActive = false;
        
        if (vignette != null)
        {
            vignette.intensity.value = 0f;
        }
        
        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = 0f;
            colorAdjustments.hueShift.value = 0f;
            colorAdjustments.contrast.value = 0f;
            colorAdjustments.colorFilter.value = Color.white;
        }
        
        if (bloom != null)
        {
            bloom.intensity.value = 0f;
        }
        
        Debug.Log("[VRPostProcessingManager] 정상 상태로 리셋 완료");
    }
    
    /// <summary>
    /// 즉시 피격 플래시 효과 (0.3초 강한 빨강)
    /// </summary>
    public void TriggerInstantDamageFlash()
    {
        Debug.Log("[VRPostProcessingManager] ⚡ 즉시 피격 플래시 효과!");
        StartCoroutine(InstantDamageFlashCoroutine());
    }
    
    /// <summary>
    /// 즉시 피격 플래시 코루틴
    /// </summary>
    private IEnumerator InstantDamageFlashCoroutine()
    {
        // 강한 빨간 플래시 효과
        ApplyInstantFlashEffect();
        
        // 0.3초 대기
        yield return new WaitForSeconds(0.3f);
        
        Debug.Log("[VRPostProcessingManager] 피격 플래시 효과 완료");
    }
    
    /// <summary>
    /// 체력별 단계 효과 설정
    /// </summary>
    public void SetHealthBasedEffect(HealthState healthState)
    {
        Debug.Log($"[VRPostProcessingManager] 체력별 효과 적용: {healthState}");
        
        if (globalVolume == null || globalVolume.profile == null)
        {
            Debug.LogError("[VRPostProcessingManager] ❌ GlobalVolume 또는 Profile이 null!");
            return;
        }
        
        int index = (int)healthState;
        if (index >= 0 && index < healthEffectSettings.Length)
        {
            ApplyHealthEffect(healthEffectSettings[index]);
        }
        else
        {
            Debug.LogError($"[VRPostProcessingManager] ❌ 잘못된 HealthState 인덱스: {index}");
        }
    }
    
    /// <summary>
    /// 즉시 플래시 효과 적용 (극강 빨강)
    /// </summary>
    private void ApplyInstantFlashEffect()
    {
        if (vignette == null || colorAdjustments == null)
        {
            Debug.LogWarning("[VRPostProcessingManager] Post Processing 컴포넌트가 없습니다!");
            return;
        }
        
        isEffectActive = true;
        Debug.Log("[VRPostProcessingManager] ⚡ 즉시 플래시 효과 적용!");
        
        // 모든 Global Volume 찾아서 우선순위 높이기
        var allVolumes = FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None);
        foreach (var vol in allVolumes)
        {
            if (vol != globalVolume)
            {
                vol.priority = 0; // 다른 Volume 우선순위 낮추기
            }
        }
        globalVolume.priority = 100; // 우리 Volume 최고 우선순위
        
        // 극강 빨간 플래시 효과
        vignette.intensity.value = 1.0f; // 최대 비네팅
        vignette.color.value = Color.red; // 순수 빨간색
        
        // 색상 조정으로 빨간 필터 효과 (극강)
        colorAdjustments.saturation.value = 120f; // 최대 채도
        colorAdjustments.hueShift.value = 0f; // 색조 이동 없음
        colorAdjustments.contrast.value = 60f; // 최대 대비
        colorAdjustments.colorFilter.value = new Color(1f, 0.1f, 0.1f, 1f); // 극강 빨간 필터
        
        // 블룸으로 강렬한 효과
        bloom.intensity.value = 3.0f; // 최대 블룸
        
        Debug.Log("[VRPostProcessingManager] ✅ 즉시 플래시 효과 적용 완료!");
    }
    
    /// <summary>
    /// 체력별 효과 적용
    /// </summary>
    private void ApplyHealthEffect(HealthEffectSettings settings)
    {
        if (vignette == null || colorAdjustments == null)
        {
            Debug.LogWarning("[VRPostProcessingManager] Post Processing 컴포넌트가 없습니다!");
            return;
        }
        
        Debug.Log($"[VRPostProcessingManager] 체력별 효과 적용: 비네팅={settings.vignetteIntensity}, 채도={settings.saturation}");
        
        // 비네팅 효과
        vignette.intensity.value = settings.vignetteIntensity;
        vignette.color.value = settings.vignetteColor;
        
        // 색상 조정
        colorAdjustments.saturation.value = settings.saturation;
        colorAdjustments.hueShift.value = settings.hueShift;
        colorAdjustments.contrast.value = settings.contrast;
        
        // 체력별 색상 필터
        if (settings.vignetteIntensity > 0f)
        {
            colorAdjustments.colorFilter.value = new Color(1f, 1f - settings.vignetteIntensity * 0.8f, 1f - settings.vignetteIntensity * 0.8f, 1f);
        }
        else
        {
            colorAdjustments.colorFilter.value = Color.white;
        }
        
        // 블룸 효과
        bloom.intensity.value = settings.vignetteIntensity * 0.5f;
        
        isEffectActive = settings.vignetteIntensity > 0f;
    }
    
    /// <summary>
    /// 효과 복구 코루틴 (간단한 버전)
    /// </summary>
    private System.Collections.IEnumerator RestoreEffectAfterDelay(float duration)
    {
        // 지속시간 대기
        yield return new WaitForSeconds(duration);
        
        // 즉시 정상화
        ResetToNormalState();
        
        Debug.Log("[VRPostProcessingManager] ✅ 피격 효과 복구 완료!");
    }
    
    /// <summary>
    /// VR 피격 효과 코루틴 (Post Processing 기반) - 사용 안함
    /// </summary>
    private System.Collections.IEnumerator VRDamageEffectCoroutine(float intensity, float duration)
    {
        if (vignette == null || colorAdjustments == null)
        {
            Debug.LogWarning("[VRPostProcessingManager] Post Processing 컴포넌트가 없습니다!");
            yield break;
        }
        
        isEffectActive = true;
        Debug.Log("[VRPostProcessingManager] 🔴 강화된 Post Processing VR 피격 효과 시작!");
        
        // 즉시 강력한 빨간 효과 적용
        vignette.intensity.value = 0.9f; // 더 강한 비네팅
        vignette.color.value = Color.red; // 순수 빨간색
        
        // 색상 조정으로 빨간 필터 효과 (극강 빨간색)
        colorAdjustments.saturation.value = 80f; // 매우 높은 채도
        colorAdjustments.hueShift.value = 0f; // 색조 이동 없음 (순수 빨간색)
        colorAdjustments.contrast.overrideState = true;
        colorAdjustments.contrast.value = 30f; // 매우 높은 대비
        
        // 추가: 색온도 조정으로 빨간색 강화
        colorAdjustments.colorFilter.overrideState = true;
        colorAdjustments.colorFilter.value = new Color(1f, 0.3f, 0.3f, 1f); // 빨간 필터
        
        // 블룸으로 강렬한 효과
        bloom.intensity.value = 1.2f; // 더 강한 블룸
        
        // 지속시간 대기
        yield return new WaitForSeconds(duration);
        
        // 점진적으로 원상복구
        float elapsedTime = 0f;
        float restoreDuration = 1.0f;
        
        float startVignette = vignette.intensity.value;
        float startSaturation = colorAdjustments.saturation.value;
        float startHueShift = colorAdjustments.hueShift.value;
        float startContrast = colorAdjustments.contrast.value;
        Color startColorFilter = colorAdjustments.colorFilter.value;
        float startBloom = bloom.intensity.value;
        
        while (elapsedTime < restoreDuration)
        {
            float t = elapsedTime / restoreDuration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            vignette.intensity.value = Mathf.Lerp(startVignette, 0f, t);
            colorAdjustments.saturation.value = Mathf.Lerp(startSaturation, 0f, t);
            colorAdjustments.hueShift.value = Mathf.Lerp(startHueShift, 0f, t);
            colorAdjustments.contrast.value = Mathf.Lerp(startContrast, 0f, t);
            colorAdjustments.colorFilter.value = Color.Lerp(startColorFilter, Color.white, t);
            bloom.intensity.value = Mathf.Lerp(startBloom, 0f, t);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 최종 정상화
        ResetToNormalState();
        
        Debug.Log("[VRPostProcessingManager] ✅ 피격 효과 원상복구 완료!");
    }
    
    /// <summary>
    /// 체력 기반 점진적 효과
    /// </summary>
    public void UpdateHealthBasedEffect(float healthPercentage)
    {
        if (vignette == null) return;
        
        Debug.Log($"[VRPostProcessingManager] 체력 기반 효과 업데이트: {healthPercentage:P1}");
        
        // 체력이 낮을수록 강한 비네팅
        if (healthPercentage <= 0.5f)
        {
            float effectStrength = 1f - healthPercentage;
            float vignetteIntensity = Mathf.Lerp(0.1f, 0.6f, effectStrength * 2f);
            
            vignette.intensity.value = vignetteIntensity;
            vignette.color.value = Color.red;
            
            Debug.Log($"[VRPostProcessingManager] 🩸 체력 기반 효과: 비네팅={vignetteIntensity:F2}");
        }
        else
        {
            vignette.intensity.value = 0f;
        }
    }
    
    /// <summary>
    /// Game Over 암전 효과
    /// </summary>
    public void TriggerGameOverEffect()
    {
        Debug.Log("[VRPostProcessingManager] 💀 Game Over 효과 시작!");
        StartCoroutine(GameOverEffectCoroutine());
    }
    
    /// <summary>
    /// Game Over 암전 효과 코루틴
    /// </summary>
    private System.Collections.IEnumerator GameOverEffectCoroutine()
    {
        if (vignette == null) yield break;
        
        // 1단계: 빠른 빨간 플래시
        vignette.intensity.value = 1f;
        vignette.color.value = Color.red;
        yield return new WaitForSeconds(0.3f);
        
        // 2단계: 점진적 암전 (3초에 걸쳐)
        float fadeTime = 3f;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeTime)
        {
            float t = elapsedTime / fadeTime;
            vignette.color.value = Color.Lerp(Color.red, Color.black, t);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 3단계: 완전 암전
        vignette.intensity.value = 1f;
        vignette.color.value = Color.black;
        
        Debug.Log("[VRPostProcessingManager] 💀 Game Over 암전 완료!");
        
        // Game Over UI 표시 이벤트 발생
        OnGameOverEffectComplete?.Invoke();
    }
    
    /// <summary>
    /// 효과 상태 설정
    /// </summary>
    public void SetEffectState(EffectState newState, float intensity = 1.0f)
    {
        currentState = newState;
        currentIntensity = intensity;
        
        Debug.Log($"[VRPostProcessingManager] 효과 상태 변경: {newState} (강도: {intensity})");
    }
} 