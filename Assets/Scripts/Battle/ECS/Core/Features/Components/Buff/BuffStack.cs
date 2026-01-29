using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.LowLevel;
using Battle.ECS.Core.Interfaces;
using FixMath;

namespace Battle.ECS.Component
{
    /// <summary>
    /// Buff堆叠组件
    /// </summary>
    public struct BuffStack : IRelease
    {
        public UnsafeList<BuffStackInfo> Value;

        public BuffStack(int capacity)
        {
            Value = new UnsafeList<BuffStackInfo>(capacity);
        }

        public void Release()
        {
            Value.Dispose();
        }

        /// <summary>
        /// 添加一个Buff堆叠信息
        /// </summary>
        /// <param name="info">要添加的堆叠信息</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(BuffStackInfo info)
        {
            Value.Add(info);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveFirst()
        {
            if (Value.Count <= 0) return;
            Value.RemoveAt(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveLast()
        {
            if (Value.Count <= 0) return;
            Value.RemoveAt(Value.Count - 1);
        }

        /// <summary>
        /// 根据施法者移除Buff堆叠
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="removeAll">是否移除这个施法者所有的堆叠</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(Entity caster, bool removeAll)
        {
            if (Value.Count == 0) return;
            for (int i = Value.Count - 1; i >= 0; i--)
            {
                if (Value[i].Caster == caster)
                {
                    Value.RemoveAt(i);
                    if (removeAll == false)
                        break; // 只移除第一个匹配的施法者
                }
            }
        }

        /// <summary>
        /// 是否存在某个施法者的Buff堆叠
        /// </summary>
        /// <param name="caster"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(Entity caster)
        {
            if (Value.Count == 0) return false;
            for (int i = Value.Count - 1; i >= 0; i--)
            {
                if (Value[i].Caster == caster)
                    return true;
            }
            return false;
        }
    }

    // buff堆叠信息
    public struct BuffStackInfo
    {
        public Entity Caster;
        public FP RemainingTime;
    }
}