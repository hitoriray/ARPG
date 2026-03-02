using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Extend.System;
using Battle.ECS.Component;
using Config;
using Item;
using FixMath;
using JKFrame;
using UI;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// 掉落物管理器（单例 MonoBehaviour）。
    /// 职责：
    ///   1. 对象池管理 WorldDropItem GameObject
    ///   2. Spawn：生成掉落物 ECS Entity（DropItem + ViewReference）+ GO，施加弹射力
    ///   3. Update：Physics.OverlapSphereNonAlloc 检测自动拾取
    ///   4. LifetimeProcessor：InlineEntityQuery 每帧批量递减 Lifetime，到期回收
    /// </summary>
    public class LootDropManager : SingletonMono<LootDropManager>
    {
        [Header("默认掉落物预制体（ItemConfig 未配置时使用）")]
        [SerializeField] private GameObject _defaultDropPrefab;

        [Header("自动拾取检测半径（玩家为圆心）")]
        [SerializeField] private float _autoPickupRadius = 2.5f;

        [Header("物品拾取检测Trigger Layer")]
        [SerializeField] private LayerMask _dropItemLayer;

        // ── ECS World（独立，不归 BattleContext 管）───────────────
        private World _world;

        // 查询：同时有 DropItem 和 ViewReference 组件
        private readonly QueryDescription _lifetimeQuery =
            new QueryDescription().WithAll<DropItem, ViewReference>();

        private readonly List<Entity> _toDestroy = new(16);
        
        // ── 物理检测 buffer（预分配，0 GC）────────────────────────
        private readonly Collider[] _overlapBuffer = new Collider[64];

        // ── 生命周期 ──────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _world = World.Create();
        }

        private void OnDestroy()
        {
            if (_world != null)
            {
                // 清理还在场上的所有掉落物实体
                _toDestroy.Clear();
                _world.CollectEntities(in _lifetimeQuery, _toDestroy);
                foreach (var entity in _toDestroy)
                {
                    ref var viewRef = ref _world.Get<ViewReference>(entity);
                    if (viewRef.ViewObject != null)
                    {
                        var item = viewRef.ViewObject.GetComponent<WorldDropItem>();
                        if (item != null)
                        {
                            item.Reset();
                            item.gameObject.GameObjectPushPool();
                        }
                    }
                }
                
                _toDestroy.Clear();
                _world.Dispose();
            }
        }

        private void Update()
        {
            TickLifetimes();
            DetectAutoPickup();
        }

        // ── 公开 API ───────────────────────────────────────────

        /// <summary>
        /// 在世界中生成掉落物（ECS Entity + GO）。
        /// 由 DropOnDeath 调用。
        /// </summary>
        public void SpawnWorldDrop(ItemConfig config, int count, Vector3 position, float lockDelay = -1f)
        {
            if (config == null || !config.SpawnAsWorldDrop) return;

            var prefab = config.WorldDropPrefab != null ? config.WorldDropPrefab : _defaultDropPrefab;
            if (prefab == null)
            {
                RayDebug.Error("没有默认掉落物预制体，请在 Inspector 赋值！");
                return;
            }

            var go = ProjectUtility.GetOrInstantiateGameObject(prefab, null);
            go.transform.position = position;
            go.SetActive(true);

            // 创建 ECS Entity：DropItem（纯数据）+ ViewReference（GO 引用）
            var entity = _world.Create(
                new DropItem
                {
                    Config   = config,
                    Count    = count,
                    Lifetime = (FP)config.WorldDropLifetime,
                },
                new ViewReference(go)
            );

            // 初始化 WorldDropItem MonoBehaviour
            var item = go.GetComponent<WorldDropItem>();
            if (item == null) item = go.AddComponent<WorldDropItem>();
            item.Init(config, count, entity);

            // 手动拾取列表由触发器进出范围注册/注销，避免远距离就显示 UI
            item.ApplyBounceForce(lockDelay);
        }

        /// <summary>
        /// 拾取掉落物（自动 or 手动）。
        /// </summary>
        public void Collect(WorldDropItem item)
        {
            if (item == null || item.Config == null) return;

            int realAdd = InventoryManager.AddItem(item.Config, item.Count);
            if (realAdd > 0)
            {
                var ui = UISystem.GetWindow<UI_GameSceneMainWindow>();
                if (ui != null)
                    ui.ShowPickupNotification(item.Config, realAdd);
            }

            if (!item.Config.AutoPickup)
                InteractManager.Instance?.UnregisterDropItem(item);

            DestroyDrop(item);
        }

        // ── 内部逻辑 ──────────────────────────────────────────

        private void TickLifetimes()
        {
            _toDestroy.Clear();

            var processor = new LifetimeProcessor
            {
                DeltaTime = (FP)Time.deltaTime,
                ToDestroy = _toDestroy,
            };
            _world.InlineEntityQuery<LifetimeProcessor, DropItem>(in _lifetimeQuery, ref processor);

            foreach (var entity in _toDestroy)
            {
                if (!_world.IsAlive(entity)) continue;

                ref var viewRef = ref entity.Get<ViewReference>();
                var wdi = viewRef.ViewObject?.GetComponent<WorldDropItem>();
                if (wdi != null)
                    DestroyDrop(wdi);          // DestroyDrop 内部会读 item.Entity 销毁
                else
                    _world.Destroy(entity);    // 兜底：GO 已丢失则直接删 Entity
            }
            _toDestroy.Clear();
        }

        private struct LifetimeProcessor : IForEachWithEntity<DropItem>
        {
            public FP DeltaTime;
            public List<Entity> ToDestroy;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update(Entity entity, ref DropItem drop)
            {
                if (drop.Lifetime < FP.Zero)
                {
                    // Lifetime < 0 表示无限，不做衰减与销毁
                    return;
                }

                drop.Lifetime -= DeltaTime;
                if (drop.Lifetime <= FP.Zero)
                    ToDestroy.Add(entity);
            }
        }

        private void DetectAutoPickup()
        {
            var player = PlayerService.Instance?.GetCharacterController();
            if (player == null) return;

            // 自动拾取检测
            int count = Physics.OverlapSphereNonAlloc(
                player.ModelTransform.position,
                _autoPickupRadius,
                _overlapBuffer,
                _dropItemLayer);

            for (int i = 0; i < count; i++)
            {
                var col = _overlapBuffer[i];
                if (col == null) continue;
                var dropItem = col.GetComponentInParent<WorldDropItem>();
                if (dropItem == null || dropItem.Config == null) continue;
                if (!dropItem.Config.AutoPickup) continue;

                Collect(dropItem);
            }
        }

        private void DestroyDrop(WorldDropItem item)
        {
            if (item == null) return;

            // 销毁 ECS Entity
            if (_world.IsAlive(item.Entity))
                _world.Destroy(item.Entity);

            var go = item.gameObject;
            item.Reset();
            go.GameObjectPushPool();
        }

        public void RemoveDrop(WorldDropItem item)
        {
            DestroyDrop(item);
        }
    }
}
