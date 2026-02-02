using UnityEditor;
using UnityEngine;

namespace Editor.BattleDebug
{
    /// <summary>
    /// 调试窗口样式定义
    /// </summary>
    public static class BattleDebugStyles
    {
        // ============================================
        // 颜色定义
        // ============================================
        
        public static readonly Color HeaderColor = new Color(0.4f, 0.7f, 1f, 1f);
        public static readonly Color SubHeaderColor = new Color(0.6f, 0.85f, 1f, 1f);
        public static readonly Color SuccessColor = new Color(0.4f, 0.9f, 0.4f, 1f);
        public static readonly Color WarningColor = new Color(1f, 0.8f, 0.3f, 1f);
        public static readonly Color ErrorColor = new Color(1f, 0.4f, 0.4f, 1f);
        public static readonly Color DisabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        public static readonly Color SeparatorColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        public static readonly Color BoxBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
        
        // 阵营颜色
        public static readonly Color PlayerColor = new Color(0.3f, 0.7f, 1f, 1f);     // 玩家阵营 - 蓝色
        public static readonly Color EnemyColor = new Color(1f, 0.4f, 0.4f, 1f);      // 敌方阵营 - 红色
        public static readonly Color NeutralColor = new Color(1f, 0.85f, 0.4f, 1f);   // 中立阵营 - 黄色
        public static readonly Color BuffColor = new Color(0.5f, 0.9f, 0.5f, 1f);     // Buff - 绿色
        public static readonly Color ModifierColor = new Color(0.8f, 0.6f, 1f, 1f);   // Modifier - 紫色
        
        // 图表颜色
        public static readonly Color ChartLineColor = new Color(0.3f, 0.8f, 0.3f, 1f);
        public static readonly Color ChartGridColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        public static readonly Color ChartBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        
        // ============================================
        // 间距常量
        // ============================================
        
        public const float SmallSpace = 5f;
        public const float MediumSpace = 10f;
        public const float LargeSpace = 20f;
        public const float SectionSpace = 15f;
        
        // ============================================
        // 样式缓存
        // ============================================
        
        private static GUIStyle _titleStyle;
        private static GUIStyle _headerStyle;
        private static GUIStyle _subHeaderStyle;
        private static GUIStyle _successStyle;
        private static GUIStyle _warningStyle;
        private static GUIStyle _errorStyle;
        private static GUIStyle _disabledStyle;
        private static GUIStyle _centerStyle;
        private static GUIStyle _boxStyle;
        private static GUIStyle _tabButtonStyle;
        private static GUIStyle _tabButtonActiveStyle;
        private static GUIStyle _valueStyle;
        
        public static GUIStyle TitleStyle => _titleStyle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(5, 5, 5, 5)
        };
        
        public static GUIStyle HeaderStyle => _headerStyle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = HeaderColor }
        };
        
        public static GUIStyle SubHeaderStyle => _subHeaderStyle ??= new GUIStyle(EditorStyles.label)
        {
            fontSize = 12,
            normal = { textColor = SubHeaderColor }
        };
        
        public static GUIStyle SuccessStyle => _successStyle ??= new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = SuccessColor }
        };
        
        public static GUIStyle WarningStyle => _warningStyle ??= new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = WarningColor }
        };
        
        public static GUIStyle ErrorStyle => _errorStyle ??= new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = ErrorColor }
        };
        
        public static GUIStyle DisabledStyle => _disabledStyle ??= new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = DisabledColor }
        };
        
        public static GUIStyle CenterStyle => _centerStyle ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter
        };
        
        public static GUIStyle BoxStyle => _boxStyle ??= new GUIStyle("box")
        {
            padding = new RectOffset(10, 10, 10, 10),
            margin = new RectOffset(5, 5, 5, 5)
        };
        
        public static GUIStyle TabButtonStyle => _tabButtonStyle ??= new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fixedHeight = 32,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(10, 10, 5, 5)
        };
        
        public static GUIStyle TabButtonActiveStyle
        {
            get
            {
                if (_tabButtonActiveStyle == null)
                {
                    _tabButtonActiveStyle = new GUIStyle(TabButtonStyle)
                    {
                        fontStyle = FontStyle.Bold
                    };
                    _tabButtonActiveStyle.normal.textColor = HeaderColor;
                }
                return _tabButtonActiveStyle;
            }
        }
        
        public static GUIStyle ValueStyle => _valueStyle ??= new GUIStyle(EditorStyles.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };
        
        // ============================================
        // 工具方法
        // ============================================
        
        /// <summary>
        /// 绘制分隔线
        /// </summary>
        public static void DrawSeparator()
        {
            GUILayout.Space(SmallSpace);
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(1));
            EditorGUI.DrawRect(rect, SeparatorColor);
            GUILayout.Space(SmallSpace);
        }
        
        /// <summary>
        /// 绘制搜索框
        /// </summary>
        public static void DrawSearchField(ref string searchTerm, string placeholder = "搜索...")
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("🔍", GUILayout.Width(20));
            searchTerm = EditorGUILayout.TextField(searchTerm);
            if (GUILayout.Button("✕", GUILayout.Width(25)))
            {
                searchTerm = string.Empty;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制统计卡片
        /// </summary>
        public static void DrawStatCard(string label, string value, Color? valueColor = null)
        {
            EditorGUILayout.BeginVertical(BoxStyle, GUILayout.MinWidth(100));
            EditorGUILayout.LabelField(label, DisabledStyle);
            var style = new GUIStyle(ValueStyle);
            if (valueColor.HasValue)
                style.normal.textColor = valueColor.Value;
            EditorGUILayout.LabelField(value, style);
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制带颜色的进度条
        /// </summary>
        public static void DrawProgressBar(float value, float max, Color color, float height = 20f)
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(height));
            
            // 背景
            EditorGUI.DrawRect(rect, BoxBackgroundColor);
            
            // 进度
            float ratio = max > 0 ? Mathf.Clamp01(value / max) : 0;
            var progressRect = new Rect(rect.x, rect.y, rect.width * ratio, rect.height);
            EditorGUI.DrawRect(progressRect, color);
            
            // 文本
            var text = $"{value:F1} / {max:F1} ({ratio * 100:F0}%)";
            EditorGUI.LabelField(rect, text, CenterStyle);
        }
        
        /// <summary>
        /// 格式化字节大小
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
