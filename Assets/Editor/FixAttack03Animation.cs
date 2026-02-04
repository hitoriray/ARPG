using UnityEditor;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// 修复 Attack03_RM 动画的结束位移问题
    /// 该动画是旋转一圈回原位，但 Z 轴结束时有约 0.134m 的位移，需要修正为 0
    /// </summary>
    public class FixAttack03Animation : EditorWindow
    {
        [MenuItem("Tools/Animation/修复 Attack03 动画位移")]
        public static void ShowWindow()
        {
            GetWindow<FixAttack03Animation>("修复 Attack03 动画");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Attack03 动画位移修复工具", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "该工具会修复 Attack03_RM.anim 的根运动曲线，使动画结束时回到原点位置。\n" +
                "问题：动画结束时 Z 轴有约 0.134m 的位移偏差。",
                MessageType.Info);
            EditorGUILayout.Space();

            if (GUILayout.Button("修复动画", GUILayout.Height(40)))
            {
                FixAnimation();
            }
        }

        private void FixAnimation()
        {
            string animPath = "Assets/Res/Animations/1004_Katixiya/RootMotion/Attack03_RM.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
            
            if (clip == null)
            {
                EditorUtility.DisplayDialog("错误", $"找不到动画文件：{animPath}", "确定");
                return;
            }

            // 获取所有曲线绑定
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            bool modified = false;
            System.Text.StringBuilder log = new System.Text.StringBuilder();

            foreach (var binding in bindings)
            {
                // 修复 Root 骨骼的位移曲线（使结束位置回到起始位置）
                if (binding.path == "Root" && 
                    (binding.propertyName == "m_LocalPosition.x" || 
                     binding.propertyName == "m_LocalPosition.z"))
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve != null && curve.length > 1)
                    {
                        Keyframe firstKey = curve.keys[0];
                        Keyframe lastKey = curve.keys[curve.length - 1];
                        
                        // 检查结束值是否与起始值不同
                        if (Mathf.Abs(lastKey.value - firstKey.value) > 0.01f)
                        {
                            log.AppendLine($"修复 {binding.path}.{binding.propertyName}:");
                            log.AppendLine($"  起始值: {firstKey.value}");
                            log.AppendLine($"  结束值: {lastKey.value} -> {firstKey.value}");
                            
                            // 设置最后一帧的值为第一帧的值
                            curve.MoveKey(curve.length - 1, new Keyframe(
                                lastKey.time,
                                firstKey.value,  // 回到起始位置
                                0f,              // inTangent 设为 0
                                0f               // outTangent 设为 0
                            ));
                            
                            AnimationUtility.SetEditorCurve(clip, binding, curve);
                            modified = true;
                        }
                    }
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
                Debug.Log($"[FixAttack03] 修复完成:\n{log}");
                EditorUtility.DisplayDialog("成功", $"动画已修复！\n\n{log}", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", 
                    "未找到需要修复的曲线。\n\n可能的原因：\n- 动画已经是正确的\n- Root 骨骼的位移曲线起始和结束值相同", 
                    "确定");
            }
        }
    }
}
