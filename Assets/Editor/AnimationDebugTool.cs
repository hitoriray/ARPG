using UnityEngine;
using UnityEditor;

/// <summary>
/// 动画调试工具 - 用于检查 FBX 动画的骨骼数据
/// </summary>
public class AnimationDebugTool : EditorWindow
{
    private AnimationClip clipToAnalyze;

    [MenuItem("Tools/Animation/Debug Animation Data")]
    public static void ShowWindow()
    {
        GetWindow<AnimationDebugTool>("动画数据调试");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("动画数据分析工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        clipToAnalyze = EditorGUILayout.ObjectField("动画 Clip", clipToAnalyze, typeof(AnimationClip), false) as AnimationClip;

        EditorGUILayout.Space(10);

        if (GUILayout.Button("分析骨骼数据", GUILayout.Height(30)))
        {
            if (clipToAnalyze != null)
            {
                AnalyzeAnimation(clipToAnalyze);
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "请先选择一个动画 Clip", "确定");
            }
        }
    }

    private void AnalyzeAnimation(AnimationClip clip)
    {
        RayDebug.Log($"========== 分析动画: {clip.name} ==========");
        RayDebug.Log($"长度: {clip.length:F3}s, 帧率: {clip.frameRate}");

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

        // 统计所有骨骼路径
        System.Collections.Generic.HashSet<string> bonePaths = new System.Collections.Generic.HashSet<string>();
        foreach (var binding in bindings)
        {
            if (binding.type == typeof(Transform))
            {
                bonePaths.Add(binding.path);
            }
        }

        RayDebug.Log($"\n总共有 {bonePaths.Count} 个骨骼节点:");
        foreach (var path in bonePaths)
        {
            RayDebug.Log($"  - {(string.IsNullOrEmpty(path) ? "[根节点]" : path)}");
        }

        // 检查根节点和 Bip001 的位置曲线
        RayDebug.Log("\n========== 位置曲线分析 ==========");

        CheckPositionCurves(clip, bindings, "");  // 根节点
        CheckPositionCurves(clip, bindings, "Bip001");
        CheckPositionCurves(clip, bindings, "Root/Bip001");
    }

    private void CheckPositionCurves(AnimationClip clip, EditorCurveBinding[] bindings, string path)
    {
        AnimationCurve posX = null, posY = null, posZ = null;

        foreach (var binding in bindings)
        {
            if (binding.path == path && binding.type == typeof(Transform))
            {
                if (binding.propertyName == "m_LocalPosition.x")
                    posX = AnimationUtility.GetEditorCurve(clip, binding);
                else if (binding.propertyName == "m_LocalPosition.y")
                    posY = AnimationUtility.GetEditorCurve(clip, binding);
                else if (binding.propertyName == "m_LocalPosition.z")
                    posZ = AnimationUtility.GetEditorCurve(clip, binding);
            }
        }

        if (posX != null || posY != null || posZ != null)
        {
            RayDebug.Log($"\n路径: {(string.IsNullOrEmpty(path) ? "[根节点]" : path)}");

            if (posX != null)
            {
                float startX = posX.Evaluate(0);
                float endX = posX.Evaluate(clip.length);
                RayDebug.Log($"  X轴: 起始={startX:F6}, 结束={endX:F6}, 位移={endX - startX:F6}");
            }

            if (posY != null)
            {
                float startY = posY.Evaluate(0);
                float endY = posY.Evaluate(clip.length);
                RayDebug.Log($"  Y轴: 起始={startY:F6}, 结束={endY:F6}, 位移={endY - startY:F6}");
            }

            if (posZ != null)
            {
                float startZ = posZ.Evaluate(0);
                float endZ = posZ.Evaluate(clip.length);
                RayDebug.Log($"  Z轴: 起始={startZ:F6}, 结束={endZ:F6}, 位移={endZ - startZ:F6}");
            }

            if (posX != null && posZ != null)
            {
                float startX = posX.Evaluate(0);
                float startZ = posZ.Evaluate(0);
                float endX = posX.Evaluate(clip.length);
                float endZ = posZ.Evaluate(clip.length);
                float totalDisplacement = Vector2.Distance(new Vector2(startX, startZ), new Vector2(endX, endZ));
                RayDebug.Log($"  总位移(XZ): {totalDisplacement:F6} 米");
            }
        }
        else
        {
            RayDebug.Log($"\n路径: {(string.IsNullOrEmpty(path) ? "[根节点]" : path)} - 无位置曲线");
        }
    }
}
