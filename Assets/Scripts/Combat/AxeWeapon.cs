using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AxeWeapon : MonoBehaviour
{
    [Header("타격 설정 - 게임 디자인 문서 기준")]
    public float baseDamage = 50f;
    public float attackCooldown = 1f;
    public LayerMask enemyLayer = -1; // 모든 레이어 (디버그용)
    
    [Header("타격 감지")]
    public Transform axeHead; // 도끼날 부분
    public float hitRadius = 3.0f; // 2.0f에서 3.0f로 더욱 확대
    public float minSwingVelocity = 2f; // 최소 휘두르기 속도
    
    [Header("컨트롤러 설정")]
    public OVRInput.Button equipButton = OVRInput.Button.PrimaryHandTrigger; // Grip 버튼으로 복원
    public OVRInput.Button attackButton = OVRInput.Button.PrimaryIndexTrigger; // 공격 버튼
    public OVRInput.Controller controllerType = OVRInput.Controller.RTouch; // 오른손 컨트롤러
    
    [Header("장착 설정")]
    public Transform attachPoint; // 실제 손이 잡을 위치 (손잡이 중간)
    public Transform handAnchor; // 손 위치 (OVRCameraRig의 RightHandAnchor)
    public Vector3 equipOffset = new Vector3(0, 0, 0); // 추가 미세 조정
    public Vector3 equipRotation = new Vector3(0, 0, 0); // 추가 회전 조정
    
    [Header("효과")]
    public AudioClip hitSound;
    public AudioClip criticalHitSound; // 치명타 사운드
    public AudioClip equipSound; // 장착 사운드
    public AudioClip unequipSound; // 해제 사운드
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
    private bool isEquipped = false;
    // private bool wasEquipButtonPressed = false;
    // private bool wasAttackButtonPressed = false;
    
    // 원래 위치 저장 (장착 해제 시 복귀용)
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;
    
    private void Start()
    {
        
        
        // AttachPoint 자동 찾기
        if (attachPoint == null)
        {
            attachPoint = transform.Find("AttachPoint");
            if (attachPoint == null)
            {
                Debug.LogWarning("[AxeWeapon] AttachPoint가 없습니다. 기본 위치 사용.");
            }
        }
        
        // 원래 위치와 회전 저장
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        
        // Rigidbody 컴포넌트 가져오기
        axeRigidbody = GetComponent<Rigidbody>();
        if (axeRigidbody == null)
        {
            Debug.LogError("[AxeWeapon] Rigidbody 컴포넌트가 없습니다!");
        }
        
        // Hand Anchor 자동 찾기
        if (handAnchor == null)
        {
            // 방법 1: OVRCameraRig 찾기 (가장 정확한 방법)
            GameObject cameraRig = GameObject.Find("OVRCameraRig");
            if (cameraRig != null)
            {
                Transform rightHand = cameraRig.transform.Find("TrackingSpace/RightHandAnchor");
                if (rightHand != null)
                {
                    handAnchor = rightHand;
                }
                else
                {
                    // 다른 경로 시도
                    rightHand = cameraRig.transform.Find("RightHandAnchor");
                    if (rightHand != null)
                    {
                        handAnchor = rightHand;
                    }
                }
            }
            
            // 방법 2: Building Block 구조 찾기 (현재 씬 구조)
            if (handAnchor == null)
            {
                // PlayerController나 Camera Rig 찾기
                GameObject playerController = GameObject.Find("PlayerController");
                if (playerController != null)
                {
                    // Building Block 구조에서 RightHandAnchor 찾기
                    Transform rightHand = playerController.transform.Find("Camera Rig/TrackingSpace/RightHandAnchor");
                    if (rightHand == null)
                        rightHand = playerController.transform.Find("TrackingSpace/RightHandAnchor");
                    if (rightHand == null)
                        rightHand = playerController.transform.Find("RightHandAnchor");
                    
                    if (rightHand != null)
                    {
                        handAnchor = rightHand;
                    }
                }
            }
            
            // 방법 3: 태그 기반 찾기 (더 안전한 방법)
            if (handAnchor == null)
            {
                GameObject[] handObjects = GameObject.FindGameObjectsWithTag("Player");
                foreach (GameObject obj in handObjects)
                {
                    Transform rightHand = obj.transform.Find("RightHandAnchor");
                    if (rightHand == null)
                    {
                        // 재귀적으로 찾기
                        rightHand = FindChildByName(obj.transform, "RightHandAnchor");
                    }
                    
                    if (rightHand != null)
                    {
                        handAnchor = rightHand;
                        break;
                    }
                }
            }
            
            // 방법 4: 마지막 수단 - 메인 카메라 사용
            if (handAnchor == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    handAnchor = mainCamera.transform;
                    Debug.LogWarning("[AxeWeapon] Hand Anchor를 찾을 수 없어서 메인 카메라를 임시 사용!");
                }
                else
                {
                    Debug.LogError("[AxeWeapon] Hand Anchor를 전혀 찾을 수 없습니다!");
                }
            }
        }
        
        lastPosition = transform.position;
    }
    
    private void Update()
    {
      
        
        // 장착/해제 버튼 확인
        if (OVRInput.GetDown(equipButton, controllerType))
        {
            //Debug.Log("[AxeWeapon] A버튼 눌림 감지!");
            ToggleEquip();
        }

        // 공격 버튼 확인 (장착된 상태에서만)
        if (isEquipped && OVRInput.GetDown(attackButton, controllerType))
        {
            //Debug.Log("[AxeWeapon] 공격 버튼 눌림!");
            Attack();
        }
        
        lastPosition = transform.position;
    }
    
    private void ToggleEquip()
    {
        if (isEquipped)
        {
            UnequipAxe();
        }
        else
        {
            EquipAxe();
        }
    }
    
    private void EquipAxe()
    {
        if (handAnchor == null)
        {
            //Debug.LogError("[AxeWeapon] Hand Anchor가 설정되지 않았습니다!");
            return;
        }

        isEquipped = true;
        
                // AttachPoint를 사용한 정확한 장착
        if (attachPoint != null)
        {
            // 도끼 전체를 손에 붙이되
            transform.SetParent(handAnchor);
            
            // AttachPoint가 손 위치에 오도록 오프셋 계산
            Vector3 offset = transform.position - attachPoint.position;
            transform.position = handAnchor.position + offset;
            
            // 회전도 AttachPoint 기준으로 조정
            Quaternion rotOffset = Quaternion.Inverse(attachPoint.localRotation);
            transform.rotation = handAnchor.rotation * rotOffset;
            
            // 추가 미세 조정
            transform.localPosition += equipOffset;
            transform.localRotation *= Quaternion.Euler(equipRotation);
        }
        else
        {
            //Debug.LogWarning("[AxeWeapon] AttachPoint가 없어서 기본 방식 사용");
            
            // AttachPoint가 없으면 기존 방식
            transform.SetParent(handAnchor);
            transform.localPosition = equipOffset;
            transform.localRotation = Quaternion.Euler(equipRotation);
        }
        
        // 물리 비활성화 (손에 고정)
        if (axeRigidbody != null)
        {
            axeRigidbody.isKinematic = true;
        }
        
        // 햅틱 피드백
        OVRInput.SetControllerVibration(0.3f, 0.3f, controllerType);
        StartCoroutine(StopVibration(0.2f));
    }
    
    private void UnequipAxe()
    {
        isEquipped = false;
        
        // 물리 활성화
        if (axeRigidbody != null)
        {
            axeRigidbody.isKinematic = false;
        }
        
        // 원래 위치로 복귀
        transform.SetParent(originalParent);
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        
        // 사운드 재생
        if (audioSource != null && unequipSound != null)
        {
            audioSource.PlayOneShot(unequipSound);
        }
        
        //Debug.Log("[AxeWeapon] 도끼 해제!");
    }
    
    private void Attack()
    {
        // 쿨다운 체크
        if (Time.time - lastAttackTime < attackCooldown)
        {
            return;
        }
            
        // 휘두르기 속도 체크
        Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
        float swingSpeed = velocity.magnitude;
        
        // 최소 속도 체크 (컨트롤러 기반이므로 더 관대하게)
        if (swingSpeed < minSwingVelocity * 0.5f)
        {
            swingSpeed = minSwingVelocity; // 기본 속도 보장
        }
        
        // 광범위 타격 감지 (Enemy 전체를 감싸도록)
        Vector3[] attackPoints = {
            transform.position,                           // 손 위치
            transform.position + transform.forward * 1.0f,  // 멀리 앞쪽
            transform.position + transform.right * 0.8f,    // 오른쪽
            transform.position - transform.right * 0.8f,    // 왼쪽
            transform.position + transform.up * 0.5f,       // 위쪽
            transform.position - transform.up * 0.3f        // 아래쪽
        };
        
        List<Collider> allHitColliders = new List<Collider>();
        
        // 각 지점에서 타격 감지
        foreach (Vector3 point in attackPoints)
        {
            Collider[] pointHits = Physics.OverlapSphere(point, hitRadius, enemyLayer);
            foreach (Collider hit in pointHits)
            {
                if (!allHitColliders.Contains(hit))
                {
                    allHitColliders.Add(hit);
                }
            }
        }
        
        Collider[] hitColliders = allHitColliders.ToArray();
        
        foreach (Collider hitCollider in hitColliders)
        {
            // 광신도 타격 처리
            CultistAI cultist = hitCollider.GetComponent<CultistAI>();
            if (cultist != null)
            {
                // Debug.Log($"[AxeWeapon] ===== 광신도 발견: {cultist.name} =====");
                // Debug.Log($"[AxeWeapon] HitCultist 호출 전!");
                HitCultist(cultist, swingSpeed);
                //Debug.Log($"[AxeWeapon] HitCultist 호출 후!");
                lastAttackTime = Time.time;
                break; // 한 번에 하나씩만 타격
            }
            else
            {
                //Debug.Log($"[AxeWeapon] {hitCollider.name}에는 CultistAI 컴포넌트가 없음");
            }
        }
        
        // 공격 시 항상 햅틱 피드백 (타격 여부와 관계없이)
        TriggerAttackFeedback();
    }
    
    private void HitCultist(CultistAI cultist, float swingSpeed)
    {
        // 기본 50 데미지 (기존 체력 시스템 사용)
        float finalDamage = baseDamage;
        
        // 치명타 판정 (높은 속도로 휘둘렀을 때)
        bool isCriticalHit = swingSpeed > minSwingVelocity * 1.5f;
        if (isCriticalHit)
        {
            finalDamage *= 1.5f; // 치명타는 1.5배 데미지
        }
        
        //Debug.Log($"[AxeWeapon] 최종 데미지: {finalDamage}, TakeDamage 호출 전!");
        
        // 광신도에게 데미지 전달
        cultist.TakeDamage(finalDamage, transform.position);
        
        //Debug.Log($"[AxeWeapon] TakeDamage 호출 완료!");
        
        // 기본 효과 재생
        PlayHitEffects(isCriticalHit);
        
        // 햅틱 피드백
        TriggerHapticFeedback(isCriticalHit);
        
        string hitType = isCriticalHit ? "치명타" : "일반 타격";
        //Debug.Log($"[AxeWeapon] {cultist.name}에게 {hitType}! 데미지: {finalDamage}");
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
    
    private void TriggerAttackFeedback()
    {
        // 공격 시 기본 햅틱 피드백
        OVRInput.SetControllerVibration(0.4f, 0.4f, controllerType);
        Invoke(nameof(StopVibration), 0.1f);
    }
    
    private void TriggerHapticFeedback(bool isCriticalHit)
    {
        // 타격 성공 시 강한 햅틱 피드백
        float vibrationStrength = isCriticalHit ? criticalHitVibration : normalHitVibration;
        
        OVRInput.SetControllerVibration(vibrationStrength, vibrationStrength, controllerType);
        Invoke(nameof(StopVibration), vibrationDuration);
    }
    
    private IEnumerator StopVibration(float delay)
    {
        yield return new WaitForSeconds(delay);
        OVRInput.SetControllerVibration(0f, 0f, controllerType);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (axeHead != null)
        {
            Gizmos.color = isEquipped ? Color.green : Color.red;
            Gizmos.DrawWireSphere(axeHead.position, hitRadius);
        }
    }
    
    // 재귀적으로 자식 오브젝트에서 이름으로 찾기
    private Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            
            Transform found = FindChildByName(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

} 