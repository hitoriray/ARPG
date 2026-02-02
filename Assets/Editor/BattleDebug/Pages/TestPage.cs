using Battle.ECS;
using Battle.ECS.Core;
using FixMath;
using UnityEditor;
using UnityEngine;

namespace Editor.BattleDebug.Pages
{
    /// <summary>
    /// 测试页 - 调试工具和快捷操作
    /// </summary>
    public class TestPage : IBattleDebugPage
    {
        public string Title => "测试";
        public string Icon => "[T]";
        
        private Vector2 _scrollPosition;
        
        // 时间控制
        private float _timeScale = 1f;
        
        // 实体创建参数
        private Vector3 _spawnPosition = Vector3.zero;
        private int _spawnCount = 1;
        
        public void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("仅在播放模式下可用", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }
            
            var runner = BattleEcsRunner.Instance;
            if (runner?.Context == null)
            {
                EditorGUILayout.HelpBox("战斗上下文不可用", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }
            
            var context = runner.Context;
            
            // ============================================
            // 时间控制
            // ============================================
            EditorGUILayout.LabelField("时间控制", BattleDebugStyles.HeaderStyle);
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("时间缩放:", GUILayout.Width(70));
            _timeScale = EditorGUILayout.Slider(_timeScale, 0.1f, 3f);
            
            if (GUILayout.Button("0.5x", GUILayout.Width(40))) _timeScale = 0.5f;
            if (GUILayout.Button("1x", GUILayout.Width(40))) _timeScale = 1f;
            if (GUILayout.Button("2x", GUILayout.Width(40))) _timeScale = 2f;
            EditorGUILayout.EndHorizontal();
            
            // 应用时间缩放
            Time.timeScale = _timeScale;
            EditorGUILayout.LabelField($"当前: {Time.timeScale:F2}x", BattleDebugStyles.DisabledStyle);
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(BattleDebugStyles.MediumSpace);
            
            // ============================================
            // 战斗状态控制
            // ============================================
            EditorGUILayout.LabelField("战斗状态", BattleDebugStyles.HeaderStyle);
            EditorGUILayout.BeginVertical("box");
            
            var state = context.State.Value;
            EditorGUILayout.LabelField($"当前状态: {state}", 
                state == BattleState.Running ? BattleDebugStyles.SuccessStyle : BattleDebugStyles.WarningStyle);
            
            EditorGUILayout.BeginHorizontal();
            
            if (state == BattleState.Running)
            {
                if (GUILayout.Button("暂停", GUILayout.Height(30)))
                {
                    context.State.Value = BattleState.Paused;
                }
            }
            else
            {
                if (GUILayout.Button("继续", GUILayout.Height(30)))
                {
                    context.State.Value = BattleState.Running;
                }
            }
            
            if (GUILayout.Button("单帧步进", GUILayout.Height(30)))
            {
                // 暂停后执行一帧
                context.State.Value = BattleState.Paused;
                context.LogicTime.Update();
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(BattleDebugStyles.MediumSpace);
            
            // ============================================
            // 实体创建
            // ============================================
            EditorGUILayout.LabelField("实体创建", BattleDebugStyles.HeaderStyle);
            EditorGUILayout.BeginVertical("box");
            
            _spawnPosition = EditorGUILayout.Vector3Field("生成位置", _spawnPosition);
            _spawnCount = EditorGUILayout.IntSlider("数量", _spawnCount, 1, 100);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("创建测试实体", GUILayout.Height(30)))
            {
                for (int i = 0; i < _spawnCount; i++)
                {
                    var offset = Random.insideUnitSphere * 2f;
                    var pos = _spawnPosition + offset;
                    context.World.Create(
                        new Battle.ECS.Component.Position((TSVector3)pos)
                    );
                }
                RayDebug.Log($"[BattleDebug] 创建了 {_spawnCount} 个测试实体");
            }
            
            if (GUILayout.Button("在玩家位置创建", GUILayout.Height(30)))
            {
                var player = GameObject.FindObjectOfType<Player.PlayerController>();
                if (player != null)
                {
                    _spawnPosition = player.transform.position;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(BattleDebugStyles.MediumSpace);
            
            // ============================================
            // 调试信息
            // ============================================
            EditorGUILayout.LabelField("调试信息", BattleDebugStyles.HeaderStyle);
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField($"逻辑帧: {context.LogicTime.FrameCount}");
            EditorGUILayout.LabelField($"逻辑时间: {(float)context.LogicTime.Time:F3}s");
            EditorGUILayout.LabelField($"帧间隔: {(float)context.LogicTime.DeltaTime * 1000:F1}ms");
            EditorGUILayout.LabelField($"实体数: {context.World?.Size ?? 0}");
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(BattleDebugStyles.MediumSpace);
            
            // ============================================
            // 危险操作
            // ============================================
            EditorGUILayout.LabelField("危险操作", BattleDebugStyles.HeaderStyle);
            EditorGUILayout.BeginVertical("box");
            
            GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
            if (GUILayout.Button("清空所有实体", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("警告", "确定要清空所有实体吗？此操作不可撤销！", "确定", "取消"))
                {
                    context.World?.Clear();
                    RayDebug.Log("[BattleDebug] 已清空所有实体");
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
        }
    }
}
