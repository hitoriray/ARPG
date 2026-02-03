using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// 分析动画曲线工具 - 用于找出哪个骨骼包含位移数据
/// </summary>
public class AnimationCurveAnalyzer : EditorWindow
{
    private string fbxPath = "Assets/Res/MC/Katixiya/fbx/Attack01.fbx";
    private Vector2 scrollPosition;
    private string analysisResult = "";
    
    [MenuItem("Tools/动画/动画曲线分析")]
    public static void ShowWindow()
    {
        GetWindow<AnimationCurveAnalyzer>("动画曲线分析");
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("动画曲线分析工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("此工具用于分析 FBX 文件中动画的骨骼位移数据，找出哪个骨骼包含根运动。", MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        // FBX 路径选择
        EditorGUILayout.BeginHorizontal();
        fbxPath = EditorGUILayout.TextField("FBX 路径", fbxPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string selectedPath = EditorUtility.OpenFilePanel("选择 FBX 文件", "Assets", "fbx");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                fbxPath = ConvertToRelativePath(selectedPath);
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("分析动画曲线", GUILayout.Height(30)))
        {
            AnalyzeAnimation();
        }
        
        EditorGUILayout.Space(10);
        
        // 显示分析结果
        if (!string.IsNullOrEmpty(analysisResult))
        {
            EditorGUILayout.LabelField("分析结果:", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));
            EditorGUILayout.TextArea(analysisResult, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("复制到剪贴板"))
            {
                GUIUtility.systemCopyBuffer = analysisResult;
            }
        }
    }
    
    private void AnalyzeAnimation()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== 分析 FBX: {fbxPath} ===\n");
        
        // 加载 FBX
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
            analysisResult = "未找到动画片段！";
            return;
        }
        
        sb.AppendLine($"动画片段: {sourceClip.name}");
        sb.AppendLine($"时长: {sourceClip.length:F3} 秒");
        sb.AppendLine($"帧率: {sourceClip.frameRate} FPS");
        sb.AppendLine();
        
        // 获取所有曲线绑定
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(sourceClip);
        
        // 收集所有骨骼的位移数据
        Dictionary<string, BoneMotionData> boneMotions = new Dictionary<string, BoneMotionData>();
        
        foreach (EditorCurveBinding binding in bindings)
        {
            // 只分析位置曲线
            if (binding.type == typeof(Transform) && binding.propertyName.StartsWith("m_LocalPosition"))
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                
                if (!boneMotions.ContainsKey(binding.path))
                {
                    boneMotions[binding.path] = new BoneMotionData { BonePath = binding.path };
                }
                
                BoneMotionData data = boneMotions[binding.path];
                
                float startValue = curve.Evaluate(0);
                float endValue = curve.Evaluate(sourceClip.length);
                float delta = endValue - startValue;
                
                // 计算曲线的最大变化范围
                float minVal = float.MaxValue;
                float maxVal = float.MinValue;
                foreach (Keyframe key in curve.keys)
                {
                    if (key.value < minVal) minVal = key.value;
                    if (key.value > maxVal) maxVal = key.value;
                }
                float range = maxVal - minVal;
                
                if (binding.propertyName.EndsWith(".x"))
                {
                    data.DeltaX = delta;
                    data.RangeX = range;
                    data.StartX = startValue;
                    data.EndX = endValue;
                    data.KeyCountX = curve.keys.Length;
                }
                else if (binding.propertyName.EndsWith(".y"))
                {
                    data.DeltaY = delta;
                    data.RangeY = range;
                    data.StartY = startValue;
                    data.EndY = endValue;
                    data.KeyCountY = curve.keys.Length;
                }
                else if (binding.propertyName.EndsWith(".z"))
                {
                    data.DeltaZ = delta;
                    data.RangeZ = range;
                    data.StartZ = startValue;
                    data.EndZ = endValue;
                    data.KeyCountZ = curve.keys.Length;
                }
            }
        }
        
        // 按位移量排序（XZ 平面位移）
        var sortedBones = boneMotions.Values
            .OrderByDescending(b => Mathf.Sqrt(b.RangeX * b.RangeX + b.RangeZ * b.RangeZ))
            .ToList();
        
        sb.AppendLine("=== 骨骼位移分析（按 XZ 平面位移范围排序）===\n");
        sb.AppendLine("注意: 'Range' 表示整个动画中该轴的最大变化范围");
        sb.AppendLine("      'Delta' 表示从首帧到尾帧的位移变化\n");
        
        int count = 0;
        foreach (var bone in sortedBones)
        {
            float xzRange = Mathf.Sqrt(bone.RangeX * bone.RangeX + bone.RangeZ * bone.RangeZ);
            float xzDelta = Mathf.Sqrt(bone.DeltaX * bone.DeltaX + bone.DeltaZ * bone.DeltaZ);
            
            // 只显示有明显位移的骨骼（XZ范围 > 0.01）
            if (xzRange > 0.01f || Mathf.Abs(bone.RangeY) > 0.01f)
            {
                sb.AppendLine($"【{(string.IsNullOrEmpty(bone.BonePath) ? "ROOT (空路径)" : bone.BonePath)}】");
                sb.AppendLine($"  XZ 平面范围: {xzRange:F4}   XZ 位移Delta: {xzDelta:F4}");
                sb.AppendLine($"  X: Start={bone.StartX:F4}, End={bone.EndX:F4}, Delta={bone.DeltaX:F4}, Range={bone.RangeX:F4}, Keys={bone.KeyCountX}");
                sb.AppendLine($"  Y: Start={bone.StartY:F4}, End={bone.EndY:F4}, Delta={bone.DeltaY:F4}, Range={bone.RangeY:F4}, Keys={bone.KeyCountY}");
                sb.AppendLine($"  Z: Start={bone.StartZ:F4}, End={bone.EndZ:F4}, Delta={bone.DeltaZ:F4}, Range={bone.RangeZ:F4}, Keys={bone.KeyCountZ}");
                sb.AppendLine();
                count++;
                
                if (count >= 20) // 只显示前20个
                {
                    sb.AppendLine($"... 还有 {sortedBones.Count - 20} 个骨骼");
                    break;
                }
            }
        }
        
        // 找出最可能包含根运动的骨骼
        sb.AppendLine("\n=== 根运动候选骨骼 ===\n");
        
        var candidates = sortedBones
            .Where(b => Mathf.Sqrt(b.RangeX * b.RangeX + b.RangeZ * b.RangeZ) > 0.1f)
            .Take(5)
            .ToList();
            
        if (candidates.Count > 0)
        {
            sb.AppendLine("以下骨骼在 XZ 平面有较大位移，可能是根运动的来源：");
            foreach (var bone in candidates)
            {
                float xzRange = Mathf.Sqrt(bone.RangeX * bone.RangeX + bone.RangeZ * bone.RangeZ);
                sb.AppendLine($"  → {(string.IsNullOrEmpty(bone.BonePath) ? "ROOT" : bone.BonePath)}: XZ范围={xzRange:F4}");
            }
        }
        else
        {
            sb.AppendLine("未找到明显的根运动骨骼！可能动画本身就是 In-Place 的。");
        }
        
        // 检查现有工具查找的骨骼
        sb.AppendLine("\n=== 现有工具检查 ===\n");
        var bip001 = sortedBones.FirstOrDefault(b => b.BonePath == "Bip001" || b.BonePath.EndsWith("/Bip001"));
        if (bip001 != null)
        {
            float xzRange = Mathf.Sqrt(bip001.RangeX * bip001.RangeX + bip001.RangeZ * bip001.RangeZ);
            sb.AppendLine($"找到 Bip001 骨骼: {bip001.BonePath}");
            sb.AppendLine($"  XZ 位移范围: {xzRange:F4}");
            if (xzRange < 0.1f)
            {
                sb.AppendLine("  ⚠️ Bip001 的位移很小，位移数据可能在其他骨骼上！");
            }
        }
        else
        {
            sb.AppendLine("⚠️ 未找到 Bip001 骨骼！这个模型可能使用不同的骨骼命名。");
        }
        
        // 列出所有骨骼路径
        sb.AppendLine("\n=== 所有骨骼路径 ===\n");
        var allPaths = bindings
            .Select(b => b.path)
            .Distinct()
            .OrderBy(p => p)
            .ToList();
            
        foreach (var path in allPaths)
        {
            sb.AppendLine(string.IsNullOrEmpty(path) ? "  (ROOT - 空路径)" : $"  {path}");
        }
        
        analysisResult = sb.ToString();
        Debug.Log(analysisResult);
    }
    
    private class BoneMotionData
    {
        public string BonePath;
        public float StartX, StartY, StartZ;
        public float EndX, EndY, EndZ;
        public float DeltaX, DeltaY, DeltaZ;
        public float RangeX, RangeY, RangeZ;
        public int KeyCountX, KeyCountY, KeyCountZ;
    }
    
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
        
        return absolutePath;
    }
}
