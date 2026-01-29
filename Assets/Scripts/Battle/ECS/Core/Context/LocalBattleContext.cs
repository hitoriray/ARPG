using FixMath;

namespace Battle.ECS.Core
{
    /// <summary>
    /// 本地战斗上下文
    /// </summary>
    public sealed class LocalBattleContext : BattleContext
    {
        public LocalBattleContext(int randomSeed, FP logicDeltaTime) : base(randomSeed, logicDeltaTime)
        {
            UpdateLevel = UpdateLevel.LogicTime;
            State.Value = BattleState.None;
        }
    }
}
