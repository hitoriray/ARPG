using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
namespace Arch.Extend.System
{
    public static class EntityExtensions
    {
        /// <summary>
        /// 安全获取一个组件的引用，会判断实体是否存活
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="exists"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ref T SafeGet<T>(this in Entity entity, out bool exists) where T : struct
        {
            exists = false;
            if (entity.IsAlive() == false) return ref Unsafe.NullRef<T>();
            return ref entity.TryGetRef<T>(out exists);
        }

        /// <summary>
        /// 替换一个组件，不存在会Add，存在会Set
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="component"></param>
        /// <typeparam name="T"></typeparam>
        public static void Replace<T>(this in Entity entity, T component)
        {
            if (entity.Has<T>())
                entity.Set(component);
            else
                entity.Add(component);
        }

        /// <summary>
        /// 触发一个组件的更新事件
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="T"></typeparam>
        public static void Update<T>(this in Entity entity)
        {
            var world = World.Worlds[entity.WorldId];
            world.OnComponentSet<T>(entity);
        }

        /// <summary>
        /// 添加一个组件，如果已经存在则不添加
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="component"></param>
        /// <typeparam name="T"></typeparam>
        public static bool TryAdd<T>(this in Entity entity, T component) where T : struct
        {
            if (entity.Has<T>())
                return false;
            entity.Add(component);
            return true;
        }

        /// <summary>
        /// 尝试移除一个组件
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>是否存在</returns>
        public static bool TryRemove<T>(this in Entity entity)
        {
            if (entity.Has<T>() == false)
                return false;
            entity.Remove<T>();
            return true;
        }

        /// <summary>
        /// 获取或创建一个组件的引用
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ref T GetOrCreate<T>(this in Entity entity)
        {
            if (entity.Has<T>()) return ref entity.Get<T>();
            entity.Add<T>();
            return ref entity.Get<T>();
        }

        /// <summary>
        /// 销毁实体
        /// </summary>
        /// <param name="entity"></param>
        public static void Destroy(this in Entity entity)
        {
            var world = World.Worlds[entity.WorldId];
            world.Destroy(entity);
        }
    }
}
