using Arch.Core;
using Arch.Extend.System;
using Battle.ECS.Component;
using Battle.ECS.Core;

namespace Battle.ECS.System
{
    public class WeaponHitSystem : IUpdateLevelSystem<GameLogic>
    {
        private readonly BattleContext _context;
        private readonly QueryDescription _query = new QueryDescription().WithAll<WeaponHitRequest>().WithNone<Death, Destroy>();

        public WeaponHitSystem(BattleContext context)
        {
            _context = context;
        }
        
        public void Update()
        {
            var processor = new WeaponHitProcessor();
            _context.World.InlineEntityQuery<WeaponHitProcessor, WeaponHitRequest>(in _query, ref processor);
        }

        public struct WeaponHitProcessor : IForEachWithEntity<WeaponHitRequest>
        {
            public void Update(Entity entity, ref WeaponHitRequest req)
            {
                if (req.Behaviour == null || req.Target == null)
                    return;
                // TODO：先走原逻辑
                req.Behaviour.OnAttackDetection(req.Target, req.AttackData);
                entity.TryAdd(new Death());
            }
        }
    }
}