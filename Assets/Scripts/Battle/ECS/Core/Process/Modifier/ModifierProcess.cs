using Arch.Core;
using Arch.Core.Extensions;
using Battle.ECS.Component;

namespace Battle.ECS.Core.Process
{
    public abstract class ModifierProcess : ProcessBase, IDeathProcess
    {
        /// <summary>
        ///  应用效果
        /// </summary>
        /// <param name="entity"></param>
        public abstract void Apply(in Entity entity);

        /// <summary>
        /// 移除效果
        /// </summary>
        /// <param name="entity"></param>
        public virtual void OnDeath(in Entity entity)
        {
            entity.Add<Destroy>();
        }
    }
}