using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

/// <summary>
/// 增强的调试工具类
/// 自动获取调用者的类名和方法名，支持多种日志级别和丰富的功能
/// </summary>
public class RayDebug
{
    #region 配置
    /// <summary>
    /// 是否启用调试输出（在正式发布时可以关闭）
    /// </summary>
    public static bool Enabled = true;
    
    /// <summary>
    /// 最小日志级别（低于此级别的日志不会输出）
    /// </summary>
    public static LogLevel MinLevel = LogLevel.Trace;
    
    /// <summary>
    /// 是否显示时间戳
    /// </summary>
    public static bool ShowTimestamp = false;
    
    /// <summary>
    /// 是否显示帧数
    /// </summary>
    public static bool ShowFrameCount = false;
    #endregion
    
    #region LogLevel

    public enum LogLevel
    {
        Trace = 0,      // 最详细的跟踪信息
        Debug = 1,      // 调试信息
        Info = 2,       // 普通信息
        Warning = 3,    // 警告
        Error = 4,      // 错误
        Off = 999,      // 关闭所有日志
    }
    #endregion
    
    #region 核心日志方法
    /// <summary>
    /// 输出 Trace 级别日志（灰色）
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Trace(
        string message,
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!ShouldLog(LogLevel.Trace)) return;
        string formatted = FormatMessage(message, filePath, memberName, lineNumber, "TRACE", "gray");
        Debug.Log(formatted, context);
    }
    /// <summary>
    /// 输出 Debug 级别日志（白色）
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(
        string message,
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!ShouldLog(LogLevel.Debug)) return;
        string formatted = FormatMessage(message, filePath, memberName, lineNumber);
        Debug.Log(formatted, context);
    }
    /// <summary>
    /// 输出 Info 级别日志（青色）
    /// </summary>
    public static void Info(
        string message,
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!ShouldLog(LogLevel.Info)) return;
        string formatted = FormatMessage(message, filePath, memberName, lineNumber, "INFO", "cyan");
        Debug.Log(formatted, context);
    }
    /// <summary>
    /// 输出 Warning 级别日志（黄色）
    /// </summary>
    public static void Warn(
        string message,
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!ShouldLog(LogLevel.Warning)) return;
        string formatted = FormatMessage(message, filePath, memberName, lineNumber, "WARN", "yellow");
        Debug.LogWarning(formatted, context);
    }
    /// <summary>
    /// 输出 Error 级别日志（红色）
    /// </summary>
    public static void Error(
        string message,
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!ShouldLog(LogLevel.Error)) return;
        string formatted = FormatMessage(message, filePath, memberName, lineNumber, "ERROR", "red");
        Debug.LogError(formatted, context);
    }
    /// <summary>
    /// 输出异常信息
    /// </summary>
    public static void Exception(
        Exception exception,
        string additionalMessage = null,
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string className = GetClassName(filePath);
        string prefix = $"<color=red>[EXCEPTION]</color> [{className}.{memberName}:{lineNumber}]";
        if (!string.IsNullOrEmpty(additionalMessage))
        {
            Debug.LogError($"{prefix} {additionalMessage}", context);
        }
        Debug.LogException(exception, context);
    }
    #endregion
    #region 条件日志
    /// <summary>
    /// 只有当条件为 true 时才输出日志
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogIf(
        bool condition,
        string message,
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (condition)
        {
            Log(message, context, filePath, memberName, lineNumber);
        }
    }
    /// <summary>
    /// 只有当条件为 true 时才输出警告
    /// </summary>
    public static void WarnIf(
        bool condition,
        string message,
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (condition)
        {
            Warn(message, context, filePath, memberName, lineNumber);
        }
    }
    /// <summary>
    /// 只有当条件为 true 时才输出错误
    /// </summary>
    public static void ErrorIf(
        bool condition,
        string message,
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (condition)
        {
            Error(message, context, filePath, memberName, lineNumber);
        }
    }
    #endregion
    #region 断言
    /// <summary>
    /// 断言条件为 true，否则输出错误日志
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Assert(
        bool condition,
        string message = "Assertion failed!",
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!condition)
        {
            Error($"[ASSERT] {message}", context, filePath, memberName, lineNumber);
        }
    }
    /// <summary>
    /// 断言对象不为 null
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void AssertNotNull(
        object obj,
        string objectName = "Object",
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (obj == null || (obj is Object unityObj && unityObj == null))
        {
            Error($"[ASSERT] {objectName} is null!", context, filePath, memberName, lineNumber);
        }
    }
    #endregion
    #region 分组日志（用于复杂流程）
    /// <summary>
    /// 开始一个日志分组
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void BeginGroup(
        string groupName,
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!ShouldLog(LogLevel.Debug)) return;
        string className = GetClassName(filePath);
        Debug.Log($"<color=magenta>▼▼▼ [{className}.{memberName}] {groupName} ▼▼▼</color>", context);
    }
    /// <summary>
    /// 结束一个日志分组
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void EndGroup(
        string groupName = null,
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!ShouldLog(LogLevel.Debug)) return;
        string className = GetClassName(filePath);
        string suffix = string.IsNullOrEmpty(groupName) ? "" : $" {groupName}";
        Debug.Log($"<color=magenta>▲▲▲ [{className}.{memberName}]{suffix} ▲▲▲</color>", context);
    }
    #endregion
    #region 性能计时
    private static readonly System.Collections.Generic.Dictionary<string, Stopwatch> _timers = new();
    /// <summary>
    /// 开始计时
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void StartTimer(
        string timerName,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "")
    {
        string key = $"{GetClassName(filePath)}.{memberName}.{timerName}";
        if (!_timers.ContainsKey(key))
        {
            _timers[key] = new Stopwatch();
        }
        _timers[key].Restart();
    }
    /// <summary>
    /// 停止计时并输出耗时
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void StopTimer(
        string timerName,
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string className = GetClassName(filePath);
        string key = $"{className}.{memberName}.{timerName}";
        if (_timers.TryGetValue(key, out var sw))
        {
            sw.Stop();
            string formatted = FormatMessage($"⏱ {timerName}: {sw.ElapsedMilliseconds}ms ({sw.ElapsedTicks} ticks)", 
                filePath, memberName, lineNumber, "TIMER", "orange");
            Debug.Log(formatted, context);
        }
    }
    #endregion
    #region 变量追踪
    /// <summary>
    /// 追踪变量值变化（输出变量名和值）
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Watch<T>(
        string name,
        T value,
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!ShouldLog(LogLevel.Debug)) return;
        string formatted = FormatMessage($"👁 {name} = {value}", filePath, memberName, lineNumber, "WATCH", "#88ff88");
        Debug.Log(formatted, context);
    }
    /// <summary>
    /// 追踪多个变量
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void WatchAll(
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0,
        params (string name, object value)[] variables)
    {
        if (!ShouldLog(LogLevel.Debug)) return;
        string className = GetClassName(filePath);
        var sb = new System.Text.StringBuilder();
        sb.Append($"<color=#88ff88>[WATCH]</color> [{className}.{memberName}:{lineNumber}] ");
        for (int i = 0; i < variables.Length; i++)
        {
            if (i > 0) sb.Append(" | ");
            sb.Append($"👁 {variables[i].name} = {variables[i].value}");
        }
        Debug.Log(sb.ToString(), context);
    }
    #endregion
    #region 工具方法
    private static bool ShouldLog(LogLevel level)
    {
        return Enabled && level >= MinLevel;
    }
    private static string GetClassName(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return "Unknown";
        // 从文件路径中提取类名（假设文件名与类名相同）
        int lastSlash = filePath.LastIndexOfAny(new[] { '/', '\\' });
        int lastDot = filePath.LastIndexOf('.');
        if (lastSlash >= 0 && lastDot > lastSlash)
        {
            return filePath.Substring(lastSlash + 1, lastDot - lastSlash - 1);
        }
        return filePath;
    }
    private static string FormatMessage(
        string message,
        string filePath,
        string memberName,
        int lineNumber,
        string level = null,
        string color = null)
    {
        string className = GetClassName(filePath);
        var sb = new System.Text.StringBuilder();
        // 日志级别标签及颜色开始
        if (!string.IsNullOrEmpty(level) && !string.IsNullOrEmpty(color))
        {
            sb.Append($"<color={color}>[{level}] ");
        }
        else if (!string.IsNullOrEmpty(level))
        {
            sb.Append($"[{level}] ");
        }

        // 时间戳
        if (ShowTimestamp)
        {
            sb.Append($"[{DateTime.Now:HH:mm:ss.fff}] ");
        }
        // 帧数
        if (ShowFrameCount)
        {
            sb.Append($"[F:{Time.frameCount}] ");
        }
        // 类名.方法名:行号
        sb.Append($"[{className}.{memberName}:{lineNumber}] ");

        // 消息内容
        sb.Append(message);

        // 如果有颜色，需要在最末尾闭合标签
        if (!string.IsNullOrEmpty(level) && !string.IsNullOrEmpty(color))
        {
            sb.Append("</color>");
        }

        return sb.ToString();
    }
    #endregion
    #region 扩展 - 快捷方法
    /// <summary>
    /// 输出方法进入日志
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Enter(
        string additionalInfo = null,
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string msg = string.IsNullOrEmpty(additionalInfo) ? ">>> Enter" : $">>> Enter ({additionalInfo})";
        Trace(msg, context, filePath, memberName, lineNumber);
    }
    /// <summary>
    /// 输出方法退出日志
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Exit(
        string additionalInfo = null,
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string msg = string.IsNullOrEmpty(additionalInfo) ? "<<< Exit" : $"<<< Exit ({additionalInfo})";
        Trace(msg, context, filePath, memberName, lineNumber);
    }
    /// <summary>
    /// 输出分隔线
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Separator(string title = null)
    {
        if (!ShouldLog(LogLevel.Debug)) return;
        if (string.IsNullOrEmpty(title))
        {
            Debug.Log("<color=gray>════════════════════════════════════════</color>");
        }
        else
        {
            Debug.Log($"<color=gray>══════════ {title} ══════════</color>");
        }
    }
    /// <summary>
    /// 输出当前堆栈信息
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void StackTrace(
        Object context = null,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string className = GetClassName(filePath);
        var stackTrace = new StackTrace(1, true); // 跳过当前方法
        Debug.Log($"<color=orange>[STACK]</color> [{className}.{memberName}:{lineNumber}]\n{stackTrace}", context);
    }
    #endregion
}