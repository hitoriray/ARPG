using System.Runtime.CompilerServices;
using Battle.ECS.Core.Interfaces;
using FixMath;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 缩放组件
    /// 记录模型原始Size、平面阴影原始Size、当前模型缩放比
    /// </summary>
    public struct ScaleComp : IInterpolatable
    {
        public FP ModelInitInitSize { get; private set; } // 模型原始Size
        public FP ShadowInitSize { get; private set; } // 平面阴影原始Size, 为0 表示无阴影

        public bool IsDirty { get; private set; }
        public TSVector3 Scale { get; private set; } // 非均匀缩放比

        public FP ShadowScale { get; private set; } // 平面阴影最终缩放比

        public TSVector3 ModelScale { get; private set; } // 模型最终缩放比

#region 构造函数
        /// <summary>
        /// 默认缩放为1无平面阴影
        /// </summary>
        public ScaleComp(FP modelInitInitSize)
        {
            IsDirty = true;
            ModelInitInitSize = modelInitInitSize;
            Scale = TSVector3.one;
            ModelScale = modelInitInitSize * Scale;
            ShadowInitSize = FP.Zero;
            ShadowScale = FP.Zero;
        }

        /// <summary>
        /// 无平面阴影
        /// </summary>
        public ScaleComp(FP modelInitInitSize, TSVector3 scale)
        {
            IsDirty = true;
            ModelInitInitSize = modelInitInitSize;
            Scale = scale;
            ModelScale = modelInitInitSize * scale;
            ShadowInitSize = FP.Zero;
            ShadowScale = FP.Zero;
        }

        /// <summary>
        /// 均匀缩放，有平面阴影
        /// </summary>
        public ScaleComp(FP modelInitScale, FP scale, FP shadowInitSize)
        {
            IsDirty = true;
            ModelInitInitSize = modelInitScale;
            Scale = TSVector3.one * scale;
            ModelScale = modelInitScale * Scale;
            ShadowInitSize = shadowInitSize;
            ShadowScale = shadowInitSize * scale;
        }
  #endregion

        /// <summary>
        /// 均匀缩放
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetScale(FP scale)
        {
            Scale = TSVector3.one * scale;
            ModelScale = ModelInitInitSize * Scale;
            ShadowScale = ShadowInitSize * scale;
            IsDirty = true;
        }

        /// <summary>
        /// 非均匀缩放
        /// </summary>
        public void SetNonUniformScale(TSVector3 scale)
        {
            Scale = scale;
            ModelScale = ModelInitInitSize * Scale;
            ShadowScale = ShadowInitSize * scale.x;
            IsDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetShadowInitScale(FP scale)
        {
            IsDirty = true;
            ShadowInitSize = scale;
            ShadowScale = ShadowInitSize * scale;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetModelInitScale(FP scale)
        {
            IsDirty = true;
            ModelInitInitSize = scale;
            ModelScale = ModelInitInitSize * Scale;
        }

        public void Reset()
        {
            IsDirty = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return $"ScaleComp: \nModelInitInitSize:{ModelInitInitSize}\nScale:{Scale}\nShadowInitSize:{ShadowInitSize}\nShadowScale:{ShadowScale}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetInterpolatableState()
        {
            IsDirty = false;
        }
    }
}