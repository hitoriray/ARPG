using System.Runtime.CompilerServices;
using Battle.ECS.Core.Interfaces;
using FixMath;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 位置组件
    /// </summary>
    public struct Position : IInterpolatable
    {
        public TSVector3 LastValue; // 上一帧位置
        public TSVector3 Value;     // 当前帧位置
        public bool IsDirty { get; private set; }
        public bool IsDirectly { get; private set; }

        public Position(TSVector3 value)
        {
            LastValue = value;
            Value = value;
            IsDirty = true;
            IsDirectly = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(TSVector3 value)
        {
            LastValue = Value;
            Value = value;
            IsDirty = true;
            IsDirectly = false;
        }
        
        /// <summary>
        /// 直接设置，不需要插值表现
        /// </summary>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDirectly(TSVector3 value)
        {
            LastValue = value;
            Value = value;
            IsDirty = true;
            IsDirectly = true;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetInterpolatableState()
        {
            LastValue = Value;
            IsDirty = false;
            IsDirectly = false;
        }
    }
}