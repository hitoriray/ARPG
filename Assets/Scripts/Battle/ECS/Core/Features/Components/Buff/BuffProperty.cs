using Config;
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
    
}