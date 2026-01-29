using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.LowLevel;

namespace Arch.Extend.EntityIndex
{
    /// <summary>
    /// 使用一个主键建立对世界中实体的引用
    /// </summary>
    public class PrimaryEntityIndex<TKey, TComp>
    {
        public delegate TKey GetKeyHandler(in TComp comp);

        private readonly World _world;
        private readonly GetKeyHandler _getKeyHandler;
        private readonly Dictionary<TKey, Entity> _entities = new();

        public PrimaryEntityIndex(World world, GetKeyHandler getKeyHandler)
        {
            _world = world;
            _getKeyHandler = getKeyHandler;
            using var entities = new UnsafeArray<Entity>(world.Size);
            _world.GetEntities(new QueryDescription(), entities.AsSpan());
            foreach (Entity entity in entities)
            {
                ref TComp comp = ref _world.TryGetRef<TComp>(entity, out var exists);
                if (exists) IndexEntity(in entity, in comp);
            }
            world.SubscribeComponentAdded<TComp>(OnComponentAdded);
            world.SubscribeComponentRemoved<TComp>(OnComponentRemoved);
            world.SubscribeComponentSet<TComp>(OnComponentSet);
        }

        /// <summary>
        /// 通过主键获取实体，如果不存在则返回Entity.Null
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Entity GetEntity(TKey key)
        {
            return _entities.TryGetValue(key, out var entity) ? entity : Entity.Null;
        }

        /// <summary>
        /// 尝试获取实体，如果不存在则返回false
        /// </summary>
        /// <param name="key"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool TryGetEntity(TKey key, out Entity entity)
        {
            return _entities.TryGetValue(key, out entity);
        }

        private void OnComponentAdded(in Entity entity, ref TComp comp)
        {
            IndexEntity(in entity, in comp);
        }

        private void OnComponentRemoved(in Entity entity, ref TComp comp)
        {
            var key = _getKeyHandler(in comp);
            if (_entities.Remove(key, out var e) == false || e != entity)
            {
                throw new Exception($"Entity({typeof(TComp)}) for key '{key}' not found");
            }
        }

        private void OnComponentSet(in Entity entity, ref TComp comp)
        {
            var key = _getKeyHandler(in comp);
            if (_entities.TryGetValue(key, out var e) && e == entity)
            {
                //还是同一个实体，什么都不做
                return;
            }
            //更新了主键需要重新索引
            foreach (var kv in _entities)
            {
                if (kv.Value == entity)
                {
                    _entities.Remove(kv.Key);
                    break;
                }
            }
            IndexEntity(entity, key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IndexEntity(in Entity entity, in TComp comp)
        {
            var key = _getKeyHandler(in comp);
            IndexEntity(in entity, in key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IndexEntity(in Entity entity, in TKey key)
        {
            if (_entities.TryAdd(key, entity) == false)
            {
                throw new Exception($"Entity({typeof(TComp)}) for key '{key}' already exists, Only one entity for a primary key is allowed.");
            }
        }
    }
}
