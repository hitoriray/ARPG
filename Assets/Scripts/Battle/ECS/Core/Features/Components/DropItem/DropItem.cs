using Config;
using FixMath;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 掉落物
    /// </summary>
    public struct DropItem
    {
        public ItemConfig Config;
        public int        Count;
        public FP      Lifetime;   // 剩余存活时间（秒）
    }
}
