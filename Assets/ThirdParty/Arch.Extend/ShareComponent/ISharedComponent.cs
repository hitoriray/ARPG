using System;

namespace Arch.Extend.SharedComponent
{
    /// <summary>
    /// 共享组件接口，用于标记可以被多个实体共享的组件
    /// 共享相同数据的实体将被分配到同一个Chunk中
    /// </summary>
    public interface ISharedComponent : IEquatable<ISharedComponent>
    {
        
    }

    /// <summary>
    /// 泛型共享组件接口，提供类型安全的访问
    /// </summary>
    /// <typeparam name="T">共享组件具体类型</typeparam>
    public interface ISharedComponent<T> : ISharedComponent where T : struct, ISharedComponent<T>
    {
        bool Equals(T other);
    }
}
