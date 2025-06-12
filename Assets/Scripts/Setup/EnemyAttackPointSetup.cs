#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Enemy Attack Point 자동 설정 도구
/// Enemy의 Attack Point가 None인 문제를 해결합니다.
/// </summary>
public class EnemyAttackPointSetup : EditorWindow
{
    [MenuItem("Window/VR Horror Game/Setup Enemy Attack Points")]
    public static void ShowWindow()
    {
        GetWindow<EnemyAttackPointSetup>("Enemy Attack Point Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Enemy Attack Point Auto Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Enemy의 Attack Point가 None으로 설정되어 있으면 공격이 제대로 작동하지 않습니다.\n" +
            "이 도구는 자동으로 Enemy의 손 위치를 찾아서 Attack Point로 설정합니다.", 
            MessageType.Info);

        GUILayout.Space(15);

        if (GUILayout.Button("Auto Setup All Enemy Attack Points", GUILayout.Height(40)))
        {
            SetupAllEnemyAttackPoints();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Check Current Enemy Setup", GUILayout.Height(30)))
        {
            CheckCurrentEnemySetup();
        }

        GUILayout.Space(15);

        // 현재 상태 표시
        DisplayCurrentEnemyStatus();
    }

    private void SetupAllEnemyAttackPoints()
    {
        // 씬의 모든 EnemyAttackSystem 찾기
        EnemyAttackSystem[] enemyAttackSystems = FindObjectsByType<EnemyAttackSystem>(FindObjectsSortMode.None);
        
        if (enemyAttackSystems.Length == 0)
        {
            EditorUtility.DisplayDialog("No Enemies Found", 
                "씬에 EnemyAttackSystem을 가진 Enemy가 없습니다!\n" +
                "Enemy 프리팹에 EnemyAttackSystem 컴포넌트를 먼저 추가하세요.", "OK");
            return;
        }

        int setupCount = 0;
        int alreadySetupCount = 0;

        foreach (var enemyAttack in enemyAttackSystems)
        {
            // Attack Point가 이미 설정되어 있는지 확인
            if (enemyAttack.attackPoint != null)
            {
                alreadySetupCount++;
                continue;
            }

            // Attack Point 자동 설정
            Transform attackPoint = FindBestAttackPoint(enemyAttack.transform);
            if (attackPoint != null)
            {
                // Reflection을 사용해서 private 필드에 접근
                var field = typeof(EnemyAttackSystem).GetField("attackPoint", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (field != null)
                {
                    field.SetValue(enemyAttack, attackPoint);
                    EditorUtility.SetDirty(enemyAttack);
                    setupCount++;
                    
                    Debug.Log($"[EnemyAttackPointSetup] {enemyAttack.name}의 Attack Point를 {attackPoint.name}으로 설정");
                }
            }
            else
            {
                Debug.LogWarning($"[EnemyAttackPointSetup] {enemyAttack.name}에서 적절한 Attack Point를 찾을 수 없음");
            }
        }

        string message = $"Enemy Attack Point 설정 완료!\n\n" +
                        $"✅ 새로 설정된 Enemy: {setupCount}개\n" +
                        $"✅ 이미 설정된 Enemy: {alreadySetupCount}개\n" +
                        $"📊 총 Enemy 수: {enemyAttackSystems.Length}개";

        Debug.Log($"[EnemyAttackPointSetup] {message}");
        EditorUtility.DisplayDialog("Setup Complete", message, "OK");
    }

    /// <summary>
    /// Enemy에서 가장 적절한 Attack Point 찾기
    /// </summary>
    private Transform FindBestAttackPoint(Transform enemyRoot)
    {
        // 1. 오른손 찾기 (우선순위 높음)
        Transform rightHand = FindChildByName(enemyRoot, "RightHand");
        if (rightHand == null) rightHand = FindChildByName(enemyRoot, "mixamorig:RightHand");
        if (rightHand == null) rightHand = FindChildByName(enemyRoot, "R_Hand");
        if (rightHand == null) rightHand = FindChildByName(enemyRoot, "Right_Hand");
        
        if (rightHand != null)
        {
            Debug.Log($"[EnemyAttackPointSetup] 오른손 발견: {rightHand.name}");
            return rightHand;
        }

        // 2. 왼손 찾기 (차선책)
        Transform leftHand = FindChildByName(enemyRoot, "LeftHand");
        if (leftHand == null) leftHand = FindChildByName(enemyRoot, "mixamorig:LeftHand");
        if (leftHand == null) leftHand = FindChildByName(enemyRoot, "L_Hand");
        if (leftHand == null) leftHand = FindChildByName(enemyRoot, "Left_Hand");
        
        if (leftHand != null)
        {
            Debug.Log($"[EnemyAttackPointSetup] 왼손 발견: {leftHand.name}");
            return leftHand;
        }

        // 3. 무기 찾기
        Transform weapon = FindChildByName(enemyRoot, "Weapon");
        if (weapon == null) weapon = FindChildByName(enemyRoot, "Sword");
        if (weapon == null) weapon = FindChildByName(enemyRoot, "Knife");
        
        if (weapon != null)
        {
            Debug.Log($"[EnemyAttackPointSetup] 무기 발견: {weapon.name}");
            return weapon;
        }

        // 4. 마지막 수단: Enemy 중심점 사용
        Debug.LogWarning($"[EnemyAttackPointSetup] {enemyRoot.name}에서 손이나 무기를 찾지 못해 중심점 사용");
        return enemyRoot;
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

    private void CheckCurrentEnemySetup()
    {
        string report = "🔍 Enemy Attack Point 설정 확인:\n\n";
        
        EnemyAttackSystem[] enemyAttackSystems = FindObjectsByType<EnemyAttackSystem>(FindObjectsSortMode.None);
        
        if (enemyAttackSystems.Length == 0)
        {
            report += "❌ 씬에 EnemyAttackSystem이 없음\n";
            report += "   → Enemy 프리팹에 EnemyAttackSystem 컴포넌트를 추가하세요\n";
        }
        else
        {
            report += $"📊 총 Enemy 수: {enemyAttackSystems.Length}개\n\n";
            
            int setupCount = 0;
            int missingCount = 0;
            
            foreach (var enemyAttack in enemyAttackSystems)
            {
                if (enemyAttack.attackPoint != null)
                {
                    setupCount++;
                    report += $"✅ {enemyAttack.name}: {enemyAttack.attackPoint.name}\n";
                }
                else
                {
                    missingCount++;
                    report += $"❌ {enemyAttack.name}: Attack Point 없음\n";
                }
            }
            
            report += $"\n📈 설정 완료: {setupCount}개\n";
            report += $"📉 설정 필요: {missingCount}개\n";
        }

        Debug.Log(report);
        EditorUtility.DisplayDialog("Enemy Setup Check", report, "OK");
    }

    private void DisplayCurrentEnemyStatus()
    {
        EditorGUILayout.LabelField("Current Enemy Status:", EditorStyles.boldLabel);
        
        EnemyAttackSystem[] enemyAttackSystems = FindObjectsByType<EnemyAttackSystem>(FindObjectsSortMode.None);
        EditorGUILayout.LabelField($"Total Enemies: {enemyAttackSystems.Length}");

        if (enemyAttackSystems.Length > 0)
        {
            int setupCount = 0;
            foreach (var enemy in enemyAttackSystems)
            {
                if (enemy.attackPoint != null) setupCount++;
            }
            
            EditorGUILayout.LabelField($"Attack Points Set: {setupCount}/{enemyAttackSystems.Length}");
        }
    }
}
#endif 