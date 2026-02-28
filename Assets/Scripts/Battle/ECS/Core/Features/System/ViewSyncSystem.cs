using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Extend.System;
using Attribute;
using Battle.ECS.Component;
using Battle.ECS.Core;
using UnityEngine;

namespace Battle.ECS.System
{
    /// <summary>
    /// 表现层同步系统 - 将ECS逻辑数据同步到GameObject (View层)
    /// </summary>
    public class ViewSyncSystem : IUpdateLevelSystem<Always>
    {
        private readonly BattleContext _context;
        
        private readonly QueryDescription _positionSyncQuery = new QueryDescription().WithAll<Position, ViewReference>().WithNone<Death, SyncFromView>();
        private readonly QueryDescription _rotationSyncQuery = new QueryDescription().WithAll<Rotation, ViewReference>().WithNone<Death, SyncFromView>();
        // 查询条件：有Health和ViewReference（血量同步）
        private readonly QueryDescription _healthSyncQuery = new QueryDescription().WithAll<Health, ViewReference>();
        
        public ViewSyncSystem(BattleContext context)
        {
            _context = context;
        }
        
        public void Update()
        {
            SyncPositions();
            SyncRotations();
            SyncHealth();
        }

        /// <summary>
        /// 同步位置到View层
        /// </summary>
        private void SyncPositions()
        {
            var world = _context.World;
            var query = world.Query(in _positionSyncQuery);
            
            foreach (var chunk in query)
            {
                ref var firstPosition = ref chunk.GetFirst<Position>();
                ref var firstViewRef = ref chunk.GetFirst<ViewReference>();
                
                foreach (int i in chunk)
                {
                    ref var position = ref Unsafe.Add(ref firstPosition, i);
                    ref var viewRef = ref Unsafe.Add(ref firstViewRef, i);
                    
                    viewRef.ViewObject.transform.position = position.Value;
                }
            }
        }

        /// <summary>
        /// 同步旋转到View层
        /// </summary>
        private void SyncRotations()
        {
            var world = _context.World;
            var query = world.Query(in _rotationSyncQuery);
            
            foreach (var chunk in query)
            {
                ref var firstRotation = ref chunk.GetFirst<Rotation>();
                ref var firstViewRef = ref chunk.GetFirst<ViewReference>();
                
                foreach (int i in chunk)
                {
                    ref var rotation = ref Unsafe.Add(ref firstRotation, i);
                    ref var viewRef = ref Unsafe.Add(ref firstViewRef, i);
                    
                    viewRef.ViewObject.transform.rotation = rotation.Value;
                }
            }
        }

        /// <summary>
        /// 同步血量到GO层 CharacterAttribute
        /// </summary>
        private void SyncHealth()
        {
            var world = _context.World;
            var query = world.Query(in _healthSyncQuery);

            foreach (var chunk in query)
            {
                ref var firstHealth = ref chunk.GetFirst<Health>();
                ref var firstViewRef = ref chunk.GetFirst<ViewReference>();

                foreach (int i in chunk)
                {
                    ref var health = ref Unsafe.Add(ref firstHealth, i);
                    ref var viewRef = ref Unsafe.Add(ref firstViewRef, i);

                    if (viewRef.ViewObject == null)
                        continue;

                    var charAttr = viewRef.ViewObject.GetComponentInParent<CharacterAttribute>();
                    if (charAttr != null)
                    {
                        charAttr.SetHp((float)health.Current);
                    }
                }
            }
        }
    }
}
