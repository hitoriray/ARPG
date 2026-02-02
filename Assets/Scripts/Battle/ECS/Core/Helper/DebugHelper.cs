using System.Diagnostics;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Extend.System;
using Battle.ECS.Component;
using Debug = UnityEngine.Debug;

namespace Battle.ECS.Core.Helper
{
    public static class DebugHelper
    {
        /// <summary>
        /// 给实体添加调试信息
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="message"></param>
        [Conditional("BATTLE_DEBUG")]
        public static void AddDebugInfo(this in Entity entity, string message)
        {
            if (entity.IsAlive() == false) return;
            entity.Replace(new DebugInfo($"[{entity.WorldId}:{entity.Id}:{entity.Version}-{message}]"));
        }

        /// <summary>
        /// 获取实体的调试信息
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static string GetDebugInfo(this in Entity entity)
        {
#if BATTLE_DEBUG
            if (entity.IsAlive() == false) return $"[{entity.WorldId}:{entity.Id}:{entity.Version}-Dead]";
            ref var debugInfo = ref entity.TryGetRef<DebugInfo>(out var hasDebugInfo);
            if (hasDebugInfo == false) return $"[{entity.WorldId}:{entity.Id}:{entity.Version}]";
            return debugInfo.Info;
#endif
#if UNITY_EDITOR
            return $"[{entity.Id}:{entity.Version}]";
#endif
            return null;
        }

        /// <summary>
        /// 输出调试信息到控制台
        /// </summary>
        /// <param name="message"></param>
        [Conditional("BATTLE_DEBUG_LOG")]
        public static void Log(string message)
        {
            RayDebug.Log($"[BattleDebug] {message}");
        }
    }
}