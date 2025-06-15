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
    
    [Header("Daughter 스폰 설정")]
    public GameObject daughterPrefab;
    public Transform[] daughterSpots;
    public bool spawnDaughterOnStart = true;
    
    [Header("디버그")]
    public bool enableDebugLogs = true;
    
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private List<GameObject> spawnedBosses = new List<GameObject>();
    private List<GameObject> spawnedDaughters = new List<GameObject>();
    
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
        
        if (spawnDaughterOnStart)
        {
            SpawnAllDaughters();
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
    
    [ContextMenu("Spawn All Daughters")]
    public void SpawnAllDaughters()
    {
        // 기존 스폰된 Daughter들 제거
        ClearAllDaughters();
        
        if (daughterPrefab == null)
        {
            Debug.LogError("[CultistSpawner] Daughter Prefab이 설정되지 않았습니다!");
            return;
        }
        
        if (daughterSpots == null || daughterSpots.Length == 0)
        {
            Debug.LogError("[CultistSpawner] Daughter Spots이 설정되지 않았습니다!");
            return;
        }
        
        // 각 Daughter 위치에 Daughter 스폰
        for (int i = 0; i < daughterSpots.Length; i++)
        {
            if (daughterSpots[i] != null)
            {
                SpawnDaughterAt(daughterSpots[i], i);
            }
        }
        
        DebugLog($"총 {spawnedDaughters.Count}명의 Daughter를 스폰했습니다.");
    }
    
    [ContextMenu("Spawn All Characters")]
    public void SpawnAllCharacters()
    {
        SpawnAllEnemies();
        SpawnAllBosses();
        SpawnAllDaughters();
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
    
    void SpawnDaughterAt(Transform daughterSpot, int index)
    {
        // Daughter 위치에 정확히 스폰
        Vector3 spawnPosition = daughterSpot.position;
        
        // Daughter 생성
        GameObject newDaughter = Instantiate(daughterPrefab, spawnPosition, daughterSpot.rotation);
        newDaughter.name = $"Daughter {index + 1}";
        
        // 딸 구출 트리거 자동 추가
        SetupDaughterRescueTrigger(newDaughter);
        
        DebugLog($"Daughter {newDaughter.name}을 {daughterSpot.name}에 배치했습니다.");
        
        // 스폰된 리스트에 추가
        spawnedDaughters.Add(newDaughter);
    }
    
    private void SetupDaughterRescueTrigger(GameObject daughter)
    {
        // DaughterRescueTrigger 컴포넌트 추가
        DaughterRescueTrigger rescueTrigger = daughter.GetComponent<DaughterRescueTrigger>();
        if (rescueTrigger == null)
        {
            rescueTrigger = daughter.AddComponent<DaughterRescueTrigger>();
        }
        
        // Collider가 없으면 추가
        Collider daughterCollider = daughter.GetComponent<Collider>();
        if (daughterCollider == null)
        {
            SphereCollider sphereCollider = daughter.AddComponent<SphereCollider>();
            sphereCollider.radius = 2f; // VR에서 접근하기 쉬운 크기
            sphereCollider.isTrigger = true;
        }
        else if (!daughterCollider.isTrigger)
        {
            // 기존 Collider가 있지만 트리거가 아니면 추가 트리거 생성
            SphereCollider triggerCollider = daughter.AddComponent<SphereCollider>();
            triggerCollider.radius = 2f;
            triggerCollider.isTrigger = true;
        }
        
        DebugLog($"딸 구출 트리거 설정 완료: {daughter.name}");
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
    
    [ContextMenu("Clear All Daughters")]
    public void ClearAllDaughters()
    {
        foreach (GameObject daughter in spawnedDaughters)
        {
            if (daughter != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(daughter);
                }
                else
                {
                    DestroyImmediate(daughter);
                }
            }
        }
        
        spawnedDaughters.Clear();
        DebugLog("모든 Daughter를 제거했습니다.");
    }
    
    [ContextMenu("Clear All Characters")]
    public void ClearAllCharacters()
    {
        ClearAllEnemies();
        ClearAllBosses();
        ClearAllDaughters();
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
        
        // Daughter Spots 시각화
        if (daughterSpots != null)
        {
            for (int i = 0; i < daughterSpots.Length; i++)
            {
                if (daughterSpots[i] != null)
                {
                    // Daughter 위치 표시 (보라색)
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(daughterSpots[i].position, 0.6f);
                    
                    // Daughter 스폰 위치 표시 (흰색)
                    Vector3 daughterSpawnPos = daughterSpots[i].position;
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireCube(daughterSpawnPos, Vector3.one * 0.4f);
                    
                    // 텍스트 표시 (에디터에서만)
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(daughterSpots[i].position + Vector3.up * 1f, $"Daughter {i + 1}");
                    #endif
                }
            }
        }
    }
}