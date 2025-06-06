using UnityEngine;
using Oculus.Interaction.HandGrab;

public class AxeWeapon : MonoBehaviour
{
    [Header("타격 설정 - 게임 디자인 문서 기준")]
    public float baseDamage = 1f; // 기본 1타격 (타격 횟수 기반)
    public float attackCooldown = 1f;
    public LayerMask enemyLayer = -1;
    
    [Header("타격 감지")]
    public Transform axeHead; // 도끼날 부분
    public float hitRadius = 0.3f;
    public float minSwingVelocity = 2f; // 최소 휘두르기 속도
    
    [Header("효과")]
    public AudioClip hitSound;
    public AudioClip criticalHitSound; // 치명타 사운드
    public ParticleSystem hitEffect;
    public ParticleSystem bloodEffect; // 피 효과
    
    [Header("햅틱 피드백 설정")]
    public float normalHitVibration = 0.6f;
    public float criticalHitVibration = 1.0f;
    public float vibrationDuration = 0.2f;
    
    private Rigidbody axeRigidbody;
    private AudioSource audioSource;
    private float lastAttackTime;
    private Vector3 lastPosition;
    private bool isGrabbed = false;
    private HandGrabInteractable handGrabInteractable;
    
    private void Start()
    {
        axeRigidbody = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        
        if (axeHead == null)
            axeHead = transform;
            
        lastPosition = transform.position;
        
        // HandGrab 이벤트 연결
        handGrabInteractable = GetComponent<HandGrabInteractable>();
        if (handGrabInteractable != null)
        {
            handGrabInteractable.WhenSelectingInteractorAdded.Action += OnGrabbed;
            handGrabInteractable.WhenSelectingInteractorRemoved.Action += OnReleased;
        }
    }
    
    private void Update()
    {
        if (isGrabbed)
        {
            CheckForHit();
        }
        
        lastPosition = transform.position;
    }
    
    private void OnGrabbed(HandGrabInteractor interactor)
    {
        isGrabbed = true;
        Debug.Log("[AxeWeapon] 도끼를 잡았습니다!");
    }
    
    private void OnReleased(HandGrabInteractor interactor)
    {
        isGrabbed = false;
        Debug.Log("[AxeWeapon] 도끼를 놓았습니다!");
    }
    
    private void CheckForHit()
    {
        // 쿨다운 체크
        if (Time.time - lastAttackTime < attackCooldown)
            return;
            
        // 휘두르기 속도 체크
        Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
        float swingSpeed = velocity.magnitude;
        
        if (swingSpeed < minSwingVelocity)
            return;
            
        // 타격 감지
        Collider[] hitColliders = Physics.OverlapSphere(axeHead.position, hitRadius, enemyLayer);
        
        foreach (Collider hitCollider in hitColliders)
        {
            // 광신도 타격 처리
            CultistAI cultist = hitCollider.GetComponent<CultistAI>();
            if (cultist != null)
            {
                HitCultist(cultist, swingSpeed);
                lastAttackTime = Time.time;
                break; // 한 번에 하나씩만 타격
            }
        }
    }
    
    private void HitCultist(CultistAI cultist, float swingSpeed)
    {
        // 기본 50 데미지 (기존 체력 시스템 사용)
        float finalDamage = 50f;
        
        // 치명타 판정 (높은 속도로 휘둘렀을 때)
        bool isCriticalHit = swingSpeed > minSwingVelocity * 2f;
        if (isCriticalHit)
        {
            finalDamage *= 1.5f; // 치명타는 1.5배 데미지
        }
        
        // 광신도에게 데미지 전달
        cultist.TakeDamage(finalDamage, transform.position);
        
        // 기본 효과 재생
        PlayHitEffects(isCriticalHit);
        
        // 햅틱 피드백
        TriggerHapticFeedback(isCriticalHit);
        
        string hitType = isCriticalHit ? "치명타" : "일반 타격";
        Debug.Log($"[AxeWeapon] {cultist.name}에게 {hitType}! 데미지: {finalDamage}");
    }
    
    private void PlayHitEffects(bool isCriticalHit)
    {
        // 사운드 재생
        if (audioSource != null)
        {
            AudioClip soundToPlay = isCriticalHit && criticalHitSound != null ? criticalHitSound : hitSound;
            if (soundToPlay != null)
            {
                audioSource.PlayOneShot(soundToPlay, 0.8f);
            }
        }
        
        // 파티클 효과
        if (hitEffect != null)
        {
            hitEffect.transform.position = axeHead.position;
            hitEffect.Play();
        }
        
        // 피 효과 (치명타일 때)
        if (isCriticalHit && bloodEffect != null)
        {
            bloodEffect.transform.position = axeHead.position;
            bloodEffect.Play();
        }
    }
    
    private void TriggerHapticFeedback(bool isCriticalHit)
    {
        // Meta XR 햅틱 피드백 - 최신 API 사용
        if (handGrabInteractable != null && isGrabbed)
        {
            // 치명타 여부에 따른 진동 강도 조절
            float vibrationStrength = isCriticalHit ? criticalHitVibration : normalHitVibration;
            
            // 최신 Meta XR SDK에서는 OVRInput을 직접 사용
            // 양손 컨트롤러 모두에 진동 적용
            OVRInput.SetControllerVibration(vibrationStrength, vibrationStrength, OVRInput.Controller.LTouch);
            OVRInput.SetControllerVibration(vibrationStrength, vibrationStrength, OVRInput.Controller.RTouch);
            
            // 진동 지속 시간 후 정지
            Invoke(nameof(StopVibration), vibrationDuration);
        }
    }
    
    private void StopVibration()
    {
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (axeHead != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(axeHead.position, hitRadius);
        }
    }
} 