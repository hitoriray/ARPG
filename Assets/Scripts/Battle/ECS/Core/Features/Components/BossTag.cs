namespace Battle.ECS.Component
{
    /// <summary>
    /// Boss标签组件，用于标识这是一个Boss实体
    /// </summary>
    public struct BossTag
    {
        public int BossId;
        public BossTag(int bossId) => BossId = bossId;
    }
}
