using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.LowLevel;

namespace Arch.Extend.EntityIndex
{
    /// <summary>
    /// 使用一个主键索引多个实体
    /// </summary>
    public class MultiEntityIndex<TKey, TComp>
    {
        public delegate TKey GetKeyHandler(in TComp comp);

        private readonly World _world;
        private readonly GetKeyHandler _getKeyHandler;
        private readonly Dictionary<TKey, HashSet<Entity>> _entities = new();

        public MultiEntityIndex(World world, GetKeyHandler getKeyHandler)
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
        /// 通过主键获取实体列表
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public HashSet<Entity> GetEntities(TKey key)
        {
            return _entities.GetValueOrDefault(key);
        }

        /// <summary>
        /// 获取指定key下实体的数量
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int GetEntityCount(TKey key)
        {
            return _entities.TryGetValue(key, out var entitySet) ? entitySet.Count : 0;
        }

        private void OnComponentAdded(in Entity entity, ref TComp comp)
        {
            IndexEntity(in entity, in comp);
        }

        private void OnComponentRemoved(in Entity entity, ref TComp comp)
        {
            var key = _getKeyHandler(in comp);
            if (_entities.TryGetValue(key, out var entitySet))
            {
                entitySet.Remove(entity);
            }
        }

        private void OnComponentSet(in Entity entity, ref TComp comp)
        {
            var key = _getKeyHandler(in comp);
            var set = IndexEntity(entity, key);
            foreach (var (_, value) in _entities)
            {
                if (value == set) continue;
                value.Remove(entity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private HashSet<Entity> IndexEntity(in Entity entity, in TComp comp)
        {
            var key = _getKeyHandler(in comp);
            return IndexEntity(in entity, in key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private HashSet<Entity> IndexEntity(in Entity entity, in TKey key)
        {
            if (_entities.TryGetValue(key, out var entitySet) == false)
            {
                entitySet = new HashSet<Entity>();
                _entities[key] = entitySet;
            }
            entitySet.Add(entity);
            return entitySet;
        }
    }
}
