using System;
using System.Runtime.CompilerServices;

namespace Battle.ECS.Core.Helper
{
    public static class EnumExtensions
    {
        public static string GetFlagsString<T>(this T flags) where T : Enum
        {
            if (Convert.ToInt32(flags) == 0)
                return "None";
            string result = string.Empty;
            foreach (T flag in Enum.GetValues(typeof(T)))
            {
                if (Convert.ToInt32(flags) != 0 && flags.HasFlag(flag))
                {
                    if (string.IsNullOrEmpty(result) == false)
                        result += " | ";
                    result += flag;
                }
            }
            return result;
        }

        /// <summary>
        /// 替换 HasFlag 方法，避免装箱，仅适用于Int32枚举
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool HasFlagFast<T>(this T flags, T value)
            where T : struct, Enum
        {
            var f = Unsafe.As<T, int>(ref flags);
            var v = Unsafe.As<T, int>(ref value);
            return (f & v) == v;
        }

        // 判断两个Flag 有交集（仅适用于Int32枚举 ）
        public static bool HasIntersection<T>(this T flags, T value)
            where T : struct, Enum
        {
            int f = Unsafe.As<T, int>(ref flags);
            int v = Unsafe.As<T, int>(ref value);
            return (f & v) != 0;
        }
    }
}