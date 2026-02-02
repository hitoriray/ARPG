using Battle.ECS;
using UnityEditor;
using UnityEngine;

namespace Editor.BattleDebug.Pages
{
    /// <summary>
    /// 概览页 - 显示战斗状态和实时统计
    /// </summary>
    public class OverviewPage : IBattleDebugPage
    {
        public string Title => "概览";
        public string Icon => "[O]";
        
        private Vector2 _scrollPosition;
        
        // 实时图表
        private readonly RealtimeChart _fpsChart;
        private readonly RealtimeChart _entityChart;
        private readonly RealtimeChart _deltaTimeChart;
        
        private float _fps;
        private float _smoothFps;
        
        public OverviewPage()
        {
            _fpsChart = new RealtimeChart("FPS", 120, new Color(0.3f, 0.9f, 0.3f));
            _entityChart = new RealtimeChart("实体数", 120, new Color(0.3f, 0.7f, 1f));
            _deltaTimeChart = new RealtimeChart("逻辑帧时间(ms)", 120, new Color(1f, 0.7f, 0.3f));
        }
        
        public void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // 计算FPS
            UpdateFPS();
            
            var runner = BattleEcsRunner.Instance;
            
            // 状态检查
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("仅在播放模式下可用", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }
            
            if (runner?.Context == null)
            {
                EditorGUILayout.HelpBox("战斗上下文不可用。请确保 BattleEcsRunner 已初始化。", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }
            
            var context = runner.Context;
            
            // ============================================
            // 战斗状态区域
            // ============================================
            EditorGUILayout.LabelField("战斗状态", BattleDebugStyles.HeaderStyle);
            EditorGUILayout.BeginHorizontal();
            
            var state = context.State.Value;
            var stateColor = state switch
            {
                Battle.ECS.Core.BattleState.Running => BattleDebugStyles.SuccessColor,
                Battle.ECS.Core.BattleState.Paused => BattleDebugStyles.WarningColor,
                _ => BattleDebugStyles.DisabledColor
            };
            BattleDebugStyles.DrawStatCard("状态", state.ToString(), stateColor);
            BattleDebugStyles.DrawStatCard("逻辑帧", context.LogicTime.FrameCount.ToString());
            BattleDebugStyles.DrawStatCard("逻辑时间", $"{(float)context.LogicTime.Time:F2}s");
            BattleDebugStyles.DrawStatCard("逻辑帧间隔", $"{(float)context.LogicTime.DeltaTime * 1000:F0}ms");
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(BattleDebugStyles.MediumSpace);
            
            // ============================================
            // 实体统计区域
            // ============================================
            EditorGUILayout.LabelField("实体统计", BattleDebugStyles.HeaderStyle);
            
            var world = context.World;
            int entityCount = 0;
            int archetypeCount = 0;
            
            if (world != null)
            {
                entityCount = world.Size;
                archetypeCount = world.Archetypes?.Count ?? 0;
            }
            
            // 更新图表
            _entityChart.AddSample(entityCount);
            _fpsChart.AddSample(_smoothFps);
            _deltaTimeChart.AddSample((float)context.LogicTime.DeltaTime * 1000f);
            
            EditorGUILayout.BeginHorizontal();
            BattleDebugStyles.DrawStatCard("实体总数", entityCount.ToString(), BattleDebugStyles.HeaderColor);
            BattleDebugStyles.DrawStatCard("Archetype数", archetypeCount.ToString());
            BattleDebugStyles.DrawStatCard("FPS", $"{_smoothFps:F0}", _smoothFps < 30 ? BattleDebugStyles.ErrorColor : BattleDebugStyles.SuccessColor);
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(BattleDebugStyles.MediumSpace);
            
            // ============================================
            // 实时图表区域
            // ============================================
            EditorGUILayout.LabelField("性能监控", BattleDebugStyles.HeaderStyle);
            
            // FPS图表
            EditorGUILayout.BeginVertical(BattleDebugStyles.BoxStyle);
            var fpsChartRect = GUILayoutUtility.GetRect(0, 80, GUILayout.ExpandWidth(true));
            _fpsChart.Draw(fpsChartRect);
            EditorGUILayout.EndVertical();
            
            // 实体数图表
            EditorGUILayout.BeginVertical(BattleDebugStyles.BoxStyle);
            var entityChartRect = GUILayoutUtility.GetRect(0, 80, GUILayout.ExpandWidth(true));
            _entityChart.Draw(entityChartRect);
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(BattleDebugStyles.MediumSpace);
            
            // ============================================
            // 快捷操作区域
            // ============================================
            EditorGUILayout.LabelField("快捷操作", BattleDebugStyles.HeaderStyle);
            
            EditorGUILayout.BeginHorizontal();
            
            if (state == Battle.ECS.Core.BattleState.Running)
            {
                if (GUILayout.Button("|| 暂停", GUILayout.Height(30)))
                {
                    context.State.Value = Battle.ECS.Core.BattleState.Paused;
                }
            }
            else if (state == Battle.ECS.Core.BattleState.Paused)
            {
                if (GUILayout.Button("> 继续", GUILayout.Height(30)))
                {
                    context.State.Value = Battle.ECS.Core.BattleState.Running;
                }
            }
            
            if (GUILayout.Button("清空图表", GUILayout.Height(30)))
            {
                _fpsChart.Clear();
                _entityChart.Clear();
                _deltaTimeChart.Clear();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void UpdateFPS()
        {
            // 使用Unity的deltaTime计算实时FPS
            if (Time.deltaTime > 0)
            {
                _fps = 1f / Time.deltaTime;
                // 平滑处理，避免抖动太大
                _smoothFps = Mathf.Lerp(_smoothFps, _fps, 0.1f);
            }
        }
    }
}
