using System;
using System.Collections.Generic;

namespace Manager
{
    /// <summary>
    /// 全局模态 UI 栈。
    /// 窗口在 OnShow 时调用 Push，在 OnClose 时调用 Pop。
    /// ESC/全局关闭键调用 CloseTop 弹出栈顶窗口。
    /// </summary>
    public static class UIModalStack
    {
        private static readonly Stack<Action> _stack = new();

        /// <summary>将一个关闭回调压栈（窗口打开时调用）。</summary>
        public static void Push(Action closeAction)
        {
            _stack.Push(closeAction);
        }

        /// <summary>弹出并执行栈顶的关闭回调（ESC 时调用）。</summary>
        public static void CloseTop()
        {
            if (_stack.Count > 0)
                _stack.Pop()?.Invoke();
        }

        /// <summary>手动将指定回调从栈中移除（窗口被非 ESC 途径关闭时调用）。</summary>
        public static void Remove(Action closeAction)
        {
            // Stack 不直接支持 Remove，重建一次
            var temp = new Stack<Action>(_stack);
            _stack.Clear();
            foreach (var action in temp)
            {
                if (action != closeAction)
                    _stack.Push(action);
            }
        }

        public static bool HasAny => _stack.Count > 0;
    }
}
