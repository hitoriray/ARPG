using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 修复动画根运动工具
/// 将 Bip001 骨骼的位移数据转移到根节点，实现真正的根运动
/// </summary>
public class AnimationRootMotionFixer : EditorWindow
{
    private string sourceFolderPath = "Assets/Models/绝区零/安比/Anbi/fbx";
    private string targetFolderPath = "Assets/Res/Animations/Anbi/Anbi_RootMotion";
    private bool includeYAxis = false; // 是否包含 Y 轴位移（用于跳跃等）
    private bool preserveBip001Y = true; // 保留 Bip001 的 Y 轴运动（身体起伏）

    // 统计变量
    private int newFileCount = 0;
    private int overwriteCount = 0;

    [MenuItem("Tools/Animation/Root Motion Fixer")]
    public static void ShowWindow()
    {
        GetWindow<AnimationRootMotionFixer>("根运动修复工具");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("动画根运动修复工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "此工具将 Bip001 骨骼的水平位移（XZ）转移到根节点，生成真正的根运动动画。\n" +
            "原理：绝区零的 FBX 文件将位移数据错误地保存在 Bip001 上，导致 Unity 无法识别根运动。\n" +
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
        }
        if (GUILayout.Button("11号", GUILayout.Width(60)))
        {
            sourceFolderPath = "Assets/Models/绝区零/11号/fbx";
            targetFolderPath = "Assets/Res/Animations/11/11_RootMotion";
        }
        if (GUILayout.Button("可琳", GUILayout.Width(60)))
        {
            sourceFolderPath = "Assets/Models/绝区零/可琳/fbx";
            targetFolderPath = "Assets/Res/Animations/Corin/Corin_RootMotion";
        }
        if (GUILayout.Button("猫又", GUILayout.Width(60)))
        {
            sourceFolderPath = "Assets/Models/绝区零/猫又/fbx";
            targetFolderPath = "Assets/Res/Animations/Maoyou/MaoYou_RootMotion";
        }
        if (GUILayout.Button("珂蕾妲", GUILayout.Width(60)))
        {
            sourceFolderPath = "Assets/Models/绝区零/珂蕾妲/fbx";
            targetFolderPath = "Assets/Res/Animations/KeLeiDa/KeLeiDa_RootMotion";
        }
        if (GUILayout.Button("简杜", GUILayout.Width(60)))
        {
            sourceFolderPath = "Assets/Models/绝区零/简杜/fbx";
            targetFolderPath = "Assets/Res/Animations/JianDu/JianDu_RootMotion";
        }
        if (GUILayout.Button("艾莲", GUILayout.Width(60)))
        {
            sourceFolderPath = "Assets/Models/绝区零/艾莲/fbx";
            targetFolderPath = "Assets/Res/Animations/Ellen/Ellen_RootMotion";
        }
        if (GUILayout.Button("雅", GUILayout.Width(60)))
        {
            sourceFolderPath = "Assets/Models/绝区零/雅/fbx";
            targetFolderPath = "Assets/Res/Animations/Ya/Ya_RootMotion";
        }
        if (GUILayout.Button("玲", GUILayout.Width(60)))
        {
            sourceFolderPath = "Assets/Models/绝区零/女主玲/fbx";
            targetFolderPath = "Assets/Res/Animations/Ling/Ling_RootMotion";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        includeYAxis = EditorGUILayout.Toggle("包含 Y 轴位移（跳跃）", includeYAxis);
        preserveBip001Y = EditorGUILayout.Toggle("保留 Bip001 Y 轴（身体起伏）", preserveBip001Y);

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
    }

    private void FixAllAnimations()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { sourceFolderPath });

        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("错误", $"在 {sourceFolderPath} 中未找到任何 FBX 文件", "确定");
            return;
        }

        if (!AssetDatabase.IsValidFolder(targetFolderPath))
        {
            string parentFolder = targetFolderPath.Substring(0, targetFolderPath.LastIndexOf('/'));
            string folderName = targetFolderPath.Substring(targetFolderPath.LastIndexOf('/') + 1);
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }

        // 重置统计
        newFileCount = 0;
        overwriteCount = 0;
        int successCount = 0;
        int failCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (ExtractAndFixAnimation(path))
                successCount++;
            else
                failCount++;
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

        if (!AssetDatabase.IsValidFolder(targetFolderPath))
        {
            string parentFolder = targetFolderPath.Substring(0, targetFolderPath.LastIndexOf('/'));
            string folderName = targetFolderPath.Substring(targetFolderPath.LastIndexOf('/') + 1);
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }

        // 重置统计
        newFileCount = 0;
        overwriteCount = 0;
        int successCount = 0;

        foreach (Object obj in selections)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (path.EndsWith(".FBX") || path.EndsWith(".fbx"))
            {
                if (ExtractAndFixAnimation(path))
                    successCount++;
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成",
            $"成功处理 {successCount} 个动画\n" +
            $"  · 新建: {newFileCount}\n" +
            $"  · 覆盖: {overwriteCount}",
            "确定");
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
                Debug.LogWarning($"[跳过] 未在 {fbxPath} 中找到动画片段");
                return false;
            }

            Debug.Log($"[处理] {sourceClip.name} (长度: {sourceClip.length:F2}s, 帧率: {sourceClip.frameRate})");

            // 创建新的动画片段
            AnimationClip newClip = new AnimationClip();
            newClip.name = sourceClip.name;
            newClip.frameRate = sourceClip.frameRate;

            // 复制所有曲线绑定
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(sourceClip);

            // 用于存储 Bip001 的位置曲线
            AnimationCurve bip001PosX = null;
            AnimationCurve bip001PosY = null;
            AnimationCurve bip001PosZ = null;

            // 第一遍：提取 Bip001 的位置曲线
            foreach (EditorCurveBinding binding in bindings)
            {
                if (binding.path == "Bip001" && binding.type == typeof(Transform))
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);

                    if (binding.propertyName == "m_LocalPosition.x")
                        bip001PosX = curve;
                    else if (binding.propertyName == "m_LocalPosition.y")
                        bip001PosY = curve;
                    else if (binding.propertyName == "m_LocalPosition.z")
                        bip001PosZ = curve;
                }
            }

            // 检查是否找到了 Bip001 的位移数据
            if (bip001PosX == null && bip001PosZ == null)
            {
                Debug.LogWarning($"[跳过] {sourceClip.name} 中未找到 Bip001 的位移数据");
                return false;
            }

            // 计算位移量（用于日志）
            float totalDisplacement = 0f;
            if (bip001PosX != null && bip001PosZ != null)
            {
                float startX = bip001PosX.Evaluate(0);
                float startZ = bip001PosZ.Evaluate(0);
                float endX = bip001PosX.Evaluate(sourceClip.length);
                float endZ = bip001PosZ.Evaluate(sourceClip.length);
                totalDisplacement = Vector2.Distance(new Vector2(startX, startZ), new Vector2(endX, endZ));
            }

            Debug.Log($"  → Bip001 总位移: {totalDisplacement:F3} 米");

            // 第二遍：复制曲线并转换
            foreach (EditorCurveBinding binding in bindings)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);

                // 处理 Bip001 的位置
                if (binding.path == "Bip001" && binding.type == typeof(Transform))
                {
                    if (binding.propertyName.StartsWith("m_LocalPosition"))
                    {
                        if (preserveBip001Y && binding.propertyName == "m_LocalPosition.y")
                        {
                            // 保留 Y 轴的身体起伏
                            AnimationUtility.SetEditorCurve(newClip, binding, curve);
                        }
                        else
                        {
                            // 其他位移清零（已转移到根节点）
                            AnimationCurve zeroCurve = new AnimationCurve();
                            zeroCurve.AddKey(0, 0);
                            zeroCurve.AddKey(sourceClip.length, 0);
                            AnimationUtility.SetEditorCurve(newClip, binding, zeroCurve);
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

            // 添加根节点的位移曲线（从 Bip001 转移过来）
            // 对于 Generic 动画，使用 m_LocalPosition
            if (bip001PosX != null)
            {
                EditorCurveBinding rootBindingX = new EditorCurveBinding
                {
                    path = "",
                    type = typeof(Transform),
                    propertyName = "m_LocalPosition.x"
                };
                AnimationUtility.SetEditorCurve(newClip, rootBindingX, bip001PosX);
            }

            if (includeYAxis && bip001PosY != null)
            {
                EditorCurveBinding rootBindingY = new EditorCurveBinding
                {
                    path = "",
                    type = typeof(Transform),
                    propertyName = "m_LocalPosition.y"
                };
                AnimationUtility.SetEditorCurve(newClip, rootBindingY, bip001PosY);
            }

            if (bip001PosZ != null)
            {
                EditorCurveBinding rootBindingZ = new EditorCurveBinding
                {
                    path = "",
                    type = typeof(Transform),
                    propertyName = "m_LocalPosition.z"
                };
                AnimationUtility.SetEditorCurve(newClip, rootBindingZ, bip001PosZ);
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

            string action = isOverwrite ? "覆盖" : "新建";
            Debug.Log($"  ✓ 成功修复 ({action}): {savePath}\n" +
                      $"     根节点位移: X={bip001PosX != null}, Y={includeYAxis && bip001PosY != null}, Z={bip001PosZ != null}\n" +
                      $"     Bip001 保留 Y 轴: {preserveBip001Y}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"处理 {fbxPath} 时出错: {e.Message}");
            return false;
        }
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
        Debug.LogWarning($"选择的路径不在 Unity 项目内: {absolutePath}");
        return absolutePath;
    }
}
