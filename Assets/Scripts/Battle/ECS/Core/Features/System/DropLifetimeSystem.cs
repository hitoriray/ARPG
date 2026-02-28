using System.Collections.Generic;
using Arch.Core;
using Arch.Extend.System;
using Battle.ECS.Component;
using Battle.ECS.Core;
using FixMath;

namespace Battle.ECS.System
{
    public class DropLifetimeSystem : IUpdateLevelSystem<GameFree>
    {
        private readonly BattleContext _context;
        private readonly QueryDescription _query = new QueryDescription().WithAll<DropItem, ViewReference>().WithNone<Death, Destroy>();
        private readonly List<Entity> _toDestroy = new(16);
        
        public DropLifetimeSystem(BattleContext context)
        {
            _context = context;
        }

        public void Update()
        {
            _toDestroy.Clear();
            
            var processor = new DropLifetimeProcessor
            {
                DeltaTime = _context.LogicTime.DeltaTime,
                ToDestroy = _toDestroy,
            };
            _context.World.InlineEntityQuery<DropLifetimeProcessor, DropItem, ViewReference>(in _query, ref processor);

            foreach (var entity in _toDestroy)
            {
                entity.TryAdd(new Destroy());
            }
        }
        
        private struct DropLifetimeProcessor : IForEachWithEntity<DropItem, ViewReference>
        {
            public FP DeltaTime;
            public List<Entity> ToDestroy;
        
            public void Update(Entity entity, ref DropItem dropItem, ref ViewReference viewRef)
            {
                dropItem.Lifetime -= DeltaTime;
                if (dropItem.Lifetime <= FP.Zero)
                {
                    ToDestroy.Add(entity);
                }
            }
        }
    }

    
}