using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Extend.System;
using Battle.ECS.Component;
using Config;
using Data;
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
        /// 由 DropOnDeath 调用；背包丢弃等场景可通过 forceWorldDrop 强制生效。
        /// </summary>
        public bool SpawnWorldDrop(ItemConfig config, int count, Vector3 position, float lockDelay = -1f, string existingGuid = null, bool forceWorldDrop = false)
        {
            if (config == null) return false;
            if (!config.SpawnAsWorldDrop && !forceWorldDrop) return false;
            if (count <= 0) return false;

            bool spawned = false;

            // 检查堆叠：如果最大堆叠数为 1（不可堆叠），且请求生成数量 > 1，则拆解为多个实体
            if (config.MaxStackCount <= 1 && count > 1)
            {
                for (int i = 0; i < count; i++)
                {
                    spawned |= SpawnSingleWorldDrop(config, 1, position, lockDelay, existingGuid);
                }
            }
            else
            {
                spawned = SpawnSingleWorldDrop(config, count, position, lockDelay, existingGuid);
            }

            return spawned;
        }

        private bool SpawnSingleWorldDrop(ItemConfig config, int count, Vector3 position, float lockDelay, string existingGuid)
        {
            var prefab = config.WorldDropPrefab != null ? config.WorldDropPrefab : _defaultDropPrefab;
            if (prefab == null)
            {
                RayDebug.Error("没有默认掉落物预制体，请在 Inspector 赋值！");
                return false;
            }

            var go = ProjectUtility.GetOrInstantiateGameObjectClone(prefab, null);
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

            // 【存档检查】：如果世界掉落生命周期是 -1（无限），我们需要将其记录
            if (config.WorldDropLifetime < 0)
            {
                if (string.IsNullOrEmpty(existingGuid))
                {
                    // 这是一个全新生成的无限期掉落物，存入存档
                    string newGuid = System.Guid.NewGuid().ToString();
                    item.PersistentGuid = newGuid;
                    SavePersistentDrop(newGuid, config.ItemId, count, position);
                }
                else
                {
                    // 这是一个从存档恢复的掉落物
                    item.PersistentGuid = existingGuid;
                }
            }
            else
            {
                item.PersistentGuid = null;
            }

            // 手动拾取列表由触发器进出范围注册/注销，避免远距离就显示 UI
            item.ApplyBounceForce(lockDelay);
            return true;
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

            // 如果是持久化掉落物，拾取后要从存档中移除
            if (!string.IsNullOrEmpty(item.PersistentGuid))
            {
                RemovePersistentDrop(item.PersistentGuid);
            }

            DestroyDrop(item);
        }

        // ── 存档操作 ──────────────────────────────────────────

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
            // 如果是持久化掉落物，先从存档移除，防止读档后重复生成
            if (item != null && !string.IsNullOrEmpty(item.PersistentGuid))
            {
                RemovePersistentDrop(item.PersistentGuid);
            }
            DestroyDrop(item);
        }

        // ── 无限期掉落物持久化操作 ──────────────────────────────

        private void SavePersistentDrop(string guid, int itemId, int count, Vector3 pos)
        {
            if (DataManager.GameData == null || DataManager.GameData.PersistentDrops == null) return;

            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (!DataManager.GameData.PersistentDrops.Dictionary.TryGetValue(sceneName, out var list))
            {
                list = new Serialized_List<PersistentDropData>();
                DataManager.GameData.PersistentDrops.Dictionary[sceneName] = list;
            }

            list.List.Add(new PersistentDropData
            {
                Guid = guid,
                ItemId = itemId,
                Count = count,
                Position = pos
            });

            DataManager.SaveGameData();
        }


        private void RemovePersistentDrop(string guid)
        {
            if (DataManager.GameData == null || DataManager.GameData.PersistentDrops == null) return;

            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (DataManager.GameData.PersistentDrops.Dictionary.TryGetValue(sceneName, out var list))
            {
                int removed = list.List.RemoveAll(d => d.Guid == guid);
                if (removed > 0)
                {
                    DataManager.SaveGameData();
                }
            }
        }

        /// <summary>
        /// 在主场景加载完毕后，由场景管理器或启动器调用，用来复原场景满地的持久化掉落物
        /// </summary>
        public void RestoreScenePersistentDrops()
        {
            if (DataManager.GameData == null || DataManager.GameData.PersistentDrops == null) return;

            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (DataManager.GameData.PersistentDrops.Dictionary.TryGetValue(sceneName, out var list))
            {
                var itemTable = ResSystem.LoadAsset<ItemTable>("ItemTable");
                if (itemTable == null) return;

                foreach (var data in list.List)
                {
                    var config = itemTable.Items.Find(i => i.ItemId == data.ItemId);
                    if (config != null)
                    {
                        // 设为 lockDelay 0 是因为恢复出的不应该再重新播放弹跳效果
                        SpawnWorldDrop(config, data.Count, data.Position, 0f, data.Guid);
                    }
                }
            }
        }
    }
}
