using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
// XR Interaction Toolkit이 없는 경우를 위한 대체 구현

/// <summary>
/// VR 플레이어의 이동을 담당하는 컴포넌트
/// </summary>
public class VRLocomotion : MonoBehaviour
{
    // InputHelpers.Button 대체를 위한 열거형 정의
    public enum VRButton
    {
        None,
        Primary2DAxisClick,
        PrimaryButton,
        SecondaryButton,
        Trigger,
        Grip
    }

    [Header("컨트롤러 입력")]
    [SerializeField] private XRNode movementSource = XRNode.LeftHand;
    [SerializeField] private XRNode rotationSource = XRNode.RightHand;
    [SerializeField] private List<VRButton> moveButtons = new List<VRButton> { VRButton.Primary2DAxisClick };
    [SerializeField] private List<VRButton> sprintButtons = new List<VRButton> { VRButton.PrimaryButton };
    
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float sprintMultiplier = 2.0f;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.1f;
    
    [Header("회전 설정")]
    [SerializeField] private float rotationSpeed = 90f; // 초당 회전 각도
    [SerializeField] private float rotationDeadzone = 0.1f; // 조이스틱 데드존
    [SerializeField] private bool smoothRotation = true; // 부드러운 회전 여부
    
    [Header("은신 설정")]
    [SerializeField] private float crouchThreshold = 1.0f;  // 이 높이 이하로 내려가면 숨기 상태로 인식
    [SerializeField] private VRRig vrRig;  // VRRig 참조

    [Header("입력 모드 설정")]
    [SerializeField] private bool allowHandTrackingMode = true; // 핸드 트래킹 모드 허용 여부
    [SerializeField] private float controllerCheckInterval = 1.0f; // 컨트롤러 상태 확인 주기 (초)
    [SerializeField] private GameObject controllerModeUI; // 컨트롤러 모드 UI (선택 사항)
    [SerializeField] private GameObject handTrackingModeUI; // 핸드 트래킹 모드 UI (선택 사항)
    
    private CharacterController characterController;
    private InputDevice movementController;
    private InputDevice rotationController;
    private Transform cameraTransform;
    private Transform cameraRigTransform;
    private Vector3 moveDirection = Vector3.zero;
    private bool isGrounded = false;
    private float verticalVelocity = 0f;
    private bool isCrouching = false;
    private float originalHeight;
    
    // 모드 전환 관련 변수
    private bool isInHandTrackingMode = false;
    private float lastControllerCheckTime = 0f;
    private bool hasLoggedControllerWarning = false;
    private bool leftControllerWasConnected = false;
    private bool rightControllerWasConnected = false;
    
    private void Awake()
    {
        // 컴포넌트 참조 캐싱
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("VRLocomotion 컴포넌트에 CharacterController가 필요합니다.");
            enabled = false;
            return;
        }
        
        // 카메라 및 카메라 리그 찾기
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            cameraRigTransform = cameraTransform.parent;
        }
        else
        {
            Debug.LogError("메인 카메라를 찾을 수 없습니다.");
            enabled = false;
            return;
        }
        
        // VRRig 참조 확인
        if (vrRig == null)
        {
            vrRig = GetComponent<VRRig>();
            if (vrRig == null)
            {
                // FindObjectOfType 대신 현대적인 방식으로 VRRig 찾기
                vrRig = GameObject.FindFirstObjectByType<VRRig>();
                
                if (vrRig == null)
                {
                    Debug.LogWarning("VRRig를 찾을 수 없습니다.");
                }
            }
        }
        
        // 원래 캐릭터 컨트롤러 높이 저장
        originalHeight = characterController.height;
        
        // UI 초기 상태 설정
        if (controllerModeUI != null) controllerModeUI.SetActive(true);
        if (handTrackingModeUI != null) handTrackingModeUI.SetActive(false);
    }
    
    private void Start()
    {
        // 초기 컨트롤러 상태 확인
        CheckControllerStatus();
    }
    
    private void Update()
    {
        // 주기적으로만 컨트롤러 상태 확인 (성능 최적화)
        if (Time.time > lastControllerCheckTime + controllerCheckInterval)
        {
            CheckControllerStatus();
            lastControllerCheckTime = Time.time;
        }
        
        // 지면 체크 (모든 모드에서 필요)
        CheckGrounded();
        
        // 핸드 트래킹 모드가 아닐 때만 컨트롤러 입력 처리
        if (!isInHandTrackingMode)
        {
            // 움직임 입력 처리
            HandleMovementInput();
            
            // 회전 입력 처리
            HandleRotationInput();
        }
        else
        {
            // 핸드 트래킹 모드일 때의 이동 처리 (필요한 경우 구현)
            HandleHandTrackingMovement();
        }
        
        // 은신(앉기) 상태 체크 (모든 모드에서 필요)
        CheckCrouchState();
        
        // 캐릭터 이동 적용 (모든 모드에서 필요)
        ApplyMovement();
    }
    
    private void InitializeController(XRNode node, out InputDevice controller)
    {
        var inputDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(node, inputDevices);
        if (inputDevices.Count > 0)
        {
            controller = inputDevices[0];
        }
        else
        {
            // 로그 메시지 억제 (핸드 트래킹 모드로 전환되므로 경고 불필요)
            controller = new InputDevice();
        }
    }
    
    private void CheckControllerStatus()
    {
        bool leftControllerConnected = false;
        bool rightControllerConnected = false;
        
        // 왼쪽 컨트롤러 확인
        var leftDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftDevices);
        leftControllerConnected = leftDevices.Count > 0 && leftDevices[0].isValid;
        
        // 오른쪽 컨트롤러 확인
        var rightDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightDevices);
        rightControllerConnected = rightDevices.Count > 0 && rightDevices[0].isValid;
        
        // 모드 전환 결정
        bool shouldBeInHandTrackingMode = allowHandTrackingMode && 
                                         (!leftControllerConnected || !rightControllerConnected);
        
        // 컨트롤러 연결 상태가 변경되었을 때만 메시지 표시
        if (leftControllerConnected != leftControllerWasConnected || 
            rightControllerConnected != rightControllerWasConnected)
        {
            if (!leftControllerConnected)
                Debug.Log("왼쪽 컨트롤러 연결이 해제되었습니다.");
            else if (leftControllerConnected && !leftControllerWasConnected)
                Debug.Log("왼쪽 컨트롤러가 연결되었습니다.");
                
            if (!rightControllerConnected)
                Debug.Log("오른쪽 컨트롤러 연결이 해제되었습니다.");
            else if (rightControllerConnected && !rightControllerWasConnected)
                Debug.Log("오른쪽 컨트롤러가 연결되었습니다.");
                
            leftControllerWasConnected = leftControllerConnected;
            rightControllerWasConnected = rightControllerConnected;
        }
        
        // 모드가 변경되었을 때만 처리
        if (isInHandTrackingMode != shouldBeInHandTrackingMode)
        {
            isInHandTrackingMode = shouldBeInHandTrackingMode;
            
            if (isInHandTrackingMode)
            {
                EnterHandTrackingMode();
            }
            else
            {
                ExitHandTrackingMode();
                // 컨트롤러 다시 초기화
                InitializeController(movementSource, out movementController);
                InitializeController(rotationSource, out rotationController);
            }
        }
        
        // 핸드 트래킹 모드가 아닐 때만 컨트롤러 재연결 시도
        if (!isInHandTrackingMode)
        {
            if (!movementController.isValid)
                InitializeController(movementSource, out movementController);
                
            if (!rotationController.isValid)
                InitializeController(rotationSource, out rotationController);
        }
    }
    
    private void EnterHandTrackingMode()
    {
        // 핸드 트래킹 모드 진입 시 처리
        Debug.Log("핸드 트래킹 모드로 전환합니다.");
        
        // UI 상태 업데이트
        if (controllerModeUI != null) controllerModeUI.SetActive(false);
        if (handTrackingModeUI != null) handTrackingModeUI.SetActive(true);
        
        // 이동 방향 초기화
        moveDirection = Vector3.zero;
    }
    
    private void ExitHandTrackingMode()
    {
        // 컨트롤러 모드 복귀 시 처리
        Debug.Log("컨트롤러 모드로 전환합니다.");
        
        // UI 상태 업데이트
        if (controllerModeUI != null) controllerModeUI.SetActive(true);
        if (handTrackingModeUI != null) handTrackingModeUI.SetActive(false);
    }
    
    private void HandleHandTrackingMovement()
    {
        // 핸드 트래킹 모드에서의 이동 처리
        // 필요한 경우 여기에 구현 (예: 시선 방향 이동, 손 제스처 기반 이동 등)
        
        // 기본 구현: 이동 없음 (정적 상태)
        moveDirection = Vector3.zero;
        
        // 참고: 실제 핸드 트래킹 기반 이동을 구현하려면 해당 기기의 SDK 사용 필요
        // (Oculus, SteamVR, Microsoft Mixed Reality 등)
    }
    
    private void CheckGrounded()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
        
        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -0.1f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }
    
    private void HandleMovementInput()
    {
        // 컨트롤러에서 조이스틱 입력 가져오기
        Vector2 joystickInput = Vector2.zero;
        if (movementController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 position))
        {
            joystickInput = position;
        }
        
        // 움직임 버튼이 눌려있는지 확인
        bool isMoving = false;
        foreach (var button in moveButtons)
        {
            if (IsButtonPressed(movementController, button))
            {
                isMoving = true;
                break;
            }
        }
        
        // 달리기 버튼이 눌려있는지 확인
        bool isSprinting = false;
        if (isMoving)
        {
            foreach (var button in sprintButtons)
            {
                if (IsButtonPressed(movementController, button))
                {
                    isSprinting = true;
                    break;
                }
            }
        }
        
        // 카메라 기준 방향으로 이동 벡터 계산
        moveDirection = Vector3.zero;
        if (isMoving && joystickInput.magnitude > 0.1f)
        {
            // 카메라 방향 기준으로 이동 계산 (높이는 무시)
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();
            
            moveDirection = camForward * joystickInput.y + camRight * joystickInput.x;
            
            // 이동 속도 결정
            float currentSpeed = moveSpeed;
            
            // 달리기 상태면 속도 증가
            if (isSprinting)
            {
                currentSpeed *= sprintMultiplier;
            }
            
            // 은신(앉기) 상태면 속도 감소
            if (isCrouching)
            {
                currentSpeed *= crouchSpeedMultiplier;
            }
            
            moveDirection *= currentSpeed;
        }
    }
    
    private void HandleRotationInput()
    {
        // 회전 컨트롤러에서 조이스틱 입력 가져오기
        if (rotationController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rotationInput))
        {
            // X축 입력으로 회전 (좌우)
            float horizontalInput = rotationInput.x;
            
            // 데드존 이상의 입력만 처리
            if (Mathf.Abs(horizontalInput) > rotationDeadzone)
            {
                // 회전 각도 계산 (초당 rotationSpeed만큼 회전)
                float rotationAmount = horizontalInput * rotationSpeed * Time.deltaTime;
                
                // 플레이어 회전 적용 (카메라 리그를 회전)
                transform.Rotate(0, rotationAmount, 0);
            }
        }
    }
    
    // InputHelpers 대신 사용할 버튼 상태 확인 메서드
    private bool IsButtonPressed(InputDevice device, VRButton button)
    {
        if (!device.isValid)
            return false;
            
        switch (button)
        {
            case VRButton.Primary2DAxisClick:
                return device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool primary2DAxisClick) && primary2DAxisClick;
            
            case VRButton.PrimaryButton:
                return device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButton) && primaryButton;
            
            case VRButton.SecondaryButton:
                return device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryButton) && secondaryButton;
            
            case VRButton.Trigger:
                return device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerButton) && triggerButton;
            
            case VRButton.Grip:
                return device.TryGetFeatureValue(CommonUsages.gripButton, out bool gripButton) && gripButton;
            
            default:
                return false;
        }
    }
    
    private void CheckCrouchState()
    {
        if (cameraTransform == null) return;
        
        // 헤드셋의 실제 높이 확인 (로컬 Y 위치)
        float headsetLocalHeight = cameraTransform.localPosition.y;
        
        // 은신 상태 감지
        bool shouldBeCrouching = headsetLocalHeight <= crouchThreshold;
        
        // 상태 변경 감지
        if (shouldBeCrouching != isCrouching)
        {
            isCrouching = shouldBeCrouching;
            
            // 은신 상태에 따른 캐릭터 컨트롤러 높이 조정
            if (isCrouching)
            {
                characterController.height = originalHeight * 0.6f;
                characterController.center = new Vector3(0, -0.2f, 0);
                
                // 필요시 VRRig에 은신 상태 알림
                if (vrRig != null)
                {
                    // 진동 피드백 제공
                    vrRig.ProvideTactileFeedback(0.3f, 0.2f);
                }
                
                Debug.Log("은신 상태 진입");
            }
            else
            {
                characterController.height = originalHeight;
                characterController.center = Vector3.zero;
                Debug.Log("은신 상태 종료");
            }
        }
    }
    
    private void ApplyMovement()
    {
        // 수직 이동 적용 (중력)
        Vector3 movement = moveDirection + new Vector3(0, verticalVelocity, 0);
        
        // 캐릭터 컨트롤러 이동
        characterController.Move(movement * Time.deltaTime);
    }
    
    /// <summary>
    /// 현재 은신 상태 여부 반환
    /// </summary>
    public bool IsHiding()
    {
        return isCrouching;
    }
    
    /// <summary>
    /// 현재 핸드 트래킹 모드 여부 반환
    /// </summary>
    public bool IsInHandTrackingMode()
    {
        return isInHandTrackingMode;
    }
    
    /// <summary>
    /// 핸드 트래킹 모드를 수동으로 설정
    /// </summary>
    public void SetHandTrackingMode(bool enable)
    {
        if (enable != isInHandTrackingMode)
        {
            isInHandTrackingMode = enable;
            
            if (isInHandTrackingMode)
            {
                EnterHandTrackingMode();
            }
            else
            {
                ExitHandTrackingMode();
                // 컨트롤러 다시 초기화
                InitializeController(movementSource, out movementController);
                InitializeController(rotationSource, out rotationController);
            }
        }
    }
} 