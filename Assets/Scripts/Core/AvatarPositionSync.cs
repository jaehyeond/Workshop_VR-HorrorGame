using UnityEngine;

// /// <summary>
// /// 플레이어 컨트롤러와 아바타 사이의 위치를 동기화하는 컴포넌트
// /// </summary>
public class AvatarPositionSync : MonoBehaviour
{
//        [Header("References")]
    public Transform playerController;
    
    [Header("Offset Settings")]
    public Vector3 positionOffset = Vector3.zero;
    
    [Header("Advanced")]
    [Tooltip("위치 동기화 사용 여부")]
    public bool syncPosition = true;
    
    [Tooltip("회전 동기화 사용 여부")]
    public bool syncRotation = true;
    
    [Tooltip("리타게팅 시스템이 초기화된 후 동기화 시작 (초)")]
    public float startDelay = 0.5f;
    
    private bool initialized = false;
    
    void Start()
    {
        // 지연 초기화 (리타게팅 시스템이 준비되도록)
        Invoke("InitializeSync", startDelay);
    }
    
    void InitializeSync()
    {
        if (playerController == null)
        {
            Debug.LogWarning("PlayerController가 설정되지 않았습니다.");
            return;
        }
        
        // 초기 위치 설정
        if (syncPosition)
        {
            Vector3 initialPos = playerController.position + positionOffset;
            transform.position = initialPos;
        }
        
        initialized = true;
        Debug.Log("아바타 동기화 초기화 완료");
    }
    
    void LateUpdate()
    {
        if (!initialized || playerController == null) return;
        
        // 위치 동기화
        if (syncPosition)
        {
            // 현재 Y 높이 유지 (바닥 관통 방지)
            Vector3 targetPos = playerController.position + positionOffset;
            transform.position = targetPos;
        }
        
        // 회전 동기화 (Y축만)
        if (syncRotation)
        {
            // Y축 회전만 동기화 (리타게팅 시스템에 영향 최소화)
            Vector3 currentRot = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(
                currentRot.x,
                playerController.eulerAngles.y,
                currentRot.z
            );
        }
    }
} 