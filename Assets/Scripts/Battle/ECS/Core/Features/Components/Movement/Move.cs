using System.Runtime.CompilerServices;
using FixMath;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 移动组件
    /// </summary>
    public struct Move
    {
        public FP Speed;            // 基础移动速度
        public FP SpeedPct;         // 速度百分比
        public FP MinSpeedPct;      // 最小速度修正系数
        public FP ReferenceSpeed;   // 参考速度：0 表示不启用相对速度计算
        public TSVector3 Direction { get; private set; }

        public FP ActualSpeed
        {
            get
            {
                // 非相对模式：与旧逻辑一致，按基础速度与加速/减速百分比计算
                if (ReferenceSpeed == FP.Zero)
                    return Speed * TSMath.Max(FP.One + SpeedPct, MinSpeedPct);

                // 相对模式：参考速度参与计算，避免除零
                if (Speed <= FP.Zero)
                    return FP.Zero;

                // 相对速度比 = (基础移速 - 参考速度) / 基础移速
                var relativeRatio = (Speed - ReferenceSpeed) / Speed;
                // 最终比例 = max(1 + 加速% * 相对比, 1 + (最小修正-1) * 相对比)
                var ratio = TSMath.Max(
                    FP.One + SpeedPct * relativeRatio,
                    FP.One + (MinSpeedPct - FP.One) * relativeRatio
                );
                return Speed * ratio;
            }
        }
        
        public Move(FP speed)
        {
            Speed = speed;
            Direction = TSVector3.zero;
            SpeedPct = FP.Zero;
            MinSpeedPct = FP.One / 5;
            ReferenceSpeed = FP.Zero;
        }
        
        public Move(FP speed, FP minSpeedPct)
        {
            Speed = speed;
            Direction = TSVector3.zero;
            SpeedPct = FP.Zero;
            MinSpeedPct = minSpeedPct;
            ReferenceSpeed = FP.Zero;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMoving()
        {
            return Speed > FP.Zero && Direction != TSVector3.zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDirection(TSVector3 direction)
        {
            direction.Normalize();
            Direction = direction;
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stop()
        {
            Direction = TSVector3.zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetSpeed(FP speed)
        {
            Speed = speed;
        }
    }
}