using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
namespace Arch.Extend.System
{
    public static class WorldExtensions
    {
        /// <summary>
        /// 通过描述文件找到一个实体，找不到则返回Entity.Null
        /// </summary>
        /// <param name="world"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static Entity FirstEntity(this World world, QueryDescription description)
        {
            Query query = world.Query(description);
            foreach (Chunk chunk in query)
            {
                return chunk.Entity(0);
            }
            return Entity.Null;
        }

        /// <summary>
        /// 通过描述文件找到一个组件, 找不到返回空引用
        /// </summary>
        /// <param name="world"></param>
        /// <param name="description"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ref T First<T>(this World world, QueryDescription description)
        {
            Query query = world.Query(description);
            foreach (Chunk chunk in query)
            {
                return ref chunk.GetFirst<T>();
            }
            return ref Unsafe.NullRef<T>();
        }

        /// <summary>
        /// 销毁所有实体
        /// </summary>
        /// <param name="world"></param>
        public static void DestroyAll(this World world)
        {
            var description = new QueryDescription();
            world.Destroy(in description);
        }

        /// <summary>
        /// 将查询结果复制到一个实体集合中
        /// </summary>
        /// <param name="world"></param>
        /// <param name="description"></param>
        /// <param name="entities"></param>
        public static void CollectEntities(this World world, in QueryDescription description, ICollection<Entity> entities)
        {
            var query = world.Query(in description);
            foreach (ref var chunk in query)
            {
                ref var entityFirstElement = ref chunk.Entity(0);
                foreach (var entityIndex in chunk)
                {
                    var entity = Unsafe.Add(ref entityFirstElement, entityIndex);
                    entities.Add(entity);
                }
            }
        }
    }
}
