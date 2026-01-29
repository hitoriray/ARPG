using System.Runtime.CompilerServices;
using Battle.ECS.Core.Interfaces;
using FixMath;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 旋转组件，用于描述物体在世界中的旋转
    /// </summary>
    public struct Rotation : IInterpolatable, IRotationData
    {
        public TSQuaternion LastValue { get; private set; }     // 上一帧的旋转
        public TSQuaternion Value { get; private set; }         // 当前帧的旋转
        public bool IsDirty { get; private set; }
        public bool IsDirectly { get; private set; }

        public TSQuaternion Quaternion => Value;

        public Rotation(TSQuaternion value)
        {
            LastValue = value;
            Value = value;
            IsDirty = true;
            IsDirectly = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(TSQuaternion value)
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
        public void SetDirectly(TSQuaternion value)
        {
            LastValue = value;
            Value = value;
            IsDirty = true;
            IsDirectly = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCurrent(TSQuaternion value)
        {
            Value = value;
            IsDirty = true;
            IsDirectly = false;
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