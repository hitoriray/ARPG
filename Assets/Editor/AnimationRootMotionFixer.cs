using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 修复动画根运动工具
/// 自动检测位移源骨骼，将其位移数据转移到根节点，实现真正的根运动
/// </summary>
public class AnimationRootMotionFixer : EditorWindow
{
    private string sourceFolderPath = "Assets/Models/绝区零/安比/Anbi/fbx";
    private string targetFolderPath = "Assets/Res/Animations/Anbi/Anbi_RootMotion";
    private bool includeYAxis = false; // 是否包含 Y 轴位移（用于跳跃等）
    private bool preserveSourceBoneY = true; // 保留源骨骼的 Y 轴运动（身体起伏）
    private bool verboseLog = false; // 是否输出详细日志（影响性能）
    private float scaleMultiplier = 1.0f; // 缩放倍数（用于处理不同 FBX 的 GlobalScale）
    private bool autoDetectSourceBone = true; // 自动检测位移源骨骼
    private string customSourceBonePath = ""; // 用户自定义的源骨骼路径（留空则自动检测）
    private float minDisplacementThreshold = 0.01f; // 最小位移阈值（小于此值的骨骼不作为候选）

    // 统计变量
    private int newFileCount = 0;
    private int overwriteCount = 0;

    // 候选位移源骨骼列表（按优先级排序）
    private static readonly string[] DefaultMotionSourceCandidates = new string[]
    {
        "Bip001",           // 3ds Max Biped 标准
        "Root/Bip001",      // 嵌套结构
        "Root",             // UE 导出模型常用
        "Hips",             // Unity Humanoid 标准
        "mixamorig:Hips",   // Mixamo 模型
        "Pelvis",           // 某些模型使用 Pelvis
    };

    [MenuItem("Tools/动画/根运动修复工具")]
    public static void ShowWindow()
    {
        GetWindow<AnimationRootMotionFixer>("根运动修复工具");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("动画根运动修复工具 v2.0", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "此工具自动检测包含位移数据的骨骼，将其水平位移（XZ）转移到根节点。\n" +
            "支持多种骨骼命名规范：Bip001、Root、Hips 等。\n" +
            "生成的动画文件会自动添加 _RM 后缀（Root Motion）。",
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

        // 检查源文件夹是否存在
        if (!string.IsNullOrEmpty(sourceFolderPath) && !AssetDatabase.IsValidFolder(sourceFolderPath))
        {
            EditorGUILayout.HelpBox($"源文件夹不存在: {sourceFolderPath}", MessageType.Warning);
        }

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

        // 提示目标文件夹状态
        if (!string.IsNullOrEmpty(targetFolderPath))
        {
            if (!AssetDatabase.IsValidFolder(targetFolderPath))
            {
                EditorGUILayout.HelpBox($"目标文件夹不存在，将自动创建: {targetFolderPath}", MessageType.Info);
            }
        }

        EditorGUILayout.Space(5);

        // 快速设置按钮
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("安比", GUILayout.Width(60)))
        {
            sourceFolderPath = "Assets/Models/绝区零/安比/fbx";
            targetFolderPath = "Assets/Res/Animations/Anbi/Anbi_RootMotion";
            scaleMultiplier = 1.0f;
            customSourceBonePath = "";
        }
        if (GUILayout.Button("11号", GUILayout.Width(60)))
        {
            sourceFolderPath = "Assets/Models/绝区零/11号/fbx";
            targetFolderPath = "Assets/Res/Animations/11/11_RootMotion";
            scaleMultiplier = 1.0f;
            customSourceBonePath = "";
        }
        if (GUILayout.Button("可琳", GUILayout.Width(60)))
        {
            sourceFolderPath = "Assets/Models/绝区零/可琳/fbx";
            targetFolderPath = "Assets/Res/Animations/Corin/Corin_RootMotion";
            scaleMultiplier = 1.0f;
            customSourceBonePath = "";
        }
        if (GUILayout.Button("猫又", GUILayout.Width(60)))
        {
            sourceFolderPath = "Assets/Models/绝区零/猫又/fbx";
            targetFolderPath = "Assets/Res/Animations/Maoyou/MaoYou_RootMotion";
            scaleMultiplier = 1.0f;
            customSourceBonePath = "";
        }
        if (GUILayout.Button("珂蕾妲", GUILayout.Width(60)))
        {
            sourceFolderPath = "Assets/Models/绝区零/珂蕾妲/fbx";
            targetFolderPath = "Assets/Res/Animations/KeLeiDa/KeLeiDa_RootMotion";
            scaleMultiplier = 1.0f;
            customSourceBonePath = "";
        }
        if (GUILayout.Button("简杜", GUILayout.Width(60)))
        {
            sourceFolderPath = "Assets/Models/绝区零/简杜/fbx";
            targetFolderPath = "Assets/Res/Animations/JianDu/JianDu_RootMotion";
            scaleMultiplier = 1.0f;
            customSourceBonePath = "";
        }
        if (GUILayout.Button("艾莲", GUILayout.Width(60)))
        {
            sourceFolderPath = "Assets/Models/绝区零/艾莲/fbx";
            targetFolderPath = "Assets/Res/Animations/Ellen/Ellen_RootMotion";
            scaleMultiplier = 1.0f;
            customSourceBonePath = "";
        }
        if (GUILayout.Button("雅", GUILayout.Width(60)))
        {
            sourceFolderPath = "Assets/Models/绝区零/雅/fbx";
            targetFolderPath = "Assets/Res/Animations/Ya/Ya_RootMotion";
            scaleMultiplier = 1.0f;
            customSourceBonePath = "";
        }
        if (GUILayout.Button("玲", GUILayout.Width(60)))
        {
            sourceFolderPath = "Assets/Models/绝区零/女主玲/fbx";
            targetFolderPath = "Assets/Res/Animations/Ling/Ling_RootMotion";
            scaleMultiplier = 1.0f;
            customSourceBonePath = "";
        }
        if (GUILayout.Button("卡缇西娅", GUILayout.Width(70)))
        {
            sourceFolderPath = "Assets/Res/MC/Katixiya/fbx";
            targetFolderPath = "Assets/Res/Animations/1004_Katixiya/RootMotion";
            scaleMultiplier = 1.0f;  // 原始动画数据已经是正确的单位
            customSourceBonePath = ""; // 使用自动检测
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        
        // 高级设置
        EditorGUILayout.LabelField("高级设置", EditorStyles.boldLabel);
        
        autoDetectSourceBone = EditorGUILayout.Toggle("自动检测位移源骨骼", autoDetectSourceBone);
        
        EditorGUI.BeginDisabledGroup(autoDetectSourceBone);
        customSourceBonePath = EditorGUILayout.TextField("自定义源骨骼路径", customSourceBonePath);
        EditorGUI.EndDisabledGroup();
        
        if (autoDetectSourceBone)
        {
            EditorGUILayout.HelpBox("自动检测将扫描所有骨骼，找出 XZ 平面位移最大的骨骼作为源。", MessageType.None);
        }

        EditorGUILayout.Space(5);
        
        includeYAxis = EditorGUILayout.Toggle("包含 Y 轴位移（跳跃）", includeYAxis);
        preserveSourceBoneY = EditorGUILayout.Toggle("保留源骨骼 Y 轴（身体起伏）", preserveSourceBoneY);
        verboseLog = EditorGUILayout.Toggle("输出详细日志（影响速度）", verboseLog);
        scaleMultiplier = EditorGUILayout.Slider("缩放倍数（实验性）", scaleMultiplier, 0.001f, 10f);
        minDisplacementThreshold = EditorGUILayout.Slider("最小位移阈值", minDisplacementThreshold, 0.001f, 1f);

        EditorGUILayout.Space(5);

        // 显示预计处理的文件数量
        if (!string.IsNullOrEmpty(sourceFolderPath) && AssetDatabase.IsValidFolder(sourceFolderPath))
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { sourceFolderPath });
            EditorGUILayout.HelpBox($"将处理 {guids.Length} 个 FBX 文件", MessageType.None);
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("批量修复所有动画", GUILayout.Height(30)))
        {
            FixAllAnimations();
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("仅修复选中的 FBX", GUILayout.Height(25)))
        {
            FixSelectedFBX();
        }
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("分析选中的 FBX（仅显示骨骼位移数据）", GUILayout.Height(25)))
        {
            AnalyzeSelectedFBX();
        }
    }

    /// <summary>
    /// 分析选中的 FBX 文件，显示骨骼位移数据
    /// </summary>
    private void AnalyzeSelectedFBX()
    {
        Object[] selections = Selection.objects;
        if (selections.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Project 窗口中选中要分析的 FBX 文件", "确定");
            return;
        }

        foreach (Object obj in selections)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (path.EndsWith(".FBX") || path.EndsWith(".fbx"))
            {
                AnalyzeAnimation(path);
            }
        }
    }

    /// <summary>
    /// 分析动画文件中的骨骼位移数据
    /// </summary>
    private void AnalyzeAnimation(string fbxPath)
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
            Debug.Log($"[分析] 未在 {fbxPath} 中找到动画片段");
            return;
        }

        Debug.Log($"<color=cyan>=== 分析 FBX: {fbxPath} ===</color>");
        Debug.Log($"动画片段: {sourceClip.name}, 时长: {sourceClip.length:F3}s, 帧率: {sourceClip.frameRate}");

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(sourceClip);
        
        // 收集所有骨骼的位移数据
        Dictionary<string, MotionData> boneMotions = CollectBoneMotionData(sourceClip, bindings);

        // 按 XZ 位移排序
        var sortedBones = boneMotions.Values
            .OrderByDescending(b => b.XZDisplacement)
            .ToList();

        Debug.Log($"\n<color=yellow>【骨骼位移分析（按 XZ 位移排序，前10个）】</color>");
        
        int count = 0;
        foreach (var bone in sortedBones)
        {
            if (bone.XZDisplacement > minDisplacementThreshold || count < 5)
            {
                string pathDisplay = string.IsNullOrEmpty(bone.BonePath) ? "(ROOT - 空路径)" : bone.BonePath;
                Debug.Log($"  {pathDisplay}:\n" +
                         $"    XZ 位移: {bone.XZDisplacement:F4}\n" +
                         $"    X: Start={bone.StartX:F4}, End={bone.EndX:F4}, Delta={bone.DeltaX:F4}\n" +
                         $"    Y: Start={bone.StartY:F4}, End={bone.EndY:F4}, Delta={bone.DeltaY:F4}\n" +
                         $"    Z: Start={bone.StartZ:F4}, End={bone.EndZ:F4}, Delta={bone.DeltaZ:F4}");
                count++;
                
                if (count >= 10) break;
            }
        }

        // 找出推荐的源骨骼
        string detectedBone = DetectMotionSourceBone(sourceClip, bindings);
        if (!string.IsNullOrEmpty(detectedBone))
        {
            Debug.Log($"\n<color=green>【推荐源骨骼】: {detectedBone}</color>");
        }
        else
        {
            Debug.Log($"\n<color=red>【警告】未找到明显的位移源骨骼（所有骨骼的 XZ 位移都小于阈值 {minDisplacementThreshold}）</color>");
        }
    }

    /// <summary>
    /// 收集所有骨骼的位移数据
    /// </summary>
    private Dictionary<string, MotionData> CollectBoneMotionData(AnimationClip clip, EditorCurveBinding[] bindings)
    {
        Dictionary<string, MotionData> boneMotions = new Dictionary<string, MotionData>();

        foreach (EditorCurveBinding binding in bindings)
        {
            if (binding.type == typeof(Transform) && binding.propertyName.StartsWith("m_LocalPosition"))
            {
                if (!boneMotions.ContainsKey(binding.path))
                {
                    boneMotions[binding.path] = new MotionData { BonePath = binding.path };
                }

                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                MotionData data = boneMotions[binding.path];

                float startValue = curve.Evaluate(0);
                float endValue = curve.Evaluate(clip.length);
                float delta = endValue - startValue;

                if (binding.propertyName.EndsWith(".x"))
                {
                    data.StartX = startValue;
                    data.EndX = endValue;
                    data.DeltaX = delta;
                    data.CurveX = curve;
                }
                else if (binding.propertyName.EndsWith(".y"))
                {
                    data.StartY = startValue;
                    data.EndY = endValue;
                    data.DeltaY = delta;
                    data.CurveY = curve;
                }
                else if (binding.propertyName.EndsWith(".z"))
                {
                    data.StartZ = startValue;
                    data.EndZ = endValue;
                    data.DeltaZ = delta;
                    data.CurveZ = curve;
                }
            }
        }

        return boneMotions;
    }

    /// <summary>
    /// 自动检测位移源骨骼
    /// </summary>
    private string DetectMotionSourceBone(AnimationClip clip, EditorCurveBinding[] bindings)
    {
        Dictionary<string, MotionData> boneMotions = CollectBoneMotionData(clip, bindings);

        // 策略1：在默认候选列表中查找有位移的骨骼
        foreach (string candidate in DefaultMotionSourceCandidates)
        {
            // 检查精确匹配
            if (boneMotions.TryGetValue(candidate, out MotionData data))
            {
                if (data.XZDisplacement > minDisplacementThreshold)
                {
                    return candidate;
                }
            }

            // 检查以候选名结尾的路径（如 "someRoot/Bip001"）
            foreach (var kvp in boneMotions)
            {
                if (kvp.Key.EndsWith("/" + candidate) && kvp.Value.XZDisplacement > minDisplacementThreshold)
                {
                    return kvp.Key;
                }
            }
        }

        // 策略2：找出 XZ 位移最大的骨骼
        var maxMotionBone = boneMotions.Values
            .Where(b => b.XZDisplacement > minDisplacementThreshold)
            .OrderByDescending(b => b.XZDisplacement)
            .FirstOrDefault();

        if (maxMotionBone != null)
        {
            return maxMotionBone.BonePath;
        }

        return null;
    }

    private class MotionData
    {
        public string BonePath;
        public float StartX, StartY, StartZ;
        public float EndX, EndY, EndZ;
        public float DeltaX, DeltaY, DeltaZ;
        public AnimationCurve CurveX, CurveY, CurveZ;

        public float XZDisplacement => Mathf.Sqrt(DeltaX * DeltaX + DeltaZ * DeltaZ);
    }

    private void FixAllAnimations()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { sourceFolderPath });

        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("错误", $"在 {sourceFolderPath} 中未找到任何 FBX 文件", "确定");
            return;
        }

        EnsureTargetFolderExists();

        // 重置统计
        newFileCount = 0;
        overwriteCount = 0;
        int successCount = 0;
        int failCount = 0;

        // ========== 性能优化：批量资源编辑模式 ==========
        AssetDatabase.StartAssetEditing();

        try
        {
            int totalCount = guids.Length;
            for (int i = 0; i < totalCount; i++)
            {
                string guid = guids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

                // 显示进度条
                float progress = (float)i / totalCount;
                bool cancelled = EditorUtility.DisplayCancelableProgressBar(
                    "修复根运动动画",
                    $"正在处理: {fileName} ({i + 1}/{totalCount})",
                    progress);

                if (cancelled)
                {
                    RayDebug.Warn($"用户取消操作，已处理 {i}/{totalCount} 个文件");
                    break;
                }

                if (ExtractAndFixAnimation(path))
                    successCount++;
                else
                    failCount++;
            }
        }
        finally
        {
            // 确保无论如何都会停止批量编辑模式
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
            $"失败: {failCount}",
            "确定");
    }

    private void FixSelectedFBX()
    {
        Object[] selections = Selection.objects;
        if (selections.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Project 窗口中选中要处理的 FBX 文件", "确定");
            return;
        }

        EnsureTargetFolderExists();

        // 重置统计
        newFileCount = 0;
        overwriteCount = 0;
        int successCount = 0;

        // ========== 性能优化：批量资源编辑模式 ==========
        AssetDatabase.StartAssetEditing();

        try
        {
            int totalCount = selections.Length;
            int currentIndex = 0;

            foreach (Object obj in selections)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (path.EndsWith(".FBX") || path.EndsWith(".fbx"))
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

                    // 显示进度条
                    float progress = (float)currentIndex / totalCount;
                    bool cancelled = EditorUtility.DisplayCancelableProgressBar(
                        "修复选中的根运动动画",
                        $"正在处理: {fileName} ({currentIndex + 1}/{totalCount})",
                        progress);

                    if (cancelled)
                    {
                        RayDebug.Warn($"用户取消操作，已处理 {currentIndex}/{totalCount} 个文件");
                        break;
                    }

                    if (ExtractAndFixAnimation(path))
                    successCount++;
                    currentIndex++;
                }
            }
        }
        finally
        {
            // 确保无论如何都会停止批量编辑模式
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成",
            $"成功处理 {successCount} 个动画\n" +
            $"  · 新建: {newFileCount}\n" +
            $"  · 覆盖: {overwriteCount}",
            "确定");
    }

    private void EnsureTargetFolderExists()
    {
        if (!AssetDatabase.IsValidFolder(targetFolderPath))
        {
            // 递归创建目录
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

    private bool ExtractAndFixAnimation(string fbxPath)
    {
        try
        {
            // 加载 FBX 中的所有动画片段
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
                if (verboseLog)
                    RayDebug.Warn($"[跳过] 未在 {fbxPath} 中找到动画片段");
                return false;
            }

            if (verboseLog)
                RayDebug.Log($"[处理] {sourceClip.name} (长度: {sourceClip.length:F2}s, 帧率: {sourceClip.frameRate})");

            // 获取所有曲线绑定
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(sourceClip);

            // 确定源骨骼路径
            string sourceBonePath;
            if (autoDetectSourceBone || string.IsNullOrEmpty(customSourceBonePath))
            {
                sourceBonePath = DetectMotionSourceBone(sourceClip, bindings);
                if (string.IsNullOrEmpty(sourceBonePath))
                {
                    if (verboseLog)
                        RayDebug.Warn($"[跳过] {sourceClip.name} 中未找到包含位移的骨骼");
                    return false;
                }
            }
            else
            {
                sourceBonePath = customSourceBonePath;
            }

            // 收集源骨骼的位移数据
            AnimationCurve sourcePosX = null;
            AnimationCurve sourcePosY = null;
            AnimationCurve sourcePosZ = null;

            foreach (EditorCurveBinding binding in bindings)
            {
                if (binding.path == sourceBonePath && binding.type == typeof(Transform))
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);

                    if (binding.propertyName == "m_LocalPosition.x")
                        sourcePosX = curve;
                    else if (binding.propertyName == "m_LocalPosition.y")
                        sourcePosY = curve;
                    else if (binding.propertyName == "m_LocalPosition.z")
                        sourcePosZ = curve;
                }
            }

            // 验证位移数据
            if (sourcePosX == null && sourcePosZ == null)
            {
                if (verboseLog)
                    RayDebug.Warn($"[跳过] {sourceClip.name} 中 {sourceBonePath} 骨骼没有位置曲线");
                return false;
            }

            // 计算位移量（仅用于日志）
            if (verboseLog)
            {
                float totalDisplacement = 0f;
                float initialY = sourcePosY != null ? sourcePosY.Evaluate(0) : 0;

                if (sourcePosX != null && sourcePosZ != null)
                {
                    float startX = sourcePosX.Evaluate(0);
                    float startZ = sourcePosZ.Evaluate(0);
                    float endX = sourcePosX.Evaluate(sourceClip.length);
                    float endZ = sourcePosZ.Evaluate(sourceClip.length);
                    totalDisplacement = Vector2.Distance(new Vector2(startX, startZ), new Vector2(endX, endZ));
                }

                RayDebug.Log($"  → 使用源骨骼: {sourceBonePath}");
                RayDebug.Log($"  → 初始 Y 位置: {initialY:F3}");
                RayDebug.Log($"  → XZ 总位移: {totalDisplacement:F3} 单位 (缩放后: {totalDisplacement * scaleMultiplier:F3})");
            }

            // 创建新的动画片段
            AnimationClip newClip = new AnimationClip();
            newClip.name = sourceClip.name;
            newClip.frameRate = sourceClip.frameRate;

            // 复制所有曲线并处理源骨骼
            foreach (EditorCurveBinding binding in bindings)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);

                if (binding.path == sourceBonePath && binding.type == typeof(Transform))
                {
                    if (binding.propertyName.StartsWith("m_LocalPosition"))
                    {
                        if (preserveSourceBoneY && binding.propertyName == "m_LocalPosition.y")
                        {
                            // 保留 Y 轴的身体起伏（保持原始曲线）
                            AnimationUtility.SetEditorCurve(newClip, binding, curve);
                        }
                        else
                        {
                            // XZ 位移清零到初始位置
                            float initialValue = curve.Evaluate(0);
                            AnimationCurve fixedCurve = new AnimationCurve();
                            fixedCurve.AddKey(0, initialValue);
                            fixedCurve.AddKey(sourceClip.length, initialValue);
                            AnimationUtility.SetEditorCurve(newClip, binding, fixedCurve);
                        }
                    }
                    else
                    {
                        // 旋转和缩放保持不变
                        AnimationUtility.SetEditorCurve(newClip, binding, curve);
                    }
                }
                else
                {
                    // 其他骨骼保持不变
                    AnimationUtility.SetEditorCurve(newClip, binding, curve);
                }
            }

            // 添加根节点的位移曲线（从源骨骼转移过来）
            if (sourcePosX != null)
            {
                float initialX = sourcePosX.Evaluate(0);
                AnimationCurve rootCurveX = CreateDeltaCurve(sourcePosX, initialX, scaleMultiplier);

                EditorCurveBinding rootBindingX = new EditorCurveBinding
                {
                    path = "",
                    type = typeof(Transform),
                    propertyName = "m_LocalPosition.x"
                };
                AnimationUtility.SetEditorCurve(newClip, rootBindingX, rootCurveX);
            }

            if (includeYAxis && sourcePosY != null)
            {
                float initialY = sourcePosY.Evaluate(0);
                AnimationCurve rootCurveY = CreateDeltaCurve(sourcePosY, initialY, scaleMultiplier);

                EditorCurveBinding rootBindingY = new EditorCurveBinding
                {
                    path = "",
                    type = typeof(Transform),
                    propertyName = "m_LocalPosition.y"
                };
                AnimationUtility.SetEditorCurve(newClip, rootBindingY, rootCurveY);
            }

            if (sourcePosZ != null)
            {
                float initialZ = sourcePosZ.Evaluate(0);
                AnimationCurve rootCurveZ = CreateDeltaCurve(sourcePosZ, initialZ, scaleMultiplier);

                EditorCurveBinding rootBindingZ = new EditorCurveBinding
                {
                    path = "",
                    type = typeof(Transform),
                    propertyName = "m_LocalPosition.z"
                };
                AnimationUtility.SetEditorCurve(newClip, rootBindingZ, rootCurveZ);
            }

            // 设置循环
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(sourceClip);
            AnimationUtility.SetAnimationClipSettings(newClip, settings);

            // 保存新的动画文件（添加 _RM 后缀表示 Root Motion）
            string fileName = System.IO.Path.GetFileNameWithoutExtension(fbxPath);
            string savePath = $"{targetFolderPath}/{fileName}_RM.anim";

            // 检查文件是否已存在
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

            // 创建新文件
            AssetDatabase.CreateAsset(newClip, savePath);

            if (verboseLog)
            {
                string action = isOverwrite ? "覆盖" : "新建";
                RayDebug.Log($"  ✓ 成功修复 ({action}): {savePath}\n" +
                             $"     源骨骼: {sourceBonePath}\n" +
                             $"     根节点位移: X={sourcePosX != null}, Y={includeYAxis && sourcePosY != null}, Z={sourcePosZ != null}");
            }

            return true;
        }
        catch (System.Exception e)
        {
            RayDebug.Error($"处理 {fbxPath} 时出错: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 创建增量曲线（减去初始值），并应用缩放
    /// </summary>
    private AnimationCurve CreateDeltaCurve(AnimationCurve source, float initialValue, float scale)
    {
        AnimationCurve result = new AnimationCurve();

        foreach (Keyframe key in source.keys)
        {
            Keyframe newKey = new Keyframe(
                key.time,
                (key.value - initialValue) * scale,
                key.inTangent * scale,
                key.outTangent * scale,
                key.inWeight,
                key.outWeight
            );
            newKey.tangentMode = key.tangentMode;
            newKey.weightedMode = key.weightedMode;
            result.AddKey(newKey);
        }

        return result;
    }

    /// <summary>
    /// 将绝对路径转换为相对于 Unity 项目的路径
    /// </summary>
    private string ConvertToRelativePath(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath))
            return "";

        string projectPath = System.IO.Path.GetFullPath(Application.dataPath + "/..");
        projectPath = projectPath.Replace("\\", "/");
        absolutePath = absolutePath.Replace("\\", "/");

        if (absolutePath.StartsWith(projectPath))
        {
            string relativePath = absolutePath.Substring(projectPath.Length + 1);
            return relativePath;
        }

        // 如果不在项目目录内，返回原路径
        RayDebug.Warn($"选择的路径不在 Unity 项目内: {absolutePath}");
        return absolutePath;
    }
}
