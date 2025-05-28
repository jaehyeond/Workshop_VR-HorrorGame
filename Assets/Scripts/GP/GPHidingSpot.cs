using UnityEngine;

/// <summary>
/// GP 게임의 특정 은신 지점을 정의하는 컴포넌트
/// </summary>
public class GPHidingSpot : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField] private string spotName = "은신 지점";
    [SerializeField] private string playerTag = "Player";
    
    [Header("시각적 요소")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(0, 1, 0, 0.3f);
    
    [Header("사운드")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip enterSound;
    [SerializeField] private AudioClip exitSound;
    
    // 내부 상태
    private bool playerIsInside = false;
    private VRLocomotion playerLocomotion;
    
    private void Start()
    {
        // 필수 컴포넌트 확인
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"은신 지점 '{gameObject.name}'에 Collider가 없습니다. Box Collider를 추가하고 'Is Trigger'를 체크하세요.");
        }
        else if (!GetComponent<Collider>().isTrigger)
        {
            Debug.LogWarning($"은신 지점 '{gameObject.name}'의 Collider에 'Is Trigger'가 체크되어 있지 않습니다.");
        }
        
        // 오디오 소스 참조
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerIsInside = true;
            playerLocomotion = other.GetComponentInParent<VRLocomotion>();
            
            // UI에 은신 지점 진입 알림
            if (GPGameManager.Instance != null && GPGameManager.Instance.UIManager != null)
            {
                GPGameManager.Instance.UIManager.ShowInteractionPrompt($"{spotName}에 숨을 수 있습니다", true);
            }
            
            // 진입 사운드 재생
            if (audioSource != null && enterSound != null)
            {
                audioSource.PlayOneShot(enterSound);
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerIsInside = false;
            
            // UI에서 은신 지점 알림 제거
            if (GPGameManager.Instance != null && GPGameManager.Instance.UIManager != null)
            {
                GPGameManager.Instance.UIManager.ShowInteractionPrompt("", false);
                GPGameManager.Instance.UIManager.UpdateHidingIndicator(false);
            }
            
            // 나가는 사운드 재생
            if (audioSource != null && exitSound != null)
            {
                audioSource.PlayOneShot(exitSound);
            }
            
            playerLocomotion = null;
        }
    }
    
    private void Update()
    {
        // 플레이어가 은신 지점 안에 있고, 로코모션이 있는 경우
        if (playerIsInside && playerLocomotion != null)
        {
            // 플레이어가 은신 상태인지 확인하고 UI 업데이트
            bool isHiding = playerLocomotion.IsHiding();
            
            if (GPGameManager.Instance != null && GPGameManager.Instance.UIManager != null)
            {
                GPGameManager.Instance.UIManager.UpdateHidingIndicator(isHiding);
            }
        }
    }
    
    /// <summary>
    /// 디버깅용 시각화
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGizmo) return;
        
        Gizmos.color = gizmoColor;
        
        // 콜라이더 기반 시각화
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            if (col is BoxCollider)
            {
                BoxCollider box = col as BoxCollider;
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.matrix = oldMatrix;
            }
            else if (col is SphereCollider)
            {
                SphereCollider sphere = col as SphereCollider;
                Gizmos.DrawSphere(transform.TransformPoint(sphere.center), sphere.radius * transform.lossyScale.x);
            }
            else if (col is CapsuleCollider)
            {
                // 캡슐 콜라이더는 단순화해서 표시
                CapsuleCollider capsule = col as CapsuleCollider;
                Vector3 size = new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2);
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(capsule.center, size);
                Gizmos.matrix = oldMatrix;
            }
        }
        else
        {
            // 콜라이더가 없는 경우 기본 큐브로 표시
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
        
        // 은신 지점 이름 표시
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, spotName);
        #endif
    }
} 