using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// FBX 根运动骨骼设置工具
/// 批量设置 FBX 文件的 Motion Node（根运动源骨骼）
/// </summary>
public class FBXMotionNodeSetter : EditorWindow
{
    private string folderPath = "Assets/Res/MC/Katixiya/fbx";
    private string motionNodeName = "Root";  // 设置为 Root 骨骼
    private bool verboseLog = true;

    [MenuItem("Tools/动画/FBX根运动骨骼设置")]
    public static void ShowWindow()
    {
        GetWindow<FBXMotionNodeSetter>("FBX根运动设置");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("FBX 根运动骨骼设置工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "此工具批量设置 FBX 文件的 Motion Node（根运动源骨骼）。\n" +
            "设置后，Unity 会从指定骨骼提取根运动数据。\n" +
            "无需生成新动画文件，直接修改 FBX 导入设置！",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 文件夹选择
        EditorGUILayout.BeginHorizontal();
        folderPath = EditorGUILayout.TextField("FBX 文件夹", folderPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("选择 FBX 文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                folderPath = ConvertToRelativePath(selectedPath);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        
        motionNodeName = EditorGUILayout.TextField("根运动骨骼名称", motionNodeName);
        
        EditorGUILayout.HelpBox(
            "根据分析结果，Katixiya 模型的位移数据在 'Root' 骨骼上。\n" +
            "设置 Motion Node 为 'Root' 可以让 Unity 正确识别根运动。",
            MessageType.None);

        EditorGUILayout.Space(5);

        // 快速设置
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Katixiya", GUILayout.Width(80)))
        {
            folderPath = "Assets/Res/MC/Katixiya/fbx";
            motionNodeName = "Root";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        verboseLog = EditorGUILayout.Toggle("输出详细日志", verboseLog);

        EditorGUILayout.Space(10);

        // 统计文件数
        if (!string.IsNullOrEmpty(folderPath) && AssetDatabase.IsValidFolder(folderPath))
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folderPath });
            EditorGUILayout.HelpBox($"将处理 {guids.Length} 个 FBX 文件", MessageType.None);
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("批量设置所有 FBX", GUILayout.Height(30)))
        {
            SetMotionNodeForAll();
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("仅设置选中的 FBX", GUILayout.Height(25)))
        {
            SetMotionNodeForSelected();
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("查看选中 FBX 的当前设置", GUILayout.Height(25)))
        {
            ShowCurrentSettings();
        }
    }

    private void SetMotionNodeForAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folderPath });

        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("错误", $"在 {folderPath} 中未找到任何 FBX 文件", "确定");
            return;
        }

        int successCount = 0;
        int skipCount = 0;

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string fileName = Path.GetFileNameWithoutExtension(path);

                EditorUtility.DisplayProgressBar(
                    "设置 Motion Node",
                    $"正在处理: {fileName} ({i + 1}/{guids.Length})",
                    (float)i / guids.Length);

                if (SetMotionNode(path))
                    successCount++;
                else
                    skipCount++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完成",
            $"处理完成！\n" +
            $"成功设置: {successCount} 个\n" +
            $"跳过: {skipCount} 个",
            "确定");
    }

    private void SetMotionNodeForSelected()
    {
        Object[] selections = Selection.objects;
        if (selections.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Project 窗口中选中要处理的 FBX 文件", "确定");
            return;
        }

        int successCount = 0;

        foreach (Object obj in selections)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (path.EndsWith(".FBX", System.StringComparison.OrdinalIgnoreCase) || 
                path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            {
                if (SetMotionNode(path))
                    successCount++;
            }
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完成", $"成功设置 {successCount} 个 FBX 文件", "确定");
    }

    private bool SetMotionNode(string fbxPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
        {
            if (verboseLog) Debug.LogWarning($"[跳过] 无法获取导入器: {fbxPath}");
            return false;
        }

        // 检查当前设置
        string currentMotionNode = importer.motionNodeName;
        if (currentMotionNode == motionNodeName)
        {
            if (verboseLog) Debug.Log($"[跳过] 已设置: {fbxPath}");
            return false;
        }

        // 设置 Motion Node
        importer.motionNodeName = motionNodeName;
        
        // 保存设置
        importer.SaveAndReimport();

        if (verboseLog)
        {
            Debug.Log($"<color=green>[成功]</color> {Path.GetFileName(fbxPath)}: Motion Node = '{motionNodeName}'");
        }

        return true;
    }

    private void ShowCurrentSettings()
    {
        Object[] selections = Selection.objects;
        if (selections.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Project 窗口中选中 FBX 文件", "确定");
            return;
        }

        foreach (Object obj in selections)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (path.EndsWith(".FBX", System.StringComparison.OrdinalIgnoreCase) || 
                path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            {
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer != null)
                {
                    string motionNode = string.IsNullOrEmpty(importer.motionNodeName) ? "(未设置)" : importer.motionNodeName;
                    Debug.Log($"<color=cyan>[FBX设置]</color> {Path.GetFileName(path)}:\n" +
                             $"  Motion Node: {motionNode}\n" +
                             $"  Animation Type: {importer.animationType}");
                }
            }
        }
    }

    private string ConvertToRelativePath(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath)) return "";

        string projectPath = Path.GetFullPath(Application.dataPath + "/..");
        projectPath = projectPath.Replace("\\", "/");
        absolutePath = absolutePath.Replace("\\", "/");

        if (absolutePath.StartsWith(projectPath))
        {
            return absolutePath.Substring(projectPath.Length + 1);
        }
        return absolutePath;
    }
}
