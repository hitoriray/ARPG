using Arch.Core;

namespace Battle.ECS
{
    /// <summary>
    /// 死亡回调接口 — Player/Boss Controller 实现此接口
    /// 由 DeathSystem 通过 ViewReference 找到 GO 层并调用
    /// </summary>
    public interface IDeathCallback
    {
        void OnDeath();
    }
}
