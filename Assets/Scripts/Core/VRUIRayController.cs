using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// VR 왼손 컨트롤러를 사용한 UI Ray 상호작용 시스템
/// Meta All-in-One SDK 환경에서 OVRInput을 활용
/// </summary>
public class VRUIRayController : MonoBehaviour
{
    [Header("Ray 설정")]
    [Tooltip("레이의 최대 거리")]
    public float rayLength = 10f;
    
    [Tooltip("UI 레이어 마스크")]
    public LayerMask uiLayerMask = -1;
    
    [Tooltip("레이가 시작될 Transform (보통 LeftHandAnchor)")]
    public Transform rayOrigin;
    
    [Header("시각적 설정")]
    [Tooltip("기본 레이저 색상")]
    public Color rayColorNormal = new Color(0f, 0.5f, 1f, 0.8f); // 파란색
    
    [Tooltip("UI 감지 시 레이저 색상")]
    public Color rayColorHover = new Color(0f, 1f, 0f, 0.8f); // 초록색
    
    [Tooltip("클릭 시 레이저 색상")]
    public Color rayColorClick = new Color(1f, 1f, 1f, 1f); // 흰색
    
    [Tooltip("레이저 두께")]
    public float rayWidth = 0.01f;
    
    [Header("입력 설정")]
    [Tooltip("클릭에 사용할 버튼")]
    public OVRInput.Button clickButton = OVRInput.Button.PrimaryIndexTrigger;
    
    [Tooltip("사용할 컨트롤러")]
    public OVRInput.Controller controller = OVRInput.Controller.LTouch;
    
    [Header("햅틱 설정")]
    [Tooltip("호버 시 햅틱 강도")]
    public float hoverHapticStrength = 0.1f;
    
    [Tooltip("클릭 시 햅틱 강도")]
    public float clickHapticStrength = 0.5f;
    
    [Tooltip("햅틱 지속 시간")]
    public float hapticDuration = 0.1f;
    
    // 컴포넌트 참조
    private LineRenderer lineRenderer;
    private Camera uiCamera;
    private EventSystem eventSystem;
    private GraphicRaycaster currentRaycaster;
    
    // Ray Casting 관련
    private PointerEventData pointerEventData;
    private List<RaycastResult> raycastResults = new List<RaycastResult>();
    
    // UI 상호작용 상태
    private GameObject currentUIObject;
    private Button currentButton;
    private bool isHoveringUI = false;
    private bool wasClickingLastFrame = false;
    
    // 햅틱 관리
    private Coroutine hapticCoroutine;
    
    void Start()
    {
        InitializeComponents();
        SetupLineRenderer();
        FindUIComponents();
    }
    
    void Update()
    {
        UpdateRayDirection();
        PerformUIRaycast();
        HandleControllerInput();
        UpdateVisualFeedback();
    }
    
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    void InitializeComponents()
    {
        // Ray Origin 자동 설정
        if (rayOrigin == null)
        {
            rayOrigin = transform;
            Debug.Log($"[VRUIRayController] Ray Origin을 {transform.name}으로 설정");
        }
        
        // Line Renderer 자동 추가
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            Debug.Log("[VRUIRayController] Line Renderer 자동 추가됨");
        }
    }
    
    /// <summary>
    /// Line Renderer 설정
    /// </summary>
    void SetupLineRenderer()
    {
        if (lineRenderer == null) return;
        
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        
        // LineRenderer 색상 설정 (startColor, endColor 사용)
        lineRenderer.startColor = rayColorNormal;
        lineRenderer.endColor = rayColorNormal;
        
        lineRenderer.startWidth = rayWidth;
        lineRenderer.endWidth = rayWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        
        // 기본 위치 설정
        lineRenderer.SetPosition(0, rayOrigin.position);
        lineRenderer.SetPosition(1, rayOrigin.position + rayOrigin.forward * rayLength);
        
        Debug.Log("[VRUIRayController] Line Renderer 설정 완료");
    }
    
    /// <summary>
    /// UI 컴포넌트들 찾기
    /// </summary>
    void FindUIComponents()
    {
        // Event System 찾기
        eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogWarning("[VRUIRayController] EventSystem을 찾을 수 없습니다!");
        }
        
        // UI Camera 찾기 (CenterEyeAnchor)
        FindUICamera();
        
        Debug.Log("[VRUIRayController] UI 컴포넌트 초기화 완료");
    }
    
    /// <summary>
    /// UI Camera 찾기 (VR 환경에서)
    /// </summary>
    void FindUICamera()
    {
        // CenterEyeAnchor Camera 우선 찾기
        GameObject centerEyeAnchor = GameObject.Find("CenterEyeAnchor");
        if (centerEyeAnchor != null)
        {
            uiCamera = centerEyeAnchor.GetComponent<Camera>();
            if (uiCamera != null)
            {
                Debug.Log($"[VRUIRayController] UI Camera 찾음: {uiCamera.name}");
                return;
            }
        }
        
        // Main Camera 사용
        uiCamera = Camera.main;
        if (uiCamera != null)
        {
            Debug.Log($"[VRUIRayController] Main Camera 사용: {uiCamera.name}");
        }
        else
        {
            Debug.LogError("[VRUIRayController] UI Camera를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// Ray 방향 업데이트
    /// </summary>
    void UpdateRayDirection()
    {
        if (lineRenderer == null || rayOrigin == null) return;
        
        Vector3 rayStart = rayOrigin.position;
        Vector3 rayEnd = rayStart + rayOrigin.forward * rayLength;
        
        lineRenderer.SetPosition(0, rayStart);
        lineRenderer.SetPosition(1, rayEnd);
    }
    
    /// <summary>
    /// UI Raycast 수행
    /// </summary>
    void PerformUIRaycast()
    {
        if (uiCamera == null || eventSystem == null) return;
        
        // 이전 상태 저장
        GameObject previousUIObject = currentUIObject;
        
        // Ray를 화면 좌표로 변환
        Vector3 rayStart = rayOrigin.position;
        Vector3 rayDirection = rayOrigin.forward;
        
        // 모든 Canvas에 대해 Raycast 수행
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        RaycastResult bestResult = new RaycastResult();
        float closestDistance = float.MaxValue;
        
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode != RenderMode.WorldSpace) continue;
            
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null) continue;
            
            // World Space Canvas에 대한 Ray 처리
            RaycastResult result = RaycastWorldSpaceCanvas(canvas, raycaster, rayStart, rayDirection);
            
            if (result.gameObject != null && result.distance < closestDistance)
            {
                closestDistance = result.distance;
                bestResult = result;
                currentRaycaster = raycaster;
            }
        }
        
        // 결과 처리
        if (bestResult.gameObject != null)
        {
            currentUIObject = bestResult.gameObject;
            currentButton = currentUIObject.GetComponent<Button>();
            
            // 새로운 UI 요소에 호버
            if (currentUIObject != previousUIObject)
            {
                OnUIHoverEnter();
            }
            
            isHoveringUI = true;
            
            // Ray 길이를 감지된 지점까지로 조정
            Vector3 hitPoint = rayStart + rayDirection * bestResult.distance;
            lineRenderer.SetPosition(1, hitPoint);
        }
        else
        {
            // UI에서 벗어남
            if (previousUIObject != null)
            {
                OnUIHoverExit();
            }
            
            currentUIObject = null;
            currentButton = null;
            isHoveringUI = false;
            
            // Ray 길이를 원래대로
            lineRenderer.SetPosition(1, rayStart + rayDirection * rayLength);
        }
    }
    
    /// <summary>
    /// World Space Canvas에 대한 Raycast
    /// </summary>
    RaycastResult RaycastWorldSpaceCanvas(Canvas canvas, GraphicRaycaster raycaster, Vector3 rayStart, Vector3 rayDirection)
    {
        // Canvas의 RectTransform을 평면으로 처리
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        
        // Ray와 Canvas 평면의 교차점 계산
        Plane canvasPlane = new Plane(-canvas.transform.forward, canvas.transform.position);
        
        if (canvasPlane.Raycast(new Ray(rayStart, rayDirection), out float distance))
        {
            Vector3 worldHitPoint = rayStart + rayDirection * distance;
            
            // World 좌표를 Canvas 로컬 좌표로 변환
            Vector3 localHitPoint = canvas.transform.InverseTransformPoint(worldHitPoint);
            
            // 로컬 좌표를 화면 좌표로 변환
            Vector2 screenPoint = new Vector2(
                (localHitPoint.x / canvasRect.rect.width + 0.5f) * Screen.width,
                (localHitPoint.y / canvasRect.rect.height + 0.5f) * Screen.height
            );
            
            // PointerEventData 생성
            if (pointerEventData == null)
            {
                pointerEventData = new PointerEventData(eventSystem);
            }
            
            pointerEventData.position = screenPoint;
            
            // Raycast 수행
            raycastResults.Clear();
            raycaster.Raycast(pointerEventData, raycastResults);
            
            if (raycastResults.Count > 0)
            {
                RaycastResult result = raycastResults[0];
                result.distance = distance; // 실제 거리 설정
                return result;
            }
        }
        
        return new RaycastResult();
    }
    
    /// <summary>
    /// 컨트롤러 입력 처리
    /// </summary>
    void HandleControllerInput()
    {
        bool isClicking = OVRInput.Get(clickButton, controller);
        bool clickStarted = OVRInput.GetDown(clickButton, controller);
        bool clickEnded = OVRInput.GetUp(clickButton, controller);
        
        // 클릭 시작
        if (clickStarted && isHoveringUI && currentButton != null)
        {
            OnButtonClick();
        }
        
        // 클릭 상태 저장
        wasClickingLastFrame = isClicking;
    }
    
    /// <summary>
    /// UI 호버 진입
    /// </summary>
    void OnUIHoverEnter()
    {
        // 호버 햅틱 피드백
        TriggerHapticFeedback(hoverHapticStrength, hapticDuration);
        
        // 버튼 하이라이트 (옵션)
        if (currentButton != null)
        {
            // 버튼 색상 변경 등의 시각적 피드백 추가 가능
        }
    }
    
    /// <summary>
    /// UI 호버 종료
    /// </summary>
    void OnUIHoverExit()
    {
        // 버튼 하이라이트 해제
        if (currentButton != null)
        {
            // 원래 색상으로 복구
        }
    }
    
    /// <summary>
    /// 버튼 클릭 처리
    /// </summary>
    void OnButtonClick()
    {
        if (currentButton == null) return;
        
        // 클릭 햅틱 피드백
        TriggerHapticFeedback(clickHapticStrength, hapticDuration * 2f);
        
        // 버튼 클릭 실행
        currentButton.onClick.Invoke();
        
        // 클릭 애니메이션 (옵션)
        StartCoroutine(ButtonClickAnimation());
    }
    
    /// <summary>
    /// 시각적 피드백 업데이트
    /// </summary>
    void UpdateVisualFeedback()
    {
        if (lineRenderer == null) return;
        
        Color targetColor;
        
        // 클릭 중일 때
        if (OVRInput.Get(clickButton, controller) && isHoveringUI)
        {
            targetColor = rayColorClick;
        }
        // 호버 중일 때
        else if (isHoveringUI)
        {
            targetColor = rayColorHover;
        }
        // 기본 상태
        else
        {
            targetColor = rayColorNormal;
        }
        
        // LineRenderer 색상 업데이트
        lineRenderer.startColor = targetColor;
        lineRenderer.endColor = targetColor;
    }
    
    /// <summary>
    /// 햅틱 피드백 트리거
    /// </summary>
    void TriggerHapticFeedback(float strength, float duration)
    {
        if (hapticCoroutine != null)
        {
            StopCoroutine(hapticCoroutine);
        }
        
        hapticCoroutine = StartCoroutine(HapticFeedbackCoroutine(strength, duration));
    }
    
    /// <summary>
    /// 햅틱 피드백 코루틴
    /// </summary>
    IEnumerator HapticFeedbackCoroutine(float strength, float duration)
    {
        OVRInput.SetControllerVibration(strength, strength, controller);
        yield return new WaitForSeconds(duration);
        OVRInput.SetControllerVibration(0f, 0f, controller);
    }
    
    /// <summary>
    /// 버튼 클릭 애니메이션
    /// </summary>
    IEnumerator ButtonClickAnimation()
    {
        if (currentButton == null) yield break;
        
        Transform buttonTransform = currentButton.transform;
        Vector3 originalScale = buttonTransform.localScale;
        
        // 축소
        float shrinkTime = 0.05f;
        float elapsed = 0f;
        
        while (elapsed < shrinkTime)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 0.95f, elapsed / shrinkTime);
            buttonTransform.localScale = originalScale * scale;
            yield return null;
        }
        
        // 원래 크기로 복구
        elapsed = 0f;
        while (elapsed < shrinkTime)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0.95f, 1f, elapsed / shrinkTime);
            buttonTransform.localScale = originalScale * scale;
            yield return null;
        }
        
        buttonTransform.localScale = originalScale;
    }
    
    /// <summary>
    /// 활성화/비활성화 상태 변경
    /// </summary>
    public void SetRayEnabled(bool enabled)
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = enabled;
        }
        
        this.enabled = enabled;
    }
    
    void OnDestroy()
    {
        // 햅틱 정리
        if (hapticCoroutine != null)
        {
            StopCoroutine(hapticCoroutine);
        }
        
        OVRInput.SetControllerVibration(0f, 0f, controller);
    }
    
    /// <summary>
    /// 디버그용 기즈모
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (rayOrigin != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 rayStart = rayOrigin.position;
            Vector3 rayEnd = rayStart + rayOrigin.forward * rayLength;
            Gizmos.DrawLine(rayStart, rayEnd);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(rayStart, 0.02f);
        }
    }
} 