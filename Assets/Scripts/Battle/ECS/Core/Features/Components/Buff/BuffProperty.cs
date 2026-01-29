using FixMath;

namespace Battle.ECS.Component
{
    /// <summary>
    /// Buff属性组件
    /// </summary>
    public struct BuffProperty
    {
        public FP Duration; // 持续时间
        public FP DurationPct; // 持续时间百分比
        public int MaxStack; // 最大层数
        public BattleBuffStackMode StackMode; // 叠加模式
        public BattleBuffOverflowPolicy OverflowPolicy; // 溢出策略
        // public AttrInfo[] Attrs; //加成属性值
        public int SpeedPctModifier; // 速度修正值
    }
    
    public enum BattleBuffStackMode
    {
        /// <summary>
        /// 叠加时刷新持续时间，时间结束后全部移除，溢出时替换
        /// </summary>
        RefreshDuration = 0,
        /// <summary>
        /// 每层同时计时，单独过期，溢出时替换
        /// </summary>
        IndependentDuration = 1,
        /// <summary>
        /// 逐层过期，溢出时替换
        /// </summary>
        SequentialDuration = 2,
        /// <summary>
        /// 永久有效，保留所有堆叠信息
        /// </summary>
        Permanent = 3,
    }
    
    public enum BattleBuffOverflowPolicy
    {
        /// <summary>
        /// 替换最早添加的那一层
        /// </summary>
        ReplaceOldest = 0,
        /// <summary>
        /// 替换优先级最低的那一层
        /// </summary>
        ReplaceLowestPriority = 1,
        /// <summary>
        /// 达到上限时丢弃新添加的那一层
        /// </summary>
        DiscardNewest = 2,
    }
}