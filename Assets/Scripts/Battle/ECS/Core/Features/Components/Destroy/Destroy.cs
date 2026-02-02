using System.Runtime.CompilerServices;
using FixMath;
using UnityEngine;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 延迟销毁
    /// </summary>
    public struct Destroy
    {
        public FP Delay;

        public Destroy(FP delay)
        {
            Delay = delay;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(FP deltaTime)
        {
            Delay -= deltaTime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsCompleted()
        {
            return Delay <= FP.Zero;
        }
    }
}