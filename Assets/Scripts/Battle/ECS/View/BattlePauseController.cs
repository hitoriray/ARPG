using System;
using Battle.ECS.Core.Helper;

namespace Battle.ECS.View
{
    /// <summary>
    /// 战斗暂停控制器（客户端专用）
    /// 管理战斗的暂停和恢复逻辑
    /// </summary>
    public sealed class BattlePauseController
    {
        private BattlePauseFlag _pauseFlag;

        /// <summary>
        /// 是否处于暂停状态
        /// </summary>
        public bool IsPaused()
        {
            return _pauseFlag != 0;
        }

        /// <summary>
        /// 是否为"软暂停"（暂停战斗逻辑，但允许部分视觉系统继续更新）
        /// </summary>
        public bool IsSoftPauseOnly()
        {
            const BattlePauseFlag softFlags = BattlePauseFlag.Guide;
            return (_pauseFlag & softFlags) != 0 && (_pauseFlag & ~softFlags) == 0;
        }

        /// <summary>
        /// 暂停战斗
        /// </summary>
        /// <param name="pauseTag">暂停标志</param>
        public void Pause(BattlePauseFlag pauseTag)
        {
            if (_pauseFlag.HasFlagFast(pauseTag)) return;
            _pauseFlag |= pauseTag;
        }

        /// <summary>
        /// 恢复战斗
        /// </summary>
        /// <param name="pauseTag">暂停标志</param>
        public void Resume(BattlePauseFlag pauseTag)
        {
            if (_pauseFlag.HasFlagFast(pauseTag) == false) return;
            _pauseFlag &= ~pauseTag;
        }

        /// <summary>
        /// 重置暂停状态
        /// </summary>
        public void Reset()
        {
            _pauseFlag = 0;
        }
    }

    /// <summary>
    /// 战斗暂停标志（位标志枚举）
    /// </summary>
    [Flags]
    public enum BattlePauseFlag
    {
        User = 1 << 0, // 玩家暂停
        Guide = 1 << 1, // 引导暂停（暂停战斗逻辑但允许视觉更新）
    }
}