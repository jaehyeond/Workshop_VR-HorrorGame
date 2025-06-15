using UnityEngine;

/// <summary>
/// Scene 전환 트리거 - 플레이어가 접촉하면 특정 Video Scene으로 전환
/// GameFlowManager와 연동하여 작동
/// </summary>
public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("=== Trigger Type ===")]
    [SerializeField] private TriggerType triggerType = TriggerType.BossIntroVideo;
    
    [Header("=== Trigger Settings ===")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool requiresCondition = true;
    
    public enum TriggerType
    {
        BossIntroVideo,     // 보스 인트로 영상으로 전환
        EndingVideo         // 엔딩 영상으로 전환
    }

    private bool hasTriggered = false;

    #region Unity Events

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[SceneTransitionTrigger] 트리거 진입: {other.name} (태그: {other.tag})");
        
        if (hasTriggered) 
        {
            Debug.Log($"[SceneTransitionTrigger] 이미 트리거됨 - 무시");
            return;
        }
        
        if (IsValidPlayer(other))
        {
            Debug.Log($"[SceneTransitionTrigger] 유효한 플레이어 감지: {other.name}");
            
            // 조건 확인
            if (requiresCondition && !CheckCondition())
            {
                Debug.Log($"[SceneTransitionTrigger] 조건 미충족: {triggerType}");
                return;
            }
            
            Debug.Log($"[SceneTransitionTrigger] 트리거 실행: {triggerType}");
            
            hasTriggered = true;
            ExecuteTrigger();
        }
        else
        {
            Debug.Log($"[SceneTransitionTrigger] 유효하지 않은 플레이어: {other.name}");
        }
    }

    #endregion

    #region Player Detection

    bool IsValidPlayer(Collider other)
    {
        // 태그 확인
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
        {
            // VR 플레이어는 보통 OVRCameraRig나 하위 컴포넌트에 있음
            Transform parent = other.transform.parent;
            while (parent != null)
            {
                if (parent.CompareTag(playerTag))
                {
                    return true;
                }
                parent = parent.parent;
            }
            
            // 일반적인 VR 컴포넌트 확인
            if (other.GetComponent<OVRCameraRig>() != null ||
                other.GetComponentInParent<OVRCameraRig>() != null)
            {
                return true;
            }
            
            return false;
        }

        return true;
    }

    #endregion

    #region Condition Check

    bool CheckCondition()
    {
        if (GameFlowManager.Instance == null) 
        {
            Debug.LogError("[SceneTransitionTrigger] GameFlowManager.Instance가 null입니다!");
            return false;
        }

        switch (triggerType)
        {
            case TriggerType.BossIntroVideo:
                // 인트로를 봤고, 아직 보스 인트로를 보지 않았을 때
                bool hasSeenIntro = GameFlowManager.Instance.HasSeenIntro;
                bool hasSeenBossIntro = GameFlowManager.Instance.HasSeenBossIntro;
                bool condition = hasSeenIntro && !hasSeenBossIntro;
                
                Debug.Log($"[SceneTransitionTrigger] BossIntroVideo 조건 체크:");
                Debug.Log($"  - HasSeenIntro: {hasSeenIntro}");
                Debug.Log($"  - HasSeenBossIntro: {hasSeenBossIntro}");
                Debug.Log($"  - 조건 결과: {condition}");
                
                return condition;
                
            case TriggerType.EndingVideo:
                // 보스가 처치되었고, 아직 엔딩을 보지 않았을 때
                bool isBossDefeated = GameFlowManager.Instance.IsBossDefeated;
                bool hasSeenEnding = GameFlowManager.Instance.HasSeenEnding;
                bool endingCondition = isBossDefeated && !hasSeenEnding;
                
                Debug.Log($"[SceneTransitionTrigger] EndingVideo 조건 체크:");
                Debug.Log($"  - IsBossDefeated: {isBossDefeated}");
                Debug.Log($"  - HasSeenEnding: {hasSeenEnding}");
                Debug.Log($"  - 조건 결과: {endingCondition}");
                
                return endingCondition;
                
            default:
                return true;
        }
    }

    #endregion

    #region Trigger Execution

    void ExecuteTrigger()
    {
        if (GameFlowManager.Instance == null)
        {
            Debug.LogError("[SceneTransitionTrigger] GameFlowManager를 찾을 수 없습니다!");
            return;
        }

        switch (triggerType)
        {
            case TriggerType.BossIntroVideo:
                GameFlowManager.Instance.TriggerBossIntroVideo();
                break;
                
            case TriggerType.EndingVideo:
                GameFlowManager.Instance.TriggerEndingVideo();
                break;
        }
    }

    #endregion

    #region Editor Helpers

    void OnDrawGizmos()
    {
        // 트리거 영역 시각화
        Color gizmoColor = triggerType == TriggerType.BossIntroVideo ? Color.red : Color.green;
        if (hasTriggered) gizmoColor = Color.gray;
        
        Gizmos.color = gizmoColor;
        
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            if (collider is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(Vector3.zero, box.size);
            }
            else if (collider is SphereCollider sphere)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireSphere(Vector3.zero, sphere.radius);
            }
        }
    }

    #endregion

    #region Public Methods

    public void ResetTrigger()
    {
        hasTriggered = false;
        Debug.Log($"[SceneTransitionTrigger] {triggerType} 트리거 리셋");
    }

    #endregion
} 