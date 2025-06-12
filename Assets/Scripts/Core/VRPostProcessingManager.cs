using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// VR 호러 게임을 위한 Post Processing 기반 화면 효과 매니저
/// Universal RP의 Volume 시스템을 사용한 고품질 VR 피격 효과
/// </summary>
public class VRPostProcessingManager : MonoBehaviour
{
    [Header("VR Post Processing 설정")]
    [SerializeField] private bool enableEffects = true;
    [SerializeField] private float effectTransitionSpeed = 3.0f;
    
    [Header("피격 효과 설정")]
    [SerializeField] private float damageVignetteIntensity = 0.95f;
    [SerializeField] private float damageDuration = 1.5f;
    [SerializeField] private Color damageColor = Color.red;
    
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
    
    /// <summary>
    /// Game Over 효과 완료 이벤트
    /// </summary>
    public System.Action OnGameOverEffectComplete;
    
    private void Awake()
    {
        InitializePostProcessing();
    }
    
    private void Start()
    {
        ResetToNormalState();
        Debug.Log("[VRPostProcessingManager] ✅ Post Processing 기반 VR 효과 시스템 초기화 완료");
    }
    
    /// <summary>
    /// Post Processing 시스템 초기화
    /// </summary>
    private void InitializePostProcessing()
    {
        // Global Volume 찾기 또는 생성
        globalVolume = FindFirstObjectByType<Volume>();
        if (globalVolume == null)
        {
            GameObject volumeObj = new GameObject("Global Volume");
            globalVolume = volumeObj.AddComponent<Volume>();
            globalVolume.isGlobal = true;
            globalVolume.priority = 1;
        }
        
        // Volume Profile 생성 또는 가져오기
        if (globalVolume.profile == null)
        {
            volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            globalVolume.profile = volumeProfile;
        }
        else
        {
            volumeProfile = globalVolume.profile;
        }
        
        // Post Processing 효과들 설정
        SetupPostProcessingEffects();
        
        Debug.Log("[VRPostProcessingManager] Post Processing 초기화 완료");
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
        }
        
        if (bloom != null)
        {
            bloom.intensity.value = 0f;
        }
        
        Debug.Log("[VRPostProcessingManager] 정상 상태로 리셋 완료");
    }
    
    /// <summary>
    /// VR 플레이어 피격 시 강력한 빨간 화면 효과
    /// </summary>
    public void TriggerVRDamageEffect(float intensity = 0.8f, float duration = 1.5f)
    {
        Debug.Log($"[VRPostProcessingManager] 🔴 VR 피격 효과! 강도: {intensity}, 지속시간: {duration}초");
        StartCoroutine(VRDamageEffectCoroutine(intensity, duration));
    }
    
    /// <summary>
    /// VR 피격 효과 코루틴 (Post Processing 기반)
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
        vignette.intensity.value = damageVignetteIntensity;
        vignette.color.value = damageColor;
        
        // 색상 조정으로 빨간 필터 효과
        colorAdjustments.saturation.value = 50f; // 매우 높은 채도
        colorAdjustments.hueShift.value = -10f; // 빨간색 쪽으로 색조 이동
        
        // 블룸으로 강렬한 효과
        bloom.intensity.value = 0.8f;
        
        // 지속시간 대기
        yield return new WaitForSeconds(duration);
        
        // 점진적으로 원상복구
        float elapsedTime = 0f;
        float restoreDuration = 1.0f;
        
        float startVignette = vignette.intensity.value;
        float startSaturation = colorAdjustments.saturation.value;
        float startHueShift = colorAdjustments.hueShift.value;
        float startBloom = bloom.intensity.value;
        
        while (elapsedTime < restoreDuration)
        {
            float t = elapsedTime / restoreDuration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            vignette.intensity.value = Mathf.Lerp(startVignette, 0f, t);
            colorAdjustments.saturation.value = Mathf.Lerp(startSaturation, 0f, t);
            colorAdjustments.hueShift.value = Mathf.Lerp(startHueShift, 0f, t);
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