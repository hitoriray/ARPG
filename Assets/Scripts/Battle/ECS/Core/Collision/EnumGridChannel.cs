namespace Battle.Core.Collision
{
    /// <summary>
    /// 均匀网格通道
    /// </summary>
    public enum EnumGridChannel
    {
        Monster, // 怪物
        MonsterBullet, // 怪物子弹
        PlayerBullet, // 玩家子弹
        RvoObstacle, // RVO障碍物
        PvpUnit, // PVP单位
        PlayerUnit, // 玩家可被索敌和攻击的单位
    }
}
