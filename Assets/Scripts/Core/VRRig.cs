using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// VR 플레이어의 기본 리그를 관리하는 컴포넌트
/// 헤드셋, 컨트롤러 등의 XR 장치와 캐릭터 위치를 동기화합니다.
/// </summary>
public class VRRig : MonoBehaviour
{
    [Header("XR 장치 참조")]
    [SerializeField] private Transform headsetTransform;
    [SerializeField] private Transform leftControllerTransform;
    [SerializeField] private Transform rightControllerTransform;
    
    [Header("아바타 참조")]
    [SerializeField] private Transform headAnchor;
    [SerializeField] private Transform leftHandAnchor;
    [SerializeField] private Transform rightHandAnchor;
    
    [Header("설정")]
    [SerializeField] private float playerHeight = 1.7f;
    [SerializeField] private Vector3 headPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 headRotationOffset = Vector3.zero;
    
    // 내부 참조
    private Camera xrCamera;
    private VRHandController leftHandController;
    private VRHandController rightHandController;
    
    private void Start()
    {
        InitializeRig();
    }
    
    private void InitializeRig()
    {
        // 헤드셋 카메라 참조 획득
        if (headsetTransform != null)
        {
            xrCamera = headsetTransform.GetComponent<Camera>();
        }
        
        // 왼손/오른손 컨트롤러 참조 획득
        if (leftControllerTransform != null)
        {
            leftHandController = leftControllerTransform.GetComponent<VRHandController>();
        }
        
        if (rightControllerTransform != null)
        {
            rightHandController = rightControllerTransform.GetComponent<VRHandController>();
        }
        
        // XR 원점 초기화
        if (XRSettings.isDeviceActive)
        {
            SetInitialPosition();
        }
    }
    
    private void Update()
    {
        UpdateHeadPosition();
        UpdateHandPositions();
    }
    
    /// <summary>
    /// 헤드셋 위치/회전 업데이트
    /// </summary>
    private void UpdateHeadPosition()
    {
        if (headsetTransform != null && headAnchor != null)
        {
            // 헤드 앵커 위치/회전 업데이트
            headAnchor.position = headsetTransform.position + headPositionOffset;
            headAnchor.rotation = headsetTransform.rotation * Quaternion.Euler(headRotationOffset);
        }
    }
    
    /// <summary>
    /// 컨트롤러 위치/회전 업데이트
    /// </summary>
    private void UpdateHandPositions()
    {
        // 왼손 업데이트
        if (leftControllerTransform != null && leftHandAnchor != null)
        {
            leftHandAnchor.position = leftControllerTransform.position;
            leftHandAnchor.rotation = leftControllerTransform.rotation;
        }
        
        // 오른손 업데이트
        if (rightControllerTransform != null && rightHandAnchor != null)
        {
            rightHandAnchor.position = rightControllerTransform.position;
            rightHandAnchor.rotation = rightControllerTransform.rotation;
        }
    }
    
    /// <summary>
    /// 플레이어 위치 초기화
    /// </summary>
    private void SetInitialPosition()
    {
        if (headsetTransform != null)
        {
            // XR 헤드셋의 초기 높이 적용
            float headsetYOffset = headsetTransform.localPosition.y;
            
            // 플레이어의 키를 기준으로 스케일 조정
            if (headsetYOffset > 0.1f) // 헤드셋 위치가 유효한 경우
            {
                float scale = playerHeight / headsetYOffset;
                transform.localScale = new Vector3(scale, scale, scale);
            }
        }
    }
    
    /// <summary>
    /// 헤드셋 Transform 참조 반환
    /// </summary>
    public Transform GetHeadTransform()
    {
        return headsetTransform;
    }
    
    /// <summary>
    /// 왼손 컨트롤러 Transform 참조 반환
    /// </summary>
    public Transform GetLeftControllerTransform()
    {
        return leftControllerTransform;
    }
    
    /// <summary>
    /// 오른손 컨트롤러 Transform 참조 반환
    /// </summary>
    public Transform GetRightControllerTransform()
    {
        return rightControllerTransform;
    }
    
    /// <summary>
    /// 카메라 참조 반환
    /// </summary>
    public Camera GetXRCamera()
    {
        return xrCamera;
    }
    
    /// <summary>
    /// 촉각 피드백 제공 (컨트롤러 진동)
    /// </summary>
    /// <param name="intensity">진동 강도 (0-1)</param>
    /// <param name="duration">진동 지속 시간 (초)</param>
    public void ProvideTactileFeedback(float intensity, float duration)
    {
        // 양쪽 컨트롤러에 진동 피드백 전달
        if (leftHandController != null)
        {
            leftHandController.ProvideTactileFeedback(intensity, duration);
        }
        
        if (rightHandController != null)
        {
            rightHandController.ProvideTactileFeedback(intensity * 0.7f, duration);
        }
    }
} 