using System.Collections.Generic;
using Arch.Core;
using Arch.Extend.System;
using Battle.ECS.Component;
using Battle.ECS.Core;
using Battle.ECS.Core.Interfaces;

namespace Battle.ECS.System
{
    /// <summary>
    /// 重置插值组件的状态系统
    /// </summary>
    public class ResetInterpolatableStateSystem : IUpdateLevelSystem<GameFree>, IInitializeSystem
    {
        private readonly BattleContext _context;
        private bool _firstFrame;
        private readonly List<IProcessor> _processors = new();

        public ResetInterpolatableStateSystem(BattleContext context)
        {
            _context = context;
            Register<Position>();
            Register<Rotation>();
            Register<ScaleComp>();
        }

        public void Initialize()
        {
            _firstFrame = true;
        }

        private void Register<T>() where T : IInterpolatable
        {
            _processors.Add(new Processor<T>(_context.World));
        }

        public void Update()
        {
            if (_firstFrame)
            {
                //第一帧不清理，因为Initialize也会创造dirty
                _firstFrame = false;
                return;
            }

            foreach (var processor in _processors)
            {
                processor.Update();
            }
        }

        private interface IProcessor
        {
            void Update();
        }

        private class Processor<T> : IProcessor where T : IInterpolatable
        {
            private readonly World _world;
            private readonly QueryDescription _description = new QueryDescription().WithAll<T>();

            public Processor(World world)
            {
                _world = world;
            }

            public void Update()
            {
                var query = _world.Query(in _description);
                foreach (Chunk chunk in query)
                {
                    ref var first = ref chunk.GetFirst<T>();
                    foreach (int index in chunk)
                    {
                        ref var clearDirty = ref chunk.Get(ref first, index);
                        clearDirty.ResetInterpolatableState();
                    }
                }
            }
        }
    }
}