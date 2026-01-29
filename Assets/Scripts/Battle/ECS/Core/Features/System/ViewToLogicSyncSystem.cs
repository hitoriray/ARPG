using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Extend.System;
using Battle.ECS.Component;
using Battle.ECS.Core;
using FixMath;
using Player;
using UnityEngine;

namespace Battle.ECS.System
{
    /// <summary>
    /// 将View的Transform回写到逻辑层(Position/Rotation)
    /// </summary>
    public class ViewToLogicSyncSystem : IUpdateLevelSystem<GameFree>
    {
        private readonly BattleContext _context;
        private readonly QueryDescription _posDesc = new QueryDescription().WithAll<Position, ViewReference, SyncFromView>().WithNone<Death>();
        private readonly QueryDescription _rotDesc = new QueryDescription().WithAll<Rotation, ViewReference, SyncFromView>().WithNone<Death>();

        public ViewToLogicSyncSystem(BattleContext context)
        {
            _context = context;
        }

        public void Update()
        {
            SyncPositions();
            SyncRotations();
        }

        private void SyncPositions()
        {
            var world = _context.World;
            var query = world.Query(in _posDesc);

            foreach (var chunk in query)
            {
                ref var firstPosition = ref chunk.GetFirst<Position>();
                ref var firstViewRef = ref chunk.GetFirst<ViewReference>();

                foreach (int i in chunk)
                {
                    ref var position = ref Unsafe.Add(ref firstPosition, i);
                    ref var viewRef = ref Unsafe.Add(ref firstViewRef, i);

                    var viewTransform = GetViewTransform(ref viewRef);
                    if (viewTransform == null) continue;
                    position.SetDirectly((TSVector3)viewTransform.position);
                }
            }
        }

        private void SyncRotations()
        {
            var world = _context.World;
            var query = world.Query(in _rotDesc);

            foreach (var chunk in query)
            {
                ref var firstRotation = ref chunk.GetFirst<Rotation>();
                ref var firstViewRef = ref chunk.GetFirst<ViewReference>();

                foreach (int i in chunk)
                {
                    ref var rotation = ref Unsafe.Add(ref firstRotation, i);
                    ref var viewRef = ref Unsafe.Add(ref firstViewRef, i);

                    var viewTransform = GetViewTransform(ref viewRef);
                    if (viewTransform == null) continue;
                    rotation.SetDirectly((TSQuaternion)viewTransform.rotation);
                }
            }
        }

        private static Transform GetViewTransform(ref ViewReference viewRef)
        {
            if (viewRef.ViewObject != null) return viewRef.ViewObject.transform;
            if (viewRef.View is PlayerView playerView) return playerView.transform;
            return null;
        }
    }
}
