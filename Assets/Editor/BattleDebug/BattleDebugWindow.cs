using System.Linq;
using Editor.BattleDebug.Pages;
using UnityEditor;
using UnityEngine;

namespace Editor.BattleDebug
{
    /// <summary>
    /// ARPG ECS 调试窗口
    /// </summary>
    public class BattleDebugWindow : EditorWindow
    {
        private int _selectedTab;
        private readonly IBattleDebugPage[] _pages =
        {
            new OverviewPage(),
            new EntityPage(),
            new BuffPage(),
            new AttributePage(),
            new MemoryPage(),
            new TestPage(),
            new SettingsPage(),
        };
        
        private string[] _tabNames;
        private Vector2 _tabScrollPosition;
        private bool _autoRefresh = true;
        private IBattleDebugPage _currentPage;
        
        [MenuItem("Tools/ECS 调试窗口 %#D")]
        public static void ShowWindow()
        {
            var window = GetWindow<BattleDebugWindow>("ECS 调试窗口");
            window.minSize = new Vector2(600, 400);
        }
        
        private void OnEnable()
        {
            _tabNames = _pages.Select(p => $"{p.Icon} {p.Title}").ToArray();
            _currentPage = _pages[0];
            _currentPage.OnEnable();
            
            EditorApplication.update += OnEditorUpdate;
        }
        
        private void OnDisable()
        {
            _currentPage?.OnDisable();
            EditorApplication.update -= OnEditorUpdate;
        }
        
        private void OnEditorUpdate()
        {
            if (Application.isPlaying && _autoRefresh)
            {
                Repaint();
            }
        }
        
        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            
            // ============================================
            // 左侧：标签栏
            // ============================================
            EditorGUILayout.BeginVertical(GUILayout.Width(120), GUILayout.ExpandHeight(true));
            
            // 标题
            EditorGUILayout.LabelField("ECS Debug", BattleDebugStyles.TitleStyle);
            
            BattleDebugStyles.DrawSeparator();
            
            // 标签按钮
            _tabScrollPosition = EditorGUILayout.BeginScrollView(_tabScrollPosition);
            
            for (int i = 0; i < _pages.Length; i++)
            {
                var isSelected = _selectedTab == i;
                var style = isSelected 
                    ? BattleDebugStyles.TabButtonActiveStyle 
                    : BattleDebugStyles.TabButtonStyle;
                
                if (GUILayout.Button(_tabNames[i], style))
                {
                    if (_selectedTab != i)
                    {
                        _currentPage?.OnDisable();
                        _selectedTab = i;
                        _currentPage = _pages[i];
                        _currentPage.OnEnable();
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            // 底部控制区
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginVertical("box");
            
            // 状态指示
            var statusColor = Application.isPlaying 
                ? BattleDebugStyles.SuccessColor 
                : BattleDebugStyles.DisabledColor;
            var statusText = Application.isPlaying ? "运行中" : "未运行";
            var statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = statusColor },
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField(statusText, statusStyle);
            
            // 刷新控制
            _autoRefresh = EditorGUILayout.ToggleLeft("自动刷新", _autoRefresh);
            
            if (!_autoRefresh)
            {
                if (GUILayout.Button("手动刷新"))
                {
                    Repaint();
                }
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndVertical();
            
            // ============================================
            // 右侧：内容区域 (自适应宽度)
            // ============================================
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            // 页面标题
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(_pages[_selectedTab].Title, BattleDebugStyles.HeaderStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            // 页面内容
            EditorGUILayout.BeginVertical("box");
            _pages[_selectedTab].OnGUI();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
    }
}
