using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// Movement 动画根运动生成工具
/// 为 InPlace 的移动动画（Run、Walk、Move）生成 RootMotion 位移曲线
/// 
/// 适用场景：原始动画完全是 InPlace 的，没有任何骨骼位移数据
/// 工具会根据用户设定的速度，生成线性位移曲线到根节点
/// </summary>
public class MovementRootMotionConverter : EditorWindow
{
    private string sourceFolderPath = "Assets/Res/MC/Katixiya/fbx";
    private string targetFolderPath = "Assets/Res/Animations/1004_Katixiya/Movement_RootMotion";
    
    // 速度配置
    private float runSpeed = 6.0f;      // 跑步速度（米/秒）
    private float walkSpeed = 2.0f;     // 步行速度（米/秒）
    private float defaultSpeed = 4.0f;  // 其他动画默认速度
    
    private bool verboseLog = true;
    private string filenameFilter = "Run,Walk,Move";  // 文件名过滤器
    
    // 前进方向（Unity 标准是 Z 正方向）
    private enum ForwardDirection { PositiveZ, NegativeZ, PositiveX, NegativeX }
    private ForwardDirection forwardDirection = ForwardDirection.PositiveZ;

    // 统计变量
    private int newFileCount = 0;
    private int overwriteCount = 0;

    [MenuItem("Tools/动画/Movement根运动转换")]
    public static void ShowWindow()
    {
        GetWindow<MovementRootMotionConverter>("Movement根运动转换");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Movement 根运动生成工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "为 InPlace 的移动动画生成根运动位移曲线。\n" +
            "适用于原始动画完全没有骨骼位移的情况。\n" +
            "工具会根据设定的速度生成线性位移到根节点。",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 源文件夹选择
        EditorGUILayout.BeginHorizontal();
        sourceFolderPath = EditorGUILayout.TextField("源 FBX 文件夹", sourceFolderPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("选择源 FBX 文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                sourceFolderPath = ConvertToRelativePath(selectedPath);
            }
        }
        EditorGUILayout.EndHorizontal();

        // 目标文件夹选择
        EditorGUILayout.BeginHorizontal();
        targetFolderPath = EditorGUILayout.TextField("目标文件夹", targetFolderPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("选择目标文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                targetFolderPath = ConvertToRelativePath(selectedPath);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 快速设置
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Katixiya", GUILayout.Width(80)))
        {
            sourceFolderPath = "Assets/Res/MC/Katixiya/fbx";
            targetFolderPath = "Assets/Res/Animations/1004_Katixiya/Movement_RootMotion";
            filenameFilter = "Run,Walk,Move";
        }
        if (GUILayout.Button("Anbi", GUILayout.Width(80)))
        {
            sourceFolderPath = "Assets/Models/绝区零/安比/fbx";
            targetFolderPath = "Assets/Res/Animations/1001_Anbi/Movement_RootMotion";
            filenameFilter = "Run,Walk,Move";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 文件过滤设置
        EditorGUILayout.LabelField("文件过滤", EditorStyles.boldLabel);
        filenameFilter = EditorGUILayout.TextField("文件名包含（逗号分隔）", filenameFilter);
        EditorGUILayout.HelpBox("只处理文件名包含这些关键词的 FBX，例如: Run,Walk,Move", MessageType.None);

        EditorGUILayout.Space(10);

        // 速度设置
        EditorGUILayout.LabelField("速度设置（米/秒）", EditorStyles.boldLabel);
        runSpeed = EditorGUILayout.Slider("跑步速度 (Run)", runSpeed, 1f, 15f);
        walkSpeed = EditorGUILayout.Slider("步行速度 (Walk)", walkSpeed, 0.5f, 5f);
        defaultSpeed = EditorGUILayout.Slider("其他默认速度", defaultSpeed, 1f, 10f);
        
        EditorGUILayout.HelpBox(
            "速度规则：\n" +
            "• 文件名包含 'Run' → 使用跑步速度\n" +
            "• 文件名包含 'Walk' → 使用步行速度\n" +
            "• 其他 → 使用默认速度",
            MessageType.None);

        EditorGUILayout.Space(5);
        
        forwardDirection = (ForwardDirection)EditorGUILayout.EnumPopup("前进方向", forwardDirection);
        
        EditorGUILayout.Space(5);
        verboseLog = EditorGUILayout.Toggle("输出详细日志", verboseLog);

        EditorGUILayout.Space(10);

        // 统计匹配的文件数量
        if (!string.IsNullOrEmpty(sourceFolderPath) && AssetDatabase.IsValidFolder(sourceFolderPath))
        {
            var matchingFiles = GetMatchingFiles();
            EditorGUILayout.HelpBox($"匹配到 {matchingFiles.Count} 个 FBX 文件", MessageType.None);
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("批量生成所有匹配的动画", GUILayout.Height(30)))
        {
            ConvertAllMatching();
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("仅转换选中的 FBX", GUILayout.Height(25)))
        {
            ConvertSelected();
        }
    }

    /// <summary>
    /// 根据文件名判断移动速度
    /// </summary>
    private float GetSpeedForFile(string fileName)
    {
        string upper = fileName.ToUpper();
        
        if (upper.Contains("RUN"))
            return runSpeed;
        if (upper.Contains("WALK"))
            return walkSpeed;
        
        return defaultSpeed;
    }

    /// <summary>
    /// 获取位移方向向量
    /// </summary>
    private Vector3 GetDirectionVector(string fileName)
    {
        string upper = fileName.ToUpper();
        
        // 根据文件名判断方向
        // 例如: Run_B = 后退, Run_F = 前进, Run_LF = 左前, Run_RF = 右前
        float forward = 0f;
        float right = 0f;
        
        if (upper.Contains("_F") || upper.EndsWith("F"))
            forward = 1f;
        else if (upper.Contains("_B") || upper.EndsWith("B"))
            forward = -1f;
        
        if (upper.Contains("_L") || upper.Contains("LF") || upper.Contains("LB"))
            right = -1f;
        else if (upper.Contains("_R") || upper.Contains("RF") || upper.Contains("RB"))
            right = 1f;
        
        // 如果没有明确方向，默认向前
        if (forward == 0f && right == 0f)
            forward = 1f;
        
        // 归一化
        Vector3 dir = new Vector3(right, 0f, forward).normalized;
        
        // 根据设置的前进方向调整
        switch (forwardDirection)
        {
            case ForwardDirection.NegativeZ:
                dir.z = -dir.z;
                break;
            case ForwardDirection.PositiveX:
                // 交换 X 和 Z
                float temp = dir.x;
                dir.x = dir.z;
                dir.z = temp;
                break;
            case ForwardDirection.NegativeX:
                float temp2 = dir.x;
                dir.x = -dir.z;
                dir.z = temp2;
                break;
        }
        
        return dir;
    }

    private List<string> GetMatchingFiles()
    {
        string[] filters = filenameFilter.Split(',')
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrEmpty(f))
            .ToArray();

        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { sourceFolderPath });
        List<string> result = new List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);

            bool matches = filters.Length == 0 || filters.Any(f => 
                fileName.IndexOf(f, System.StringComparison.OrdinalIgnoreCase) >= 0);

            if (matches)
            {
                result.Add(path);
            }
        }

        return result;
    }

    private void ConvertAllMatching()
    {
        var files = GetMatchingFiles();

        if (files.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有匹配的 FBX 文件", "确定");
            return;
        }

        EnsureTargetFolderExists();

        newFileCount = 0;
        overwriteCount = 0;
        int successCount = 0;
        int failCount = 0;

        AssetDatabase.StartAssetEditing();

        try
        {
            for (int i = 0; i < files.Count; i++)
            {
                string path = files[i];
                string fileName = Path.GetFileNameWithoutExtension(path);

                float progress = (float)i / files.Count;
                bool cancelled = EditorUtility.DisplayCancelableProgressBar(
                    "生成 Movement 根运动",
                    $"正在处理: {fileName} ({i + 1}/{files.Count})",
                    progress);

                if (cancelled)
                {
                    Debug.LogWarning($"用户取消操作，已处理 {i}/{files.Count} 个文件");
                    break;
                }

                if (ConvertAnimation(path))
                    successCount++;
                else
                    failCount++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完成",
            $"处理完成！\n" +
            $"━━━━━━━━━━━━━━━━━━━\n" +
            $"成功: {successCount}\n" +
            $"  · 新建: {newFileCount}\n" +
            $"  · 覆盖: {overwriteCount}\n" +
            $"失败/跳过: {failCount}",
            "确定");
    }

    private void ConvertSelected()
    {
        Object[] selections = Selection.objects;
        if (selections.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Project 窗口中选中要处理的 FBX 文件", "确定");
            return;
        }

        EnsureTargetFolderExists();

        newFileCount = 0;
        overwriteCount = 0;
        int successCount = 0;

        AssetDatabase.StartAssetEditing();

        try
        {
            foreach (Object obj in selections)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (path.EndsWith(".FBX", System.StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (ConvertAnimation(path))
                        successCount++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完成",
            $"成功生成 {successCount} 个动画\n" +
            $"  · 新建: {newFileCount}\n" +
            $"  · 覆盖: {overwriteCount}",
            "确定");
    }

    private bool ConvertAnimation(string fbxPath)
    {
        try
        {
            Object[] objects = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            AnimationClip sourceClip = null;

            foreach (Object obj in objects)
            {
                if (obj is AnimationClip clip && !clip.name.Contains("__preview__"))
                {
                    sourceClip = clip;
                    break;
                }
            }

            if (sourceClip == null)
            {
                if (verboseLog) Debug.LogWarning($"[跳过] 未在 {fbxPath} 中找到动画片段");
                return false;
            }

            string fileName = Path.GetFileNameWithoutExtension(fbxPath);
            float speed = GetSpeedForFile(fileName);
            Vector3 direction = GetDirectionVector(fileName);
            float duration = sourceClip.length;
            
            // 计算总位移
            float totalDisplacement = speed * duration;
            Vector3 displacement = direction * totalDisplacement;

            if (verboseLog)
            {
                Debug.Log($"[生成] {sourceClip.name}");
                Debug.Log($"  时长: {duration:F2}s, 速度: {speed:F1}m/s");
                Debug.Log($"  方向: ({direction.x:F2}, {direction.z:F2}), 位移: {totalDisplacement:F2}m");
            }

            // 创建新动画
            AnimationClip newClip = new AnimationClip();
            newClip.name = sourceClip.name;
            newClip.frameRate = sourceClip.frameRate;

            // 复制所有原始曲线
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(sourceClip);
            foreach (EditorCurveBinding binding in bindings)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                AnimationUtility.SetEditorCurve(newClip, binding, curve);
            }

            // 生成根节点位移曲线 (线性从 0 到 displacement)
            if (Mathf.Abs(displacement.x) > 0.001f)
            {
                AnimationCurve curveX = new AnimationCurve();
                curveX.AddKey(new Keyframe(0f, 0f, speed * direction.x, speed * direction.x));
                curveX.AddKey(new Keyframe(duration, displacement.x, speed * direction.x, speed * direction.x));
                
                EditorCurveBinding bindingX = new EditorCurveBinding
                {
                    path = "",
                    type = typeof(Transform),
                    propertyName = "m_LocalPosition.x"
                };
                AnimationUtility.SetEditorCurve(newClip, bindingX, curveX);
            }

            if (Mathf.Abs(displacement.z) > 0.001f)
            {
                AnimationCurve curveZ = new AnimationCurve();
                curveZ.AddKey(new Keyframe(0f, 0f, speed * direction.z, speed * direction.z));
                curveZ.AddKey(new Keyframe(duration, displacement.z, speed * direction.z, speed * direction.z));
                
                EditorCurveBinding bindingZ = new EditorCurveBinding
                {
                    path = "",
                    type = typeof(Transform),
                    propertyName = "m_LocalPosition.z"
                };
                AnimationUtility.SetEditorCurve(newClip, bindingZ, curveZ);
            }

            // 复制动画设置
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(sourceClip);
            AnimationUtility.SetAnimationClipSettings(newClip, settings);

            // 保存
            string savePath = $"{targetFolderPath}/{fileName}_RM.anim";

            bool isOverwrite = false;
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(savePath) != null)
            {
                isOverwrite = true;
                overwriteCount++;
                AssetDatabase.DeleteAsset(savePath);
            }
            else
            {
                newFileCount++;
            }

            AssetDatabase.CreateAsset(newClip, savePath);

            if (verboseLog)
            {
                string action = isOverwrite ? "覆盖" : "新建";
                Debug.Log($"  ✓ {action}: {savePath}");
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"处理 {fbxPath} 时出错: {e.Message}");
            return false;
        }
    }

    private void EnsureTargetFolderExists()
    {
        if (!AssetDatabase.IsValidFolder(targetFolderPath))
        {
            string[] parts = targetFolderPath.Split('/');
            string currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = currentPath + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }
                currentPath = nextPath;
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
