using Arch.Extend.System;

namespace Battle.ECS.Core
{
    /// <summary>
    /// 战斗Feature - 实现UpdateLevel机制
    /// </summary>
    public class BattleFeature : Feature, IUpdateLevelSystem<Always>
    {
        private readonly BattleContext _context;
        
        public BattleFeature(BattleContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 根据UpdateLevel过滤系统执行
        /// </summary>
        protected override bool CheckUpdate(IUpdateSystem system)
        {
            UpdateLevel systemUpdateLevel;
            if (system is IUpdateLevelSystem updateSystem)
                systemUpdateLevel = updateSystem.GetUpdateLevel();
            else 
                systemUpdateLevel = UpdateLevel.LogicTime;

            // 只有当系统的UpdateLevel >= 当前Context的UpdateLevel时才执行
            return systemUpdateLevel >= _context.UpdateLevel;
        }

        protected override bool CheckCleanup(ICleanupSystem system)
        {
            UpdateLevel systemUpdateLevel;
            if (system is IUpdateLevelSystem updateSystem)
                systemUpdateLevel = updateSystem.GetUpdateLevel();
            else 
                systemUpdateLevel = UpdateLevel.LogicTime;

            return systemUpdateLevel >= _context.UpdateLevel;
        }
    }
}
