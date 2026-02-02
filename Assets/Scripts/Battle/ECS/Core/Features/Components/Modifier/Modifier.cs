using Arch.Core;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 修改器组件，当来源或目标任一个Entity被销毁时，修改器也会被销毁
    /// </summary>
    public readonly struct Modifier
    {
        public readonly Entity Source;
        public readonly Entity Target;

        public Modifier(Entity source, Entity target)
        {
            Source = source;
            Target = target;
        }
    }
}