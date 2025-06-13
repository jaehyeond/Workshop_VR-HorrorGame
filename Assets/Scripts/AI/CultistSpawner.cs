using UnityEngine;
using System.Collections.Generic;

public class CultistSpawner : MonoBehaviour
{
    [Header("Enemy 스폰 설정")]
    public GameObject enemyPrefab;
    public Transform[] enemyPrayingSpots;
    public bool spawnEnemiesOnStart = true;
    
    [Header("Boss 스폰 설정")]
    public GameObject bossPrefab;
    public Transform[] bossPrayingSpots;
    public bool spawnBossOnStart = true;
    
    [Header("디버그")]
    public bool enableDebugLogs = true;
    
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private List<GameObject> spawnedBosses = new List<GameObject>();
    
    void Start()
    {
        if (spawnEnemiesOnStart)
        {
            SpawnAllEnemies();
        }
        
        if (spawnBossOnStart)
        {
            SpawnAllBosses();
        }
    }
    
    [ContextMenu("Spawn All Enemies")]
    public void SpawnAllEnemies()
    {
        // 기존 스폰된 Enemy들 제거
        ClearAllEnemies();
        
        if (enemyPrefab == null)
        {
            Debug.LogError("[CultistSpawner] Enemy Prefab이 설정되지 않았습니다!");
            return;
        }
        
        if (enemyPrayingSpots == null || enemyPrayingSpots.Length == 0)
        {
            Debug.LogError("[CultistSpawner] Enemy Praying Spots이 설정되지 않았습니다!");
            return;
        }
        
        // 각 Enemy 기도 위치에 Enemy 스폰
        for (int i = 0; i < enemyPrayingSpots.Length; i++)
        {
            if (enemyPrayingSpots[i] != null)
            {
                SpawnEnemyAt(enemyPrayingSpots[i], i);
            }
        }
        
        DebugLog($"총 {spawnedEnemies.Count}명의 Enemy를 스폰했습니다.");
    }
    
    [ContextMenu("Spawn All Bosses")]
    public void SpawnAllBosses()
    {
        // 기존 스폰된 Boss들 제거
        ClearAllBosses();
        
        if (bossPrefab == null)
        {
            Debug.LogError("[CultistSpawner] Boss Prefab이 설정되지 않았습니다!");
            return;
        }
        
        if (bossPrayingSpots == null || bossPrayingSpots.Length == 0)
        {
            Debug.LogError("[CultistSpawner] Boss Praying Spots이 설정되지 않았습니다!");
            return;
        }
        
        // 각 Boss 기도 위치에 Boss 스폰
        for (int i = 0; i < bossPrayingSpots.Length; i++)
        {
            if (bossPrayingSpots[i] != null)
            {
                SpawnBossAt(bossPrayingSpots[i], i);
            }
        }
        
        DebugLog($"총 {spawnedBosses.Count}명의 Boss를 스폰했습니다.");
    }
    
    [ContextMenu("Spawn All Characters")]
    public void SpawnAllCharacters()
    {
        SpawnAllEnemies();
        SpawnAllBosses();
    }
    
    void SpawnEnemyAt(Transform prayingSpot, int index)
    {
        // 기도 위치에서 약간 떨어진 곳에 스폰
        Vector3 spawnPosition = prayingSpot.position + Vector3.back * 0.5f;
        
        // Enemy 생성
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, prayingSpot.rotation);
        newEnemy.name = $"Fanatic Enemy {index + 1}";
        
        // CultistAI 컴포넌트에 기도 위치 설정
        CultistAI cultistAI = newEnemy.GetComponent<CultistAI>();
        if (cultistAI != null)
        {
            cultistAI.prayingSpot = prayingSpot;
            DebugLog($"Enemy {newEnemy.name}을 {prayingSpot.name}에 배치했습니다.");
        }
        else
        {
            Debug.LogError($"[CultistSpawner] {newEnemy.name}에 CultistAI 컴포넌트가 없습니다!");
        }
        
        // 스폰된 리스트에 추가
        spawnedEnemies.Add(newEnemy);
    }
    
    void SpawnBossAt(Transform prayingSpot, int index)
    {
        // 기도 위치에서 약간 떨어진 곳에 스폰
        Vector3 spawnPosition = prayingSpot.position + Vector3.back * 1.0f; // Boss는 더 멀리 스폰
        
        // Boss 생성
        GameObject newBoss = Instantiate(bossPrefab, spawnPosition, prayingSpot.rotation);
        newBoss.name = $"Boss {index + 1}";
        
        // BossAI 컴포넌트에 기도 위치 설정 (만약 필요하다면)
        BossAI bossAI = newBoss.GetComponent<BossAI>();
        if (bossAI != null)
        {
            // Boss는 특정 기도 위치에 고정되지 않을 수 있으므로 선택적으로 설정
            DebugLog($"Boss {newBoss.name}을 {prayingSpot.name}에 배치했습니다.");
        }
        else
        {
            Debug.LogWarning($"[CultistSpawner] {newBoss.name}에 BossAI 컴포넌트가 없습니다. 일반 GameObject로 스폰됩니다.");
        }
        
        // 스폰된 리스트에 추가
        spawnedBosses.Add(newBoss);
    }
    
    [ContextMenu("Clear All Enemies")]
    public void ClearAllEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(enemy);
                }
                else
                {
                    DestroyImmediate(enemy);
                }
            }
        }
        
        spawnedEnemies.Clear();
        DebugLog("모든 Enemy를 제거했습니다.");
    }
    
    [ContextMenu("Clear All Bosses")]
    public void ClearAllBosses()
    {
        foreach (GameObject boss in spawnedBosses)
        {
            if (boss != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(boss);
                }
                else
                {
                    DestroyImmediate(boss);
                }
            }
        }
        
        spawnedBosses.Clear();
        DebugLog("모든 Boss를 제거했습니다.");
    }
    
    [ContextMenu("Clear All Characters")]
    public void ClearAllCharacters()
    {
        ClearAllEnemies();
        ClearAllBosses();
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
        // Enemy Praying Spots 시각화
        if (enemyPrayingSpots != null)
        {
            for (int i = 0; i < enemyPrayingSpots.Length; i++)
            {
                if (enemyPrayingSpots[i] != null)
                {
                    // Enemy 기도 위치 표시 (파란색)
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(enemyPrayingSpots[i].position, 0.5f);
                    
                    // Enemy 스폰 위치 표시 (초록색)
                    Vector3 enemySpawnPos = enemyPrayingSpots[i].position + Vector3.back * 0.5f;
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(enemySpawnPos, Vector3.one * 0.3f);
                    
                    // 연결선 (노란색)
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(enemyPrayingSpots[i].position, enemySpawnPos);
                }
            }
        }
        
        // Boss Praying Spots 시각화
        if (bossPrayingSpots != null)
        {
            for (int i = 0; i < bossPrayingSpots.Length; i++)
            {
                if (bossPrayingSpots[i] != null)
                {
                    // Boss 기도 위치 표시 (빨간색)
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(bossPrayingSpots[i].position, 0.8f);
                    
                    // Boss 스폰 위치 표시 (마젠타색)
                    Vector3 bossSpawnPos = bossPrayingSpots[i].position + Vector3.back * 1.0f;
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireCube(bossSpawnPos, Vector3.one * 0.5f);
                    
                    // 연결선 (주황색)
                    Gizmos.color = new Color(1f, 0.5f, 0f); // 주황색
                    Gizmos.DrawLine(bossPrayingSpots[i].position, bossSpawnPos);
                }
            }
        }
    }
}