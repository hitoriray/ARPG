using System.Text;
using Arch.Core;
using Arch.Core.Extensions;
using Battle.ECS.Component;
using Config;
using FixMath;
using UnityEngine;

namespace Battle.ECS.Examples
{
    /// <summary>
    /// Buff调试辅助工具
    /// </summary>
    public static class BuffDebugHelper
    {
        /// <summary>
        /// 打印实体的所有Buff详细信息
        /// </summary>
        public static void PrintBuffList(Entity targetEntity, string tag = "")
        {
            if (!targetEntity.IsAlive())
            {
                RayDebug.Error($"{tag} 实体无效");
                return;
            }

            if (!targetEntity.Has<BuffList>())
            {
                RayDebug.Log($"{tag} 实体没有任何Buff");
                return;
            }

            ref var buffList = ref targetEntity.Get<BuffList>();
            var sb = new StringBuilder();
            sb.AppendLine($"========== {tag} Buff列表 (共{buffList.Value.Count}个) ==========");

            for (int i = 0; i < buffList.Value.Count; i++)
            {
                var buffEntity = buffList.Value[i];
                if (!buffEntity.IsAlive())
                {
                    sb.AppendLine($"[{i}] Buff实体无效");
                    continue;
                }

                ref var buff = ref buffEntity.Get<Buff>();
                ref var buffStack = ref buffEntity.Get<BuffStack>();
                ref var buffProperty = ref buffEntity.Get<BuffProperty>();

                sb.AppendLine($"━━━━━━ [{i}] {buff.Config.buffName} (ID:{buff.Config.buffId}) ━━━━━━");
                sb.AppendLine($"  叠加模式: {buffProperty.StackMode}");
                sb.AppendLine($"  溢出策略: {buffProperty.OverflowPolicy}");
                sb.AppendLine($"  最大层数: {buffProperty.MaxStack}");
                sb.AppendLine($"  持续时间: {(float)buffProperty.Duration}秒");
                sb.AppendLine($"  当前层数: {buffStack.Value.Count}/{buffProperty.MaxStack}");

                // 打印每层的详细信息
                if (buffStack.Value.Count > 0)
                {
                    sb.AppendLine($"  ┌── 堆叠详情 ──┐");
                    for (int j = 0; j < buffStack.Value.Count; j++)
                    {
                        var stackInfo = buffStack.Value[j];
                        var casterInfo = stackInfo.Caster.IsAlive() ? $"Entity_{stackInfo.Caster.Id}" : "已销毁";
                        sb.AppendLine($"  │ 第{j + 1}层: 施法者={casterInfo}, 剩余时间={(float)stackInfo.RemainingTime:F2}秒");
                    }
                    sb.AppendLine($"  └────────────┘");
                }

                // 打印属性修正
                if (buff.Config.AttrModifiers != null && buff.Config.AttrModifiers.Length > 0)
                {
                    sb.AppendLine($"  属性修正:");
                    foreach (var modifier in buff.Config.AttrModifiers)
                    {
                        var modeStr = modifier.mode == AttrModifyMode.Percent ? "%" : "点";
                        sb.AppendLine($"    - {modifier.type}: {(modifier.value > 0 ? "+" : "")}{modifier.value}{modeStr}");
                    }
                }

                // 打印Tick信息
                if (buffEntity.Has<Tick>())
                {
                    ref var tick = ref buffEntity.Get<Tick>();
                    sb.AppendLine($"  Tick: 间隔={(float)tick.Interval}秒, 已过={(float)tick.Elapsed:F2}秒, 剩余次数={tick.Count}");
                }

                sb.AppendLine();
            }

            sb.AppendLine($"======================================");
            RayDebug.Log(sb.ToString());
        }

        /// <summary>
        /// 打印实体属性
        /// </summary>
        public static void PrintAttributes(Entity targetEntity, string tag = "")
        {
            if (!targetEntity.IsAlive())
            {
                RayDebug.Error($"{tag} 实体无效");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"========== {tag} 属性信息 ==========");

            if (targetEntity.Has<Battle.ECS.Component.Attribute>())
            {
                ref var attr = ref targetEntity.Get<Battle.ECS.Component.Attribute>();
                sb.AppendLine($"  攻击力: {(float)attr.Attack}");
                sb.AppendLine($"  防御力: {(float)attr.Defense}");
                sb.AppendLine($"  速度: {(float)attr.Speed}");
                sb.AppendLine($"  最大生命: {(float)attr.MaxHp}");
                sb.AppendLine($"  最大法力: {(float)attr.MaxMp}");
                sb.AppendLine($"  暴击率: {(float)attr.CritRate * 100:F1}%");
                sb.AppendLine($"  暴击伤害: {(float)attr.CritDamage * 100:F0}%");
            }
            else
            {
                sb.AppendLine("  无Attribute组件");
            }

            if (targetEntity.Has<Health>())
            {
                ref var hp = ref targetEntity.Get<Health>();
                sb.AppendLine($"  当前生命: {(float)hp.Current}/{(float)hp.Max} ({hp.Ratio:P0})");
            }
            else
            {
                sb.AppendLine("  无Health组件");
            }

            sb.AppendLine($"======================================");
            RayDebug.Log(sb.ToString());
        }

        /// <summary>
        /// 验证BuffConfig配置
        /// </summary>
        public static void ValidateBuffConfig(BuffConfig config)
        {
            if (config == null)
            {
                RayDebug.Error("BuffConfig为空");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"========== BuffConfig验证: {config.buffName} ==========");
            sb.AppendLine($"  ID: {config.buffId}");
            sb.AppendLine($"  描述: {config.description}");
            sb.AppendLine($"  叠加模式: {config.stackMode}");
            sb.AppendLine($"  溢出策略: {config.overflowPolicy}");
            sb.AppendLine($"  最大层数: {config.maxStack}");
            sb.AppendLine($"  持续时间: {config.duration}秒");
            sb.AppendLine($"  Tick间隔: {config.tickInterval}秒");
            sb.AppendLine($"  Tick次数: {config.tickCount}");

            // 警告检查
            if (config.stackMode == BattleBuffStackMode.RefreshDuration)
            {
                sb.AppendLine($"  ⚠️ RefreshDuration模式：叠加时会刷新所有层的时间，到期后全部移除");
            }
            else if (config.stackMode == BattleBuffStackMode.IndependentDuration)
            {
                sb.AppendLine($"  ✅ IndependentDuration模式：每层独立计时，分别过期");
            }

            if (config.maxStack > 1 && config.stackMode == BattleBuffStackMode.RefreshDuration)
            {
                sb.AppendLine($"  💡 提示：RefreshDuration模式下，所有层会同时销毁（这是正常的）");
            }

            sb.AppendLine($"======================================");
            RayDebug.Log(sb.ToString());
        }

        /// <summary>
        /// 监控Buff变化（需要每帧调用）
        /// </summary>
        public static void MonitorBuffChanges(Entity targetEntity, ref int lastBuffCount, ref string lastBuffSnapshot)
        {
            if (!targetEntity.IsAlive() || !targetEntity.Has<BuffList>())
            {
                lastBuffCount = 0;
                lastBuffSnapshot = "";
                return;
            }

            ref var buffList = ref targetEntity.Get<BuffList>();
            int currentCount = buffList.Value.Count;

            // 生成当前快照
            var sb = new StringBuilder();
            for (int i = 0; i < buffList.Value.Count; i++)
            {
                var buffEntity = buffList.Value[i];
                if (!buffEntity.IsAlive()) continue;

                ref var buff = ref buffEntity.Get<Buff>();
                ref var buffStack = ref buffEntity.Get<BuffStack>();
                sb.Append($"{buff.Config.buffName}x{buffStack.Value.Count}");

                if (buffStack.Value.Count > 0)
                {
                    sb.Append($"[{string.Join(",", GetRemainingTimes(ref buffStack))}]");
                }

                if (i < buffList.Value.Count - 1)
                    sb.Append(" | ");
            }

            string currentSnapshot = sb.ToString();

            // 检测变化
            if (currentCount != lastBuffCount || currentSnapshot != lastBuffSnapshot)
            {
                RayDebug.Log($"[BuffMonitor] Buff变化: {lastBuffSnapshot} → {currentSnapshot}");
                lastBuffCount = currentCount;
                lastBuffSnapshot = currentSnapshot;
            }
        }

        private static string[] GetRemainingTimes(ref BuffStack buffStack)
        {
            var times = new string[buffStack.Value.Count];
            for (int i = 0; i < buffStack.Value.Count; i++)
            {
                times[i] = ((float)buffStack.Value[i].RemainingTime).ToString("F1");
            }
            return times;
        }
    }
}
