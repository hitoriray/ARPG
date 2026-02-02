using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Extend.System;
using Battle.ECS.Component;
using Battle.ECS.Core;
using Battle.ECS.Core.Helper;
using Battle.ECS.Core.Process;
using UnityEngine;

namespace Battle.ECS.System
{
    /// <summary>
    /// 死亡系统，处理实体的死亡逻辑
    /// </summary>
    public class DeathSystem : IUpdateLevelSystem<GameFree>
    {
        private readonly BattleContext _context;
        private readonly QueryDescription _query = new QueryDescription().WithAll<Death>().WithNone<Destroy>();
        private readonly List<Entity> _entities = new(16);
        
        public DeathSystem(BattleContext context)
        {
            _context = context;
        }
        
        public void Update()
        {
            _entities.Clear();
            _context.World.CollectEntities(in _query, _entities);
            foreach (var entity in _entities)
            {
                ProcessDeath(entity);
            }
        }

        private void ProcessDeath(Entity entity)
        {
            // Debug.Log($"实体死亡: {entity.GetDebugInfo()}");
            ref var logicProcess = ref entity.TryGetRef<LogicProcess>(out var hasProcess);
            if (hasProcess && logicProcess.Value is IDeathProcess deathProcess)
            {
                // 有自己处理死亡逻辑的实体
                deathProcess.OnDeath(entity);
                return;
            }
            // 无处理死亡逻辑的直接挂载销毁组件
            _context.World.Add(entity, new Destroy());
        }
    }
}