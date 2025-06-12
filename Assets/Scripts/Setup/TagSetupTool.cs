#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 필요한 태그들을 자동으로 생성하는 도구
/// </summary>
public class TagSetupTool : EditorWindow
{
    [MenuItem("Window/VR Horror Game/Setup Tags")]
    public static void ShowWindow()
    {
        GetWindow<TagSetupTool>("Tag Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Tag Setup Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "VR Horror Game에 필요한 태그들을 자동으로 생성합니다.", 
            MessageType.Info);

        GUILayout.Space(15);

        if (GUILayout.Button("Create All Required Tags", GUILayout.Height(40)))
        {
            CreateAllTags();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Check Current Tags", GUILayout.Height(30)))
        {
            CheckCurrentTags();
        }
    }

    private void CreateAllTags()
    {
        string[] requiredTags = {
            "EnemyAttackPoint",
            "Player",
            "Enemy",
            "VRPlayer"
        };

        int createdCount = 0;
        int existingCount = 0;

        foreach (string tag in requiredTags)
        {
            if (CreateTagIfNotExists(tag))
            {
                createdCount++;
                Debug.Log($"[TagSetup] ✅ 태그 생성: {tag}");
            }
            else
            {
                existingCount++;
                Debug.Log($"[TagSetup] 태그 이미 존재: {tag}");
            }
        }

        string message = $"태그 설정 완료!\n\n" +
                        $"✅ 새로 생성된 태그: {createdCount}개\n" +
                        $"📋 기존 태그: {existingCount}개";

        EditorUtility.DisplayDialog("Tag Setup Complete", message, "확인");
        Debug.Log($"[TagSetup] 태그 설정 완료! 생성: {createdCount}, 기존: {existingCount}");
    }

    private bool CreateTagIfNotExists(string tagName)
    {
        // 태그가 이미 존재하는지 확인
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        // 기존 태그 확인
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty tag = tagsProp.GetArrayElementAtIndex(i);
            if (tag.stringValue.Equals(tagName))
            {
                return false; // 이미 존재
            }
        }

        // 새 태그 추가
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
        newTag.stringValue = tagName;
        tagManager.ApplyModifiedProperties();

        return true; // 새로 생성됨
    }

    private void CheckCurrentTags()
    {
        string[] requiredTags = {
            "EnemyAttackPoint",
            "Player", 
            "Enemy",
            "VRPlayer"
        };

        string report = "현재 태그 상태:\n\n";

        foreach (string tag in requiredTags)
        {
            bool exists = TagExists(tag);
            report += $"{(exists ? "✅" : "❌")} {tag}\n";
        }

        EditorUtility.DisplayDialog("Tag Status", report, "확인");
        Debug.Log($"[TagSetup] {report.Replace("\n", " | ")}");
    }

    private bool TagExists(string tagName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty tag = tagsProp.GetArrayElementAtIndex(i);
            if (tag.stringValue.Equals(tagName))
            {
                return true;
            }
        }
        return false;
    }
}
#endif 