namespace Battle.ECS.Component
{
    /// <summary>
    /// 玩家实体组件，用于标识这是一个玩家实体
    /// </summary>
    public struct Player
    {
        public int PlayerId;
        public Player(int playerId) => PlayerId = playerId;
    }
}