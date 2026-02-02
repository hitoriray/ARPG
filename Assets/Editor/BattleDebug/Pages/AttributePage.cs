using System;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.LowLevel;
using Battle.ECS;
using Battle.ECS.Component;
using Battle.ECS.Core.Helper;
using FixMath;
using UnityEditor;
using UnityEngine;

namespace Editor.BattleDebug.Pages
{
    using Attribute = Battle.ECS.Component.Attribute;
    /// <summary>
    /// 属性调试页 - 显示实体的属性信息
    /// </summary>
    public class AttributePage : IBattleDebugPage
    {
        public string Title => "属性";
        public string Icon => "[A]";

        private string _searchText = "";
        private bool _showPlayerFaction = true;
        private bool _showMonsterFaction = true;
        private Vector2 _scrollPosition;

        public void OnGUI()
        {
            DrawToolbar();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("仅在播放模式下可用", MessageType.Info);
                return;
            }

            var runner = BattleEcsRunner.Instance;
            if (runner?.Context?.World == null)
            {
                EditorGUILayout.HelpBox("战斗上下文不可用", MessageType.Warning);
                return;
            }

            var world = runner.Context.World;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_showPlayerFaction)
            {
                DrawSection("玩家阵营", world, true);
            }

            if (_showMonsterFaction)
            {
                DrawSection("怪物阵营", world, false);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginVertical("box");

            // 搜索栏
            BattleDebugStyles.DrawSearchField(ref _searchText);

            GUILayout.Space(BattleDebugStyles.SmallSpace);

            // 显示选项
            EditorGUILayout.BeginHorizontal();
            _showPlayerFaction = EditorGUILayout.ToggleLeft("显示玩家阵营", _showPlayerFaction, GUILayout.Width(120));
            _showMonsterFaction = EditorGUILayout.ToggleLeft("显示怪物阵营", _showMonsterFaction, GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawSection(string title, World world, bool isPlayerSection)
        {
            GUILayout.Space(BattleDebugStyles.MediumSpace);
            EditorGUILayout.LabelField(title, BattleDebugStyles.HeaderStyle);
            BattleDebugStyles.DrawSeparator();

            int entityCount = 0;
            var count = world.Size;
            if (count == 0)
            {
                EditorGUILayout.LabelField("无实体", BattleDebugStyles.DisabledStyle);
                return;
            }

            using var entities = new UnsafeArray<Entity>(count);
            world.GetEntities(new QueryDescription(), entities.AsSpan());

            foreach (var entity in entities)
            {
                if (!entity.IsAlive()) continue;

                // 检查是否有Attribute组件
                ref var attribute = ref entity.TryGetRef<Attribute>(out var hasAttribute);
                if (!hasAttribute) continue;

                // 搜索过滤
                if (!MatchesSearch(entity)) continue;

                DrawEntity(entity, ref attribute);
                entityCount++;
            }

            if (entityCount == 0)
            {
                EditorGUILayout.LabelField("无匹配的实体", BattleDebugStyles.DisabledStyle);
            }
        }

        private bool MatchesSearch(in Entity entity)
        {
            if (string.IsNullOrEmpty(_searchText))
                return true;

            var debugInfo = entity.GetDebugInfo();
            return !string.IsNullOrEmpty(debugInfo) && 
                   debugInfo.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
        }

        private void DrawEntity(Entity entity, ref Attribute attribute)
        {
            EditorGUILayout.BeginVertical("box");

            // 实体标题
            EditorGUILayout.LabelField($"实体: {entity.GetDebugInfo()}", BattleDebugStyles.HeaderStyle);

            GUILayout.Space(BattleDebugStyles.SmallSpace);

            // 绘制属性
            EditorGUILayout.BeginVertical("helpbox");

            // 尝试获取Health组件
            ref var health = ref entity.TryGetRef<Health>(out var hasHealth);
            if (hasHealth)
            {
                DrawAttributeRow("生命值", $"{(float)health.Current:F0} / {(float)health.Max:F0}", 
                    GetHealthColor((float)health.Current, (float)health.Max));
            }

            // 基础属性
            DrawAttributeRow("攻击力", $"{(float)attribute.Attack:F1}", BattleDebugStyles.SuccessColor);
            DrawAttributeRow("防御力", $"{(float)attribute.Defense:F1}", BattleDebugStyles.SuccessColor);
            DrawAttributeRow("最大生命", $"{(float)attribute.MaxHp:F0}", Color.white);
            DrawAttributeRow("最大法力", $"{(float)attribute.MaxMp:F0}", new Color(0.3f, 0.6f, 1f));
            DrawAttributeRow("移动速度", $"{(float)attribute.Speed:F2}", Color.white);
            DrawAttributeRow("暴击率", $"{(float)attribute.CritRate * 100:F1}%", BattleDebugStyles.WarningColor);
            DrawAttributeRow("暴击伤害", $"{(float)attribute.CritDamage * 100:F0}%", BattleDebugStyles.WarningColor);

            EditorGUILayout.EndVertical();

            // 尝试获取Move组件
            ref var move = ref entity.TryGetRef<Move>(out var hasMove);
            if (hasMove)
            {
                GUILayout.Space(BattleDebugStyles.SmallSpace);
                DrawMoveInfo(ref move);
            }

            // 尝试获取Position组件
            ref var position = ref entity.TryGetRef<Position>(out var hasPosition);
            if (hasPosition)
            {
                GUILayout.Space(BattleDebugStyles.SmallSpace);
                DrawPositionInfo(ref position);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(BattleDebugStyles.SmallSpace);
        }

        private void DrawAttributeRow(string label, string value, Color valueColor)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(100));

            var style = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = valueColor }
            };
            EditorGUILayout.LabelField(value, style);

            EditorGUILayout.EndHorizontal();
        }

        private Color GetHealthColor(float current, float max)
        {
            if (max <= 0) return Color.white;
            float ratio = current / max;
            if (ratio > 0.5f) return BattleDebugStyles.SuccessColor;
            if (ratio > 0.25f) return BattleDebugStyles.WarningColor;
            return BattleDebugStyles.ErrorColor;
        }

        private void DrawMoveInfo(ref Move move)
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("移动信息", EditorStyles.boldLabel);

            DrawAttributeRow("基础速度", $"{(float)move.Speed:F2}", Color.white);
            DrawAttributeRow("实际速度", $"{(float)move.ActualSpeed:F2}", BattleDebugStyles.SuccessColor);
            
            var speedPct = (float)(FP.One + move.SpeedPct) * 100f;
            DrawAttributeRow("速度百分比", $"{speedPct:F1}%", 
                speedPct > 100 ? BattleDebugStyles.SuccessColor : 
                speedPct < 100 ? BattleDebugStyles.WarningColor : Color.white);

            var direction = move.Direction;
            DrawAttributeRow("移动方向", $"({(float)direction.x:F2}, {(float)direction.y:F2}, {(float)direction.z:F2})", Color.white);

            EditorGUILayout.EndVertical();
        }

        private void DrawPositionInfo(ref Position position)
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("位置信息", EditorStyles.boldLabel);

            var pos = position.Value;
            DrawAttributeRow("位置", $"({(float)pos.x:F2}, {(float)pos.y:F2}, {(float)pos.z:F2})", Color.white);

            EditorGUILayout.EndVertical();
        }
    }
}
