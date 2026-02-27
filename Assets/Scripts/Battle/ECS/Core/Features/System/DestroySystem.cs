using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Extend.System;
using Battle.ECS;
using Battle.ECS.Component;
using Battle.ECS.Core;
using System.Runtime.CompilerServices;
using Battle.ECS.Core.Helper;
using FixMath;
using JKFrame;

namespace Battle.ECS.System
{
    /// <summary>
    /// 延迟销毁系统 - 清理标记为Destroy的实体
    /// </summary>
    public class DestroySystem : IUpdateLevelSystem<GameFree>, ICleanupSystem
    {
        private readonly BattleContext _context;
        private readonly QueryDescription _destroyQuery = new QueryDescription().WithAll<Destroy>();
        private readonly List<Entity> _toDestroyList = new(32);
        
        public DestroySystem(BattleContext context)
        {
            _context = context;
        }

        public void Update()
        {
        }

        public void Cleanup()
        {
            var processor = new DestroyProcessor()
            {
                DeltaTime = _context.LogicTime.DeltaTime,
                ToDestroyList = _toDestroyList
            };
            _toDestroyList.Clear();
            _context.World.InlineEntityQuery<DestroyProcessor, Destroy>(in _destroyQuery, ref processor);

            foreach (var entity in _toDestroyList)
            {
                if (!entity.IsAlive())
                    ThrowHelper.Throw($"{entity.GetDebugInfo()} already destroyed!");
                if (entity.Has<ViewReference>())
                {
                    ref var viewRef = ref entity.Get<ViewReference>();
                    if (viewRef.ViewObject != null)
                    {
                        // 有死亡回调的角色（Player/Boss）：GO 由死亡动画结束后自行销毁或回池
                        // 不在此处回收，避免 GO 被销毁时死亡动画还未播完
                        bool hasDeathCallback = viewRef.ViewObject.GetComponentInParent<IDeathCallback>() != null;
                        if (!hasDeathCallback)
                        {
                            viewRef.ViewObject.GameObjectPushPool();
                        }
                    }
                }
                entity.Destroy();
            }
        }

        private struct DestroyProcessor : IForEachWithEntity<Destroy>
        {
            public FP DeltaTime;
            public List<Entity> ToDestroyList;
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update(Entity entity, ref Destroy destroy)
            {
                if (destroy.IsCompleted())
                {
                    ToDestroyList.Add(entity);
                }
                else
                    destroy.Update(DeltaTime);
            }
        }
    }
}
