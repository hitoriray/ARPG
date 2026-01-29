using System;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.LowLevel;

namespace Arch.Extend.EntityIndex
{
    /// <summary>
    /// 用于建立对世界中有某个独一无组件的实体的引用
    /// </summary>
    public class SingleEntityIndex<T>
    {
        private readonly World _world;
        private Entity _entity = Entity.Null;

        public SingleEntityIndex(World world)
        {
            _world = world;
            using var entities = new UnsafeArray<Entity>(world.Size);
            _world.GetEntities(new QueryDescription(), entities.AsSpan());
            foreach (Entity entity in entities)
            {
                if (_world.Has<T>(entity)) IndexEntity(in entity);
            }
            world.SubscribeComponentAdded<T>(OnComponentAdded);
            world.SubscribeComponentRemoved<T>(OnComponentRemoved);
        }

        /// <summary>
        /// 获取实体，不存在返回一个Entity.Null
        /// </summary>
        /// <returns></returns>
        public Entity GetEntity()
        {
            return _entity;
        }

        /// <summary>
        /// 尝试获取这个组件，不存在会返回一个空引用
        /// </summary>
        /// <returns></returns>
        public ref T TryGet(out bool exists)
        {
            if (_entity == Entity.Null)
            {
                exists = false;
                return ref Unsafe.NullRef<T>();
            }

            return ref _world.TryGetRef<T>(_entity, out exists);
        }

        /// <summary>
        /// 尝试获取指定组件
        /// </summary>
        /// <param name="exists"></param>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        public ref TComponent TryGet<TComponent>(out bool exists)
        {
            if (_entity == Entity.Null)
            {
                exists = false;
                return ref Unsafe.NullRef<TComponent>();
            }
            return ref _world.TryGetRef<TComponent>(_entity, out exists);
        }

        private void OnComponentRemoved(in Entity entity, ref T comp)
        {
            if (_entity == entity) _entity = Entity.Null;
        }

        private void OnComponentAdded(in Entity entity, ref T comp)
        {
            IndexEntity(in entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IndexEntity(in Entity entity)
        {
            if (_entity == entity)
            {
                throw new Exception($"Duplicate component of type {typeof(T)} detected. Entity {entity} tried to add when another entity already has this unique component.");
            }
            _entity = entity;
        }
    }
}
