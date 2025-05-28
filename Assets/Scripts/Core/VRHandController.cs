using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// VR 손 컨트롤러와 상호작용을 관리하는 컴포넌트
/// Meta XR SDK의 손 컨트롤러 기능과 연동하여 작동합니다.
/// </summary>
public class VRHandController : MonoBehaviour
{
    [Header("컨트롤러 설정")]
    [SerializeField] private XRNode handNode = XRNode.RightHand;
    [SerializeField] private Transform handTransform;
    [SerializeField] private Animator handAnimator;
    
    [Header("손가락 애니메이션")]
    [SerializeField] private string gripAnimationParameter = "Grip";
    [SerializeField] private string triggerAnimationParameter = "Trigger";
    [SerializeField] private string thumbAnimationParameter = "Thumb";
    
    private InputDevice handDevice;
    private List<InputDevice> handDevices = new List<InputDevice>();
    private float gripValue;
    private float triggerValue;
    private bool thumbValue;
    
    private void Start()
    {
        InitializeHandDevice();
    }
    
    private void Update()
    {
        if (!handDevice.isValid)
        {
            InitializeHandDevice();
            return;
        }
        
        // 컨트롤러 입력 업데이트
        UpdateControllerInputs();
        
        // 손 애니메이션 업데이트
        UpdateHandAnimation();
    }
    
    private void InitializeHandDevice()
    {
        handDevices.Clear();
        InputDevices.GetDevicesAtXRNode(handNode, handDevices);
        
        if (handDevices.Count > 0)
        {
            handDevice = handDevices[0];
        }
        else
        {
            Debug.LogWarning($"{handNode} 컨트롤러를 찾을 수 없습니다.");
        }
    }
    
    private void UpdateControllerInputs()
    {
        // Grip 버튼 값 (아날로그)
        if (!handDevice.TryGetFeatureValue(CommonUsages.grip, out gripValue))
        {
            gripValue = 0f;
        }
        
        // Trigger 버튼 값 (아날로그)
        if (!handDevice.TryGetFeatureValue(CommonUsages.trigger, out triggerValue))
        {
            triggerValue = 0f;
        }
        
        // 엄지 버튼 상태 (디지털)
        bool primaryTouchValue = false;
        handDevice.TryGetFeatureValue(CommonUsages.primaryTouch, out primaryTouchValue);
        
        bool secondaryTouchValue = false;
        handDevice.TryGetFeatureValue(CommonUsages.secondaryTouch, out secondaryTouchValue);
        
        // 엄지가 어느 버튼에든 닿아있으면 활성화
        thumbValue = primaryTouchValue || secondaryTouchValue;
    }
    
    private void UpdateHandAnimation()
    {
        if (handAnimator != null)
        {
            // 손가락 애니메이션 파라미터 설정
            handAnimator.SetFloat(gripAnimationParameter, gripValue);
            handAnimator.SetFloat(triggerAnimationParameter, triggerValue);
            handAnimator.SetBool(thumbAnimationParameter, thumbValue);
        }
    }
    
    /// <summary>
    /// 진동 피드백 제공
    /// </summary>
    public void ProvideTactileFeedback(float intensity, float duration)
    {
        if (handDevice.isValid)
        {
            handDevice.SendHapticImpulse(0, intensity, duration);
        }
    }
    
    /// <summary>
    /// 현재 Grip 값 반환
    /// </summary>
    public float GetGripValue()
    {
        return gripValue;
    }
    
    /// <summary>
    /// 현재 Trigger 값 반환
    /// </summary>
    public float GetTriggerValue()
    {
        return triggerValue;
    }
} 