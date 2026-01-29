namespace Battle.ECS.Component
{
    /// <summary>
    /// 怪物标签
    /// </summary>
    public struct MonsterTag
    {
        public int MonsterId;
        public MonsterTag(int monsterId) => MonsterId = monsterId;
    }
}