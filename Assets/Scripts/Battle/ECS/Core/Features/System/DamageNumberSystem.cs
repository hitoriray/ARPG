using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Extend.System;
using Battle.ECS.Component;
using Battle.ECS.Core;
using UnityEngine;

namespace Battle.ECS.System
{
    /// <summary>
    /// 飘字系统 — 消费 DamageNumberRequest，调用 GO 层 DamageNumberManager
    /// 注册在 LocalLogicFeature 的 DamageSystem 之后
    /// </summary>
    public class DamageNumberSystem : IUpdateLevelSystem<GameLogic>
    {
        private readonly BattleContext _context;

        private readonly QueryDescription _query =
            new QueryDescription().WithAll<DamageNumberRequest>();

        private readonly List<Entity> _entities = new(16);

        public DamageNumberSystem(BattleContext context) => _context = context;

        public void Update()
        {
            if (_context.DamageNumberService == null) return;

            _entities.Clear();
            _context.World.CollectEntities(in _query, _entities);

            foreach (var entity in _entities)
            {
                ref var req = ref entity.Get<DamageNumberRequest>();
                _context.DamageNumberService.Spawn(req.Damage, req.IsCritical, req.IsHeal, req.WorldPos);
                entity.Remove<DamageNumberRequest>();
            }
        }
    }
}
