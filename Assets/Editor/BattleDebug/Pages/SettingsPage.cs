using UnityEditor;
using UnityEngine;

namespace Editor.BattleDebug.Pages
{
    /// <summary>
    /// 设置页 - 调试设置
    /// </summary>
    public class SettingsPage : IBattleDebugPage
    {
        public string Title => "设置";
        public string Icon => "[S]";
        
        private Vector2 _scrollPosition;
        
        // Gizmos设置
        private static bool _drawGizmos = true;
        private static bool _drawEntityInfo = true;
        private static bool _drawBoundingBox = false;
        private static bool _drawMovementDirection = false;
        
        private static Color _entityInfoColor = new Color(1f, 1f, 1f, 0.8f);
        private static Color _boundingBoxColor = new Color(0f, 1f, 0f, 0.5f);
        private static Color _movementColor = new Color(1f, 0.5f, 0f, 1f);
        
        // 日志设置
        private static bool _enableDebugLog = true;
        private static bool _enableVerboseLog = false;
        
        // 属性访问器
        public static bool DrawGizmos => _drawGizmos;
        public static bool DrawEntityInfo => _drawEntityInfo;
        public static bool DrawBoundingBox => _drawBoundingBox;
        public static bool DrawMovementDirection => _drawMovementDirection;
        public static Color EntityInfoColor => _entityInfoColor;
        public static Color BoundingBoxColor => _boundingBoxColor;
        public static Color MovementColor => _movementColor;
        public static bool EnableDebugLog => _enableDebugLog;
        public static bool EnableVerboseLog => _enableVerboseLog;
        
        public void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // ============================================
            // 编译符号
            // ============================================
            EditorGUILayout.LabelField("编译设置", BattleDebugStyles.HeaderStyle);
            EditorGUILayout.BeginVertical("box");
            
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);
            
            EditorGUILayout.BeginHorizontal();
            if (defines.Contains("BATTLE_DEBUG"))
            {
                EditorGUILayout.LabelField("BATTLE_DEBUG", BattleDebugStyles.SuccessStyle, GUILayout.Width(120));
                if (GUILayout.Button("关闭", GUILayout.Width(60)))
                {
                    defines = defines.Replace("BATTLE_DEBUG", "").Replace(";;", ";").Trim(';');
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(
                        EditorUserBuildSettings.selectedBuildTargetGroup, defines);
                }
            }
            else
            {
                EditorGUILayout.LabelField("BATTLE_DEBUG", BattleDebugStyles.DisabledStyle, GUILayout.Width(120));
                if (GUILayout.Button("开启", GUILayout.Width(60)))
                {
                    defines = defines + ";BATTLE_DEBUG";
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(
                        EditorUserBuildSettings.selectedBuildTargetGroup, defines);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("开启 BATTLE_DEBUG 后会显示额外的调试信息，但可能影响性能。", MessageType.Info);
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(BattleDebugStyles.MediumSpace);
            
            // ============================================
            // Gizmos设置
            // ============================================
            EditorGUILayout.LabelField("Gizmos 设置", BattleDebugStyles.HeaderStyle);
            EditorGUILayout.BeginVertical("box");
            
            _drawGizmos = EditorGUILayout.Toggle("启用Gizmos绘制", _drawGizmos);
            
            EditorGUI.BeginDisabledGroup(!_drawGizmos);
            EditorGUI.indentLevel++;
            
            _drawEntityInfo = EditorGUILayout.Toggle("显示实体信息", _drawEntityInfo);
            if (_drawEntityInfo)
            {
                EditorGUI.indentLevel++;
                _entityInfoColor = EditorGUILayout.ColorField("文字颜色", _entityInfoColor);
                EditorGUI.indentLevel--;
            }
            
            _drawBoundingBox = EditorGUILayout.Toggle("显示包围盒", _drawBoundingBox);
            if (_drawBoundingBox)
            {
                EditorGUI.indentLevel++;
                _boundingBoxColor = EditorGUILayout.ColorField("包围盒颜色", _boundingBoxColor);
                EditorGUI.indentLevel--;
            }
            
            _drawMovementDirection = EditorGUILayout.Toggle("显示移动方向", _drawMovementDirection);
            if (_drawMovementDirection)
            {
                EditorGUI.indentLevel++;
                _movementColor = EditorGUILayout.ColorField("方向线颜色", _movementColor);
                EditorGUI.indentLevel--;
            }
            
            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(BattleDebugStyles.MediumSpace);
            
            // ============================================
            // 日志设置
            // ============================================
            EditorGUILayout.LabelField("日志设置", BattleDebugStyles.HeaderStyle);
            EditorGUILayout.BeginVertical("box");
            
            _enableDebugLog = EditorGUILayout.Toggle("启用调试日志", _enableDebugLog);
            
            EditorGUI.BeginDisabledGroup(!_enableDebugLog);
            EditorGUI.indentLevel++;
            _enableVerboseLog = EditorGUILayout.Toggle("详细日志模式", _enableVerboseLog);
            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(BattleDebugStyles.MediumSpace);
            
            // ============================================
            // 快捷操作
            // ============================================
            EditorGUILayout.LabelField("快捷操作", BattleDebugStyles.HeaderStyle);
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("重置所有设置", GUILayout.Height(25)))
            {
                ResetToDefaults();
            }
            
            if (GUILayout.Button("打开项目设置", GUILayout.Height(25)))
            {
                SettingsService.OpenProjectSettings("Project/Player");
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(BattleDebugStyles.MediumSpace);
            
            // ============================================
            // 关于
            // ============================================
            EditorGUILayout.LabelField("关于", BattleDebugStyles.HeaderStyle);
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("ARPG ECS 调试窗口", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("版本: 1.0.0", BattleDebugStyles.DisabledStyle);
            EditorGUILayout.LabelField("基于 Arch ECS 框架", BattleDebugStyles.DisabledStyle);
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void ResetToDefaults()
        {
            _drawGizmos = true;
            _drawEntityInfo = true;
            _drawBoundingBox = false;
            _drawMovementDirection = false;
            _entityInfoColor = new Color(1f, 1f, 1f, 0.8f);
            _boundingBoxColor = new Color(0f, 1f, 0f, 0.5f);
            _movementColor = new Color(1f, 0.5f, 0f, 1f);
            _enableDebugLog = true;
            _enableVerboseLog = false;
        }
    }
}
