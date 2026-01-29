using FixMath;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 生命值组件
    /// </summary>
    public struct Health
    {
        public FP Current;
        public FP Max;
        
        public Health(FP max)
        {
            Max = max;
            Current = max;
        }
        
        public Health(FP current, FP max)
        {
            Current = current;
            Max = max;
        }
        
        public FP Ratio => Max > 0 ? Current / Max : 0;
        public bool IsDead => Current <= 0;
    }
}