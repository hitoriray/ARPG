using FixMath;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 速度百分比修改器
    /// </summary>
    public readonly struct SpeedPctModifier
    {
        public readonly FP Value; // 百分比值，如 0.3 表示增加30%速度
        public SpeedPctModifier(FP value)
        {
            Value = value;
        }
    }
}