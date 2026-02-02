using FixMath;

namespace Battle.ECS.Component
{
    /// <summary>
    /// Tick组件（用于周期性效果，如DoT）
    /// </summary>
    public struct Tick
    {
        public FP Interval;      // Tick间隔
        public FP Elapsed;       // 已经过时间
        public int Count;        // 剩余次数（<0表示无限）
        public FP IntervalPct;   // Tick间隔百分比修正

        public Tick(FP interval, int count)
        {
            Interval = interval;
            Elapsed = FP.Zero;
            Count = count;
            IntervalPct = FP.One;
        }

        /// <summary>
        /// 实际Tick间隔（考虑百分比修正）
        /// </summary>
        public FP ActualInterval => Interval / IntervalPct;
    }
}