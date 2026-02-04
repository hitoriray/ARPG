namespace Battle.ECS.Component
{
    /// <summary>
    /// 玩家实体组件，用于标识这是一个玩家实体
    /// </summary>
    public struct PlayerComp
    {
        public int PlayerId;
        public PlayerComp(int playerId) => PlayerId = playerId;
    }
}