using System;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.LowLevel;
using Battle.ECS;
using Battle.ECS.Component;
using Battle.ECS.Core.Helper;
using Config;
using UnityEditor;
using UnityEngine;

namespace Editor.BattleDebug.Pages
{
    /// <summary>
    /// Buff调试页 - 显示实体的Buff列表和详细信息
    /// </summary>
    public class BuffPage : IBattleDebugPage
    {
        public string Title => "Buff";
        public string Icon => "[B]";

        private string _searchText = "";
        private bool _showPlayerBuffs = true;
        private bool _showMonsterBuffs = true;
        private Vector2 _scrollPosition;
        private Entity _selectedEntity;

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

            if (_showPlayerBuffs)
            {
                DrawSection("玩家阵营Buff", world, true);
            }

            if (_showMonsterBuffs)
            {
                DrawSection("怪物阵营Buff", world, false);
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
            _showPlayerBuffs = EditorGUILayout.ToggleLeft("显示玩家Buff", _showPlayerBuffs, GUILayout.Width(120));
            _showMonsterBuffs = EditorGUILayout.ToggleLeft("显示怪物Buff", _showMonsterBuffs, GUILayout.Width(120));
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
                
                // 检查是否有BuffList组件
                ref var buffList = ref entity.TryGetRef<BuffList>(out var hasBuffList);
                if (!hasBuffList) continue;
                
                // TODO: 阵营过滤 - 需要根据实际的阵营组件来实现
                // 目前跳过阵营判断，显示所有有BuffList的实体
                
                // 搜索过滤
                if (!MatchesSearch(entity, ref buffList)) continue;

                DrawEntity(entity, ref buffList);
                entityCount++;
            }

            if (entityCount == 0)
            {
                EditorGUILayout.LabelField("无匹配的实体", BattleDebugStyles.DisabledStyle);
            }
        }

        private bool MatchesSearch(in Entity entity, ref BuffList buffList)
        {
            if (string.IsNullOrEmpty(_searchText))
                return true;

            // 检查实体调试信息
            var debugInfo = entity.GetDebugInfo();
            if (!string.IsNullOrEmpty(debugInfo) && debugInfo.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                return true;

            // 检查Buff ID
            foreach (var buffEntity in buffList.Value)
            {
                if (!buffEntity.IsAlive()) continue;
                ref var buff = ref buffEntity.Get<Buff>();
                if (buff.ID.ToString().Contains(_searchText))
                    return true;
            }

            return false;
        }

        private void DrawEntity(Entity entity, ref BuffList buffList)
        {
            EditorGUILayout.BeginVertical("box");

            // 实体标题
            EditorGUILayout.LabelField($"挂载者: {entity.GetDebugInfo()}", BattleDebugStyles.HeaderStyle);

            GUILayout.Space(BattleDebugStyles.SmallSpace);

            if (buffList.Value.Count == 0)
            {
                EditorGUILayout.LabelField("无Buff", BattleDebugStyles.DisabledStyle);
            }
            else
            {
                // Buff事件统计
                DrawBuffStatistics(ref buffList);

                GUILayout.Space(BattleDebugStyles.SmallSpace);

                // Buff列表
                EditorGUILayout.LabelField($"Buff列表 ({buffList.Value.Count}):", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                for (int i = 0; i < buffList.Value.Count; i++)
                {
                    var buffEntity = buffList.Value[i];
                    if (buffEntity.IsAlive())
                    {
                        DrawBuff(buffEntity);
                        GUILayout.Space(BattleDebugStyles.SmallSpace);
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(BattleDebugStyles.SmallSpace);
        }

        private void DrawBuffStatistics(ref BuffList buffList)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Hurt:{buffList.HurtEvent}", GUILayout.Width(70));
            EditorGUILayout.LabelField($"DealDmg:{buffList.DealDamageEvent}", GUILayout.Width(85));
            EditorGUILayout.LabelField($"HurtMod:{buffList.HurtModifierEvent}", GUILayout.Width(85));
            EditorGUILayout.LabelField($"DmgMod:{buffList.DealDamageModifierEvent}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"Cast:{buffList.OnCastEvent}", GUILayout.Width(60));
            EditorGUILayout.LabelField($"Healed:{buffList.HealedEvent}", GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBuff(Entity buffEntity)
        {
            ref var buff = ref buffEntity.Get<Buff>();
            
            // 尝试获取BuffProperty和BuffStack
            ref var buffProperty = ref buffEntity.TryGetRef<BuffProperty>(out var hasProperty);
            ref var buffStack = ref buffEntity.TryGetRef<BuffStack>(out var hasStack);

            EditorGUILayout.BeginVertical("box");

            // Buff标题行
            EditorGUILayout.BeginHorizontal();
            
            string buffName = buff.Config != null ? $"[{buff.ID}] {buff.Config.buffName}" : $"[{buff.ID}]";
            EditorGUILayout.LabelField($"• {buffName}", BattleDebugStyles.SubHeaderStyle);

            // 层数显示
            if (hasStack)
            {
                var stackCount = buffStack.Value.Count;
                var maxStack = hasProperty ? buffProperty.MaxStack : 1;
                var stackText = $"{stackCount}/{maxStack}";
                var stackStyle = stackCount > 1 ? BattleDebugStyles.WarningStyle : BattleDebugStyles.SuccessStyle;
                EditorGUILayout.LabelField($"层数: {stackText}", stackStyle, GUILayout.Width(100));
            }

            EditorGUILayout.EndHorizontal();

            // Buff基础信息
            EditorGUILayout.BeginVertical("helpbox");

            // 施法者信息
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("施法者:", GUILayout.Width(80));
            var casterStyle = buff.Caster.IsAlive() ? EditorStyles.label : BattleDebugStyles.DisabledStyle;
            var casterText = buff.Caster.IsAlive() ? buff.Caster.GetDebugInfo() : "已死亡";
            EditorGUILayout.LabelField(casterText, casterStyle);
            EditorGUILayout.EndHorizontal();

            // 目标信息
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("目标:", GUILayout.Width(80));
            var targetText = buff.Target.IsAlive() ? buff.Target.GetDebugInfo() : "无";
            EditorGUILayout.LabelField(targetText);
            EditorGUILayout.EndHorizontal();

            // 持续时间信息
            if (hasProperty)
            {
                DrawDurationInfo(ref buffProperty);
            }

            EditorGUILayout.EndVertical();

            // 详细堆叠信息
            if (hasStack && hasProperty && buffStack.Value.Count > 0)
            {
                GUILayout.Space(BattleDebugStyles.SmallSpace);
                DrawStackDetails(ref buffStack, ref buffProperty);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDurationInfo(ref BuffProperty buffProperty)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("持续时长:", GUILayout.Width(80));

            if (buffProperty.StackMode == BattleBuffStackMode.Permanent)
            {
                EditorGUILayout.LabelField("永久", BattleDebugStyles.SuccessStyle);
            }
            else
            {
                var duration = (float)buffProperty.Duration;
                var durationPct = (float)buffProperty.DurationPct;
                var actualDuration = duration * durationPct;
                EditorGUILayout.LabelField($"{duration:F2}s × {durationPct:P0} = {actualDuration:F2}s");
            }

            EditorGUILayout.EndHorizontal();

            // 堆叠模式
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("堆叠模式:", GUILayout.Width(80));
            EditorGUILayout.LabelField(buffProperty.StackMode.ToString());
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStackDetails(ref BuffStack buffStack, ref BuffProperty buffProperty)
        {
            EditorGUILayout.LabelField("堆叠详情:");
            EditorGUI.indentLevel++;

            for (int i = 0; i < buffStack.Value.Count; i++)
            {
                var stackInfo = buffStack.Value[i];

                EditorGUILayout.BeginHorizontal();

                // 堆叠序号
                EditorGUILayout.LabelField($"#{i + 1}", GUILayout.Width(40));

                // 施法者
                var casterStyle = stackInfo.Caster.IsAlive() ? EditorStyles.miniLabel : BattleDebugStyles.DisabledStyle;
                var casterText = stackInfo.Caster.IsAlive() ? stackInfo.Caster.GetDebugInfo() : "已死亡";
                EditorGUILayout.LabelField($"施法者:{casterText}", casterStyle, GUILayout.Width(200));

                // 剩余时间
                var remainingTime = (float)stackInfo.RemainingTime;
                EditorGUILayout.LabelField($"剩余: {remainingTime:F2}s", BattleDebugStyles.WarningStyle, GUILayout.Width(100));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }
    }
}
