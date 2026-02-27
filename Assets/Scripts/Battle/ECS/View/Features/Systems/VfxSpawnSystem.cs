using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Extend.System;
using Battle.ECS.Component;
using Battle.ECS.Core;
using Battle.ECS.View;
using FixMath;
using JKFrame;
using UnityEngine;

namespace Battle.ECS.System
{
    /// <summary>
    /// 特效生成系统
    /// </summary>
    public class VfxSpawnSystem : IUpdateLevelSystem<Always>, ILoadViewSystem, IUnloadViewSystem
    {
        private readonly BattleContext _context;
        private readonly QueryDescription _desc = new QueryDescription().WithAll<VfxData>();
        private readonly HashSet<int> _prewarmed = new();
        private Camera _camera;

        public VfxSpawnSystem(BattleContext context)
        {
            _context = context;
        }

        public void Update()
        {
            var world = _context.World;
            var query = world.Query(in _desc);

            foreach (var chunk in query)
            {
                ref var firstEntity = ref chunk.Entity(0);
                ref var firstVfxData = ref chunk.GetFirst<VfxData>();

                foreach (var index in chunk)
                {
                    ref var entity = ref Unsafe.Add(ref firstEntity, index);
                    ref var vfxData = ref Unsafe.Add(ref firstVfxData, index);

                    SpawnVfx(vfxData);
                    if (!entity.Has<Death>())
                        entity.Add<Death>();
                }
            }
        }

        public void LoadView(BattleViewReference viewReference)
        {
            _camera = viewReference?.Camera;
        }

        public void UnloadView()
        {
            _camera = null;
        }

        private void SpawnVfx(in VfxData vfxData)
        {
            if (vfxData.Prefab == null) return;

            RayDebug.Log($"创建特效vfx: {vfxData.Prefab.name}");
            // Prewarm(vfxData.Prefab);
            var go = ProjectUtility.GetOrInstantiateGameObjectClone(vfxData.Prefab, null);
            if (go == null) return;

            go.transform.position = vfxData.Position;
            go.transform.rotation = vfxData.Rotation;
            go.transform.localScale = vfxData.Scale == Vector3.zero ? Vector3.one : vfxData.Scale;

            if (vfxData.LookAtCamera && _camera != null)
                go.transform.LookAt(_camera.transform.position);

            if (vfxData.AutoDestroy)
            {
                var effect = go.GetComponent<EffectController>();
                if (effect == null)
                {
                    // 预制体没有挂 EffectController，自动添加并使用默认销毁时间
                    effect = go.AddComponent<EffectController>();
                    effect.destroyTime = 3f; // 默认 3 秒销毁
                }
                // Duration > 0 时用 ECS 侧的时长覆盖，否则保留预制体上配置的 destroyTime
                if (vfxData.Duration > FP.Zero)
                    effect.destroyTime = (float)vfxData.Duration;
                effect.Init();
            }
        }

        private void Prewarm(GameObject prefab)
        {
            var id = prefab.GetInstanceID();
            if (_prewarmed.Contains(id)) return;
            _prewarmed.Add(id);
            PoolSystem.InitGameObjectPool(prefab, maxCapacity: 30, defaultQuantity: 1);
        }
    }
}
