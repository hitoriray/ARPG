using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Extend.System;
using Battle.ECS.Component;
using Battle.ECS.Core;

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
            RayDebug.Log($"实体死亡: Entity={entity.Id}");

            // 优先通过 ViewReference → IDeathCallback 通知 GO 层
            ref var viewRef = ref entity.TryGetRef<ViewReference>(out var hasView);
            if (hasView && viewRef.ViewObject != null)
            {
                var callback = viewRef.ViewObject.GetComponentInParent<IDeathCallback>();
                if (callback != null)
                {
                    callback.OnDeath();
                    // 实体加 Destroy 标记（让销毁系统处理）
                    entity.TryAdd(new Destroy());
                    return;
                }
            }

            // 无 IDeathCallback → 直接销毁实体
            entity.TryAdd(new Destroy());
        }
    }
}
