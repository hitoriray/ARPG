using System;
using System.Collections.Generic;
using Arch.Core;

namespace Arch.Extend.SharedComponent
{
    /// <summary>
    /// 共享组件管理器，负责跟踪和管理所有共享组件
    /// 维护共享组件与Archetype的映射关系
    /// </summary>
    public class SharedComponentManager
    {
        private readonly World _world;
        private readonly Dictionary<Type, Dictionary<ISharedComponent, int>> _sharedComponentToId = new();
        private readonly Dictionary<Type, List<ISharedComponent>> _idToSharedComponent = new();

        public SharedComponentManager(World world)
        {
            _world = world;
        }

        /// <summary>
        /// 获取共享组件的唯一ID，如果不存在则创建
        /// </summary>
        public int GetSharedComponentId<T>(in T sharedComponent) where T : struct, ISharedComponent<T>
        {
            var type = typeof(T);

            if (!_sharedComponentToId.TryGetValue(type, out var componentMap))
            {
                componentMap = new Dictionary<ISharedComponent, int>();
                _sharedComponentToId[type] = componentMap;
                _idToSharedComponent[type] = new List<ISharedComponent>();
            }

            if (!componentMap.TryGetValue(sharedComponent, out var id))
            {
                id = componentMap.Count;
                componentMap.Add(sharedComponent, id);
                _idToSharedComponent[type].Add(sharedComponent);
            }

            return id;
        }

        /// <summary>
        /// 通过ID获取共享组件实例
        /// </summary>
        public T GetSharedComponent<T>(int id) where T : struct, ISharedComponent<T>
        {
            var type = typeof(T);
            if (_idToSharedComponent.TryGetValue(type, out var components) &&
                id >= 0 && id < components.Count)
            {
                return (T) components[id];
            }

            throw new ArgumentOutOfRangeException(nameof(id), "Invalid shared component ID");
        }

        /// <summary>
        /// 为实体添加共享组件，会导致实体迁移到合适的Archetype
        /// </summary>
        public void AddSharedComponent<T>(in Entity entity, in T sharedComponent) where T : struct, ISharedComponent<T>
        {
            var id = GetSharedComponentId(sharedComponent);
            // 在Arch中，我们需要创建包含共享组件ID的新组件来模拟共享组件
            _world.Add(entity, new SharedComponentRef<T>(id));
        }

        /// <summary>
        /// 更新实体的共享组件，可能导致实体迁移到新的Archetype
        /// </summary>
        public void SetSharedComponent<T>(in Entity entity, in T sharedComponent) where T : struct, ISharedComponent<T>
        {
            var id = GetSharedComponentId(sharedComponent);
            if (_world.Has<SharedComponentRef<T>>(entity))
            {
                _world.Set(entity, new SharedComponentRef<T>(id));
            }
            else
            {
                _world.Add(entity, new SharedComponentRef<T>(id));
            }
        }
    }

    /// <summary>
    /// 共享组件引用，存储共享组件的ID
    /// 实际的共享组件数据由SharedComponentManager管理
    /// </summary>
    /// <typeparam name="T">共享组件类型</typeparam>
    public struct SharedComponentRef<T> where T : struct, ISharedComponent<T>
    {
        public readonly int Id;

        public SharedComponentRef(int id)
        {
            Id = id;
        }
    }
}