using UnityEngine;
using System.Collections.Generic;

public class CultistSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    public GameObject cultistPrefab;
    public Transform[] prayingSpots;
    public bool spawnOnStart = true;
    
    [Header("디버그")]
    public bool enableDebugLogs = true;
    
    private List<GameObject> spawnedCultists = new List<GameObject>();
    
    void Start()
    {
        if (spawnOnStart)
        {
            SpawnAllCultists();
        }
    }
    
    [ContextMenu("Spawn All Cultists")]
    public void SpawnAllCultists()
    {
        // 기존 스폰된 광신도들 제거
        ClearAllCultists();
        
        if (cultistPrefab == null)
        {
            Debug.LogError("[CultistSpawner] Cultist Prefab이 설정되지 않았습니다!");
            return;
        }
        
        if (prayingSpots == null || prayingSpots.Length == 0)
        {
            Debug.LogError("[CultistSpawner] Praying Spots이 설정되지 않았습니다!");
            return;
        }
        
        // 각 기도 위치에 광신도 스폰
        for (int i = 0; i < prayingSpots.Length; i++)
        {
            if (prayingSpots[i] != null)
            {
                SpawnCultistAt(prayingSpots[i], i);
            }
        }
        
        DebugLog($"총 {spawnedCultists.Count}명의 광신도를 스폰했습니다.");
    }
    
    void SpawnCultistAt(Transform prayingSpot, int index)
    {
        // 기도 위치에서 약간 떨어진 곳에 스폰
        Vector3 spawnPosition = prayingSpot.position + Vector3.back * 0.5f;
        
        // 광신도 생성
        GameObject newCultist = Instantiate(cultistPrefab, spawnPosition, prayingSpot.rotation);
        newCultist.name = $"Fanatic Enemy {index + 1}";
        
        // CultistAI 컴포넌트에 기도 위치 설정
        CultistAI cultistAI = newCultist.GetComponent<CultistAI>();
        if (cultistAI != null)
        {
            cultistAI.prayingSpot = prayingSpot;
            DebugLog($"광신도 {newCultist.name}을 {prayingSpot.name}에 배치했습니다.");
        }
        else
        {
            Debug.LogError($"[CultistSpawner] {newCultist.name}에 CultistAI 컴포넌트가 없습니다!");
        }
        
        // 스폰된 리스트에 추가
        spawnedCultists.Add(newCultist);
    }
    
    [ContextMenu("Clear All Cultists")]
    public void ClearAllCultists()
    {
        foreach (GameObject cultist in spawnedCultists)
        {
            if (cultist != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(cultist);
                }
                else
                {
                    DestroyImmediate(cultist);
                }
            }
        }
        
        spawnedCultists.Clear();
        DebugLog("모든 광신도를 제거했습니다.");
    }
    
    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[CultistSpawner] {message}");
        }
    }
    
    // 에디터에서 기도 위치 시각화
    void OnDrawGizmos()
    {
        if (prayingSpots == null) return;
        
        for (int i = 0; i < prayingSpots.Length; i++)
        {
            if (prayingSpots[i] != null)
            {
                // 기도 위치 표시
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(prayingSpots[i].position, 0.5f);
                
                // 스폰 위치 표시
                Vector3 spawnPos = prayingSpots[i].position + Vector3.back * 0.5f;
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(spawnPos, Vector3.one * 0.3f);
                
                // 연결선
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(prayingSpots[i].position, spawnPos);
            }
        }
    }
} 