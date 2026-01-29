using Arch.Extend.System;
using Battle.ECS.Core;

namespace Battle.ECS.Features
{
    /// <summary>
    /// 逻辑Feature - 包含所有战斗逻辑系统
    /// </summary>
    public class LogicFeatureBase : BattleFeature
    {
        private readonly BattleContext _context;
        private int _lastFrameCount;
        private bool _updated;

        public LogicFeatureBase(BattleContext context) : base(context)
        {
            _context = context;
        }

        public sealed override Systems Add(ISystem system)
        {
            return base.Add(system);
        }

        public override void Update()
        {
            var frameCount = _context.LogicTime.FrameCount;
            if (frameCount > _lastFrameCount)
            {
                _lastFrameCount = frameCount;
                _updated = true;
                base.Update();
            }
        }

        public override void Cleanup()
        {
            if (_updated) base.Cleanup();
            _updated = false;
        }
    }
}
