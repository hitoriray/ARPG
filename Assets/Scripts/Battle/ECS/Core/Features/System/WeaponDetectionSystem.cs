using Arch.Core;
using Arch.Extend.System;
using Battle.ECS.Component;
using Battle.ECS.Core;
using UnityEngine;

namespace Battle.ECS.System
{
    public class WeaponDetectionSystem : IUpdateLevelSystem<GameLogic>
    {
        private readonly BattleContext _context;
        private readonly QueryDescription _query = new QueryDescription().WithAll<WeaponDetectionRequest>().WithNone<Death, Destroy>();

        public WeaponDetectionSystem(BattleContext context)
        {
            _context = context;
        }
        
        public void Update()
        {
            var processor = new WeaponDetectionProcessor();
            _context.World.InlineEntityQuery<WeaponDetectionProcessor, WeaponDetectionRequest>(in _query, ref processor);
        }

        public struct WeaponDetectionProcessor : IForEachWithEntity<WeaponDetectionRequest>
        {
            public void Update(Entity entity, ref WeaponDetectionRequest req)
            {
                if (req.Behaviour == null || req.Target == null)
                    return;
                // TODO：先走原逻辑
                Debug.Log($"[{nameof(WeaponDetectionSystem)}] 触发武器命中检测: {req.Target.GetType().Name}");
                req.Behaviour.OnAttackDetection(req.Target, req.AttackData);
                entity.TryAdd(new Death());
            }
        }
    }
}