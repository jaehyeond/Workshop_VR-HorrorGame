using UnityEngine;

public static class PlayerDetectionSystem
{
    // 레이캐스트 설정 (정적으로 한 번만 생성)
    private static readonly Vector3[] rayHeightOffsets = {
        Vector3.up * 1.6f,  // 눈 높이
        Vector3.up * 1.0f,  // 가슴 높이
        Vector3.up * 0.5f   // 허리 높이
    };
    
    private static readonly Vector3[] playerTargetOffsets = {
        Vector3.up * 1.7f,  // 플레이어 머리
        Vector3.up * 1.0f,  // 플레이어 가슴
        Vector3.up * 0.5f   // 플레이어 허리
    };
    
    /// <summary>
    /// 최적화된 플레이어 가시성 체크
    /// </summary>
    public static bool CheckPlayerVisibility(Transform cultistTransform, Transform playerTransform, float detectionRange, bool enableDebug = false)
    {
        if (playerTransform == null) return false;
        
        Vector3 cultistPos = cultistTransform.position;
        Vector3 playerPos = playerTransform.position;
        
        // 1. 거리 체크 (가장 빠른 체크)
        float distanceToPlayer = Vector3.Distance(cultistPos, playerPos);
        if (distanceToPlayer > detectionRange) return false;
        
        // 2. 매니저를 통한 은신 상태 체크 (중복 제거)
        if (CultistManager.Instance != null && CultistManager.Instance.IsPlayerHiding())
        {
            if (enableDebug) Debug.Log($"[{cultistTransform.name}] 플레이어가 은신 중");
            return false;
        }
        
        // 3. 최적화된 레이캐스트 (단일 레이캐스트로 시작)
        return PerformOptimizedRaycast(cultistPos, playerPos, playerTransform, enableDebug, cultistTransform.name);
    }
    
    /// <summary>
    /// 최적화된 레이캐스트 (필요한 경우에만 다중 레이캐스트)
    /// </summary>
    private static bool PerformOptimizedRaycast(Vector3 cultistPos, Vector3 playerPos, Transform playerTransform, bool enableDebug, string cultistName)
    {
        // 먼저 중앙에서 중앙으로 단일 레이캐스트
        Vector3 rayStart = cultistPos + Vector3.up * 1.6f; // 눈 높이
        Vector3 rayTarget = playerPos + Vector3.up * 1.0f; // 플레이어 가슴
        Vector3 direction = (rayTarget - rayStart).normalized;
        float distance = Vector3.Distance(rayStart, rayTarget);
        
        RaycastHit hit;
        if (Physics.Raycast(rayStart, direction, out hit, distance + 0.1f))
        {
            // 플레이어를 직접 히트했다면 성공
            if (hit.transform == playerTransform || hit.transform.IsChildOf(playerTransform))
            {
                if (enableDebug) 
                {
                    // Debug.Log($"[{cultistName}] 플레이어 감지 성공 (단일 레이캐스트)");
                    // Debug.DrawRay(rayStart, direction * distance, Color.green, 0.1f);
                }
                return true;
            }
            
            // 장애물에 막혔다면 다중 레이캐스트 시도
            if (enableDebug)
            {
                // Debug.Log($"[{cultistName}] 시야선이 {hit.transform.name}에 의해 차단됨 - 다중 레이캐스트 시도");
                // Debug.DrawRay(rayStart, direction * hit.distance, Color.red, 0.1f);
            }
            
            return PerformMultipleRaycasts(cultistPos, playerPos, playerTransform, enableDebug, cultistName);
        }
        
        // 아무것도 히트하지 않았다면 (이상한 경우)
        if (enableDebug)
        {
            Debug.Log($"[{cultistName}] 레이캐스트가 아무것도 히트하지 않음");
            Debug.DrawRay(rayStart, direction * distance, Color.yellow, 0.1f);
        }
        
        return false;
    }
    
    /// <summary>
    /// 다중 레이캐스트 (단일 레이캐스트가 실패했을 때만)
    /// </summary>
    private static bool PerformMultipleRaycasts(Vector3 cultistPos, Vector3 playerPos, Transform playerTransform, bool enableDebug, string cultistName)
    {
        // 3x3 = 9개 조합 중에서 몇 개만 체크
        for (int i = 0; i < rayHeightOffsets.Length; i++)
        {
            for (int j = 0; j < playerTargetOffsets.Length; j++)
            {
                Vector3 rayStart = cultistPos + rayHeightOffsets[i];
                Vector3 rayTarget = playerPos + playerTargetOffsets[j];
                Vector3 direction = (rayTarget - rayStart).normalized;
                float distance = Vector3.Distance(rayStart, rayTarget);
                
                RaycastHit hit;
                if (Physics.Raycast(rayStart, direction, out hit, distance + 0.1f))
                {
                    if (hit.transform == playerTransform || hit.transform.IsChildOf(playerTransform))
                    {
                        if (enableDebug)
                        {
                            // Debug.Log($"[{cultistName}] 플레이어 감지 성공 (다중 레이캐스트 {i}-{j})");
                            // Debug.DrawRay(rayStart, direction * distance, Color.green, 0.1f);
                        }
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 거리 기반 감지 범위 조정
    /// </summary>
    public static float GetAdjustedDetectionRange(float baseRange, Vector3 cultistPos, Vector3 playerPos)
    {
        float distance = Vector3.Distance(cultistPos, playerPos);
        
        // 가까우면 감지 범위 증가 (더 민감하게)
        if (distance < 5f) return baseRange * 1.2f;
        
        // 멀면 감지 범위 감소 (성능 최적화)
        if (distance > 15f) return baseRange * 0.8f;
        
        return baseRange;
    }
    
    /// <summary>
    /// 시야각 체크 (선택적 사용)
    /// </summary>
    public static bool IsInFieldOfView(Transform cultistTransform, Transform playerTransform, float fieldOfViewAngle = 120f)
    {
        Vector3 directionToPlayer = (playerTransform.position - cultistTransform.position).normalized;
        Vector3 cultistForward = cultistTransform.forward;
        
        float angle = Vector3.Angle(cultistForward, directionToPlayer);
        return angle <= fieldOfViewAngle * 0.5f;
    }
} 