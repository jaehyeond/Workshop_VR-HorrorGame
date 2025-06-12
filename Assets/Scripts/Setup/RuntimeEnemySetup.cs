using UnityEngine;

/// <summary>
/// 런타임에 생성되는 Enemy에 자동으로 Attack Point를 설정하는 컴포넌트
/// CultistSpawner 등에서 Enemy를 생성할 때 자동으로 호출됩니다.
/// </summary>
public class RuntimeEnemySetup : MonoBehaviour
{
    [Header("자동 설정")]
    public bool autoSetupOnStart = true;
    public bool enableDebugLogs = true;
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupEnemyAttackPoint();
        }
    }
    
    /// <summary>
    /// Enemy Attack Point 자동 설정
    /// </summary>
    public void SetupEnemyAttackPoint()
    {
        EnemyAttackSystem attackSystem = GetComponent<EnemyAttackSystem>();
        if (attackSystem == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[RuntimeEnemySetup] {name}에 EnemyAttackSystem이 없습니다.");
            return;
        }
        
        if (attackSystem.attackPoint != null)
        {
            if (enableDebugLogs)
                Debug.Log($"[RuntimeEnemySetup] {name}의 Attack Point가 이미 설정되어 있습니다.");
            return;
        }
        
        // 손 위치 찾기
        Transform rightHand = FindRightHand();
        
        if (rightHand != null)
        {
            attackSystem.attackPoint = rightHand;
            
            // Attack Point에 Collider와 태그 추가
            SetupAttackPointCollider(rightHand.gameObject);
            
            if (enableDebugLogs)
                Debug.Log($"[RuntimeEnemySetup] ✅ {name}의 Attack Point를 {rightHand.name}으로 설정");
        }
        else
        {
            // 손을 찾지 못하면 앞쪽에 Attack Point 생성
            GameObject attackPointObj = new GameObject("AttackPoint");
            attackPointObj.transform.SetParent(transform);
            attackPointObj.transform.localPosition = Vector3.forward * 1f;
            
            SetupAttackPointCollider(attackPointObj);
            attackSystem.attackPoint = attackPointObj.transform;
            
            if (enableDebugLogs)
                Debug.LogWarning($"[RuntimeEnemySetup] ⚠️ {name}의 손을 찾지 못해 Attack Point를 앞쪽에 생성");
        }
    }
    
    /// <summary>
    /// 오른손 찾기
    /// </summary>
    private Transform FindRightHand()
    {
        string[] handNames = {
            "RightHand", "mixamorig:RightHand", "R_Hand", "Hand_R", 
            "RightHandIndex1", "Right_Hand", "hand_R", "HandR"
        };
        
        foreach (string handName in handNames)
        {
            Transform hand = FindChildByName(transform, handName);
            if (hand != null) return hand;
        }
        
        return null;
    }
    
    /// <summary>
    /// Attack Point Collider 설정
    /// </summary>
    private void SetupAttackPointCollider(GameObject attackPointObj)
    {
        // Collider 추가
        if (attackPointObj.GetComponent<Collider>() == null)
        {
            SphereCollider collider = attackPointObj.AddComponent<SphereCollider>();
            collider.radius = 0.1f;
            collider.isTrigger = true;
        }
        
        // 태그 설정
        attackPointObj.tag = "EnemyAttackPoint";
    }
    
    /// <summary>
    /// 재귀적으로 자식에서 이름으로 Transform 찾기
    /// </summary>
    private Transform FindChildByName(Transform parent, string name)
    {
        if (parent.name.Contains(name))
            return parent;
            
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name))
                return child;
                
            Transform found = FindChildByName(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
    
    /// <summary>
    /// 외부에서 호출 가능한 설정 메서드
    /// </summary>
    public static void SetupEnemyOnSpawn(GameObject enemyObj)
    {
        RuntimeEnemySetup setup = enemyObj.GetComponent<RuntimeEnemySetup>();
        if (setup == null)
        {
            setup = enemyObj.AddComponent<RuntimeEnemySetup>();
        }
        
        setup.SetupEnemyAttackPoint();
    }
} 