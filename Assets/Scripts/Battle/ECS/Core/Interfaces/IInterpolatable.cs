namespace Battle.ECS.Core.Interfaces
{
    /// <summary>
    ///  支持插值的组件
    /// </summary>
    public interface IInterpolatable
    {
        /// <summary>
        /// 重置插值状态
        /// </summary>
        void ResetInterpolatableState();
    }
}