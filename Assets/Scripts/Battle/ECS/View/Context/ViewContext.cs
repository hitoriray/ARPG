using Arch.Core;
using Arch.Extend.EntityIndex;
using Arch.Extend.System;
using Battle.ECS.Component;
using Battle.ECS.Core.Interfaces;

namespace Battle.ECS.View.Context
{
    /// <summary>
    /// 表现层上下文，存储表现世界的数据和对象
    /// </summary>
    public class ViewContext : IContext
    {
        public World World { get; private set; }

        // private ModelShadowScaleScriptableObject _modelShadowScaleConfig; // 模型阴影缩放SO
        // TODO: 对象池一律用JKFrame
        // public MixedGameObjectPool GameObjectPool { get; private set; }
        // public MixedScriptObjectPool<VisualRootEntityConfig> V2EMixedScriptObjectPool { get; private set; }

        public SingleEntityIndex<CameraComp> CameraIndex { get; private set; } // 相机唯一实体
        // private CameraShakeList _cameraShakeListData; // 相机效果SO

        public ViewContext()
        {
            // GameObjectPool = new MixedGameObjectPool();
            // V2EMixedScriptObjectPool = new MixedScriptObjectPool<VisualRootEntityConfig>();
            World = World.Create();
            // CameraIndex = new SingleEntityIndex<CameraCom>(World);
        }

        public void Dispose()
        {
            CameraIndex = null;
            // GameObjectPool?.Dispose();
            // V2EMixedScriptObjectPool?.Dispose();
            World.DestroyAll();
            World.Dispose();
            World = null;
        }

        // public void SetModelShadowScaleConfig(ModelShadowScaleScriptableObject modelShadowScaleConfig)
        // {
        //     _modelShadowScaleConfig = modelShadowScaleConfig;
        // }

//         public float GetModelShadowScale(string assetName)
//         {
// #if XY_DEV
//             if (_modelShadowScaleConfig.IsNullPtr())
//             {
//                 // Logger.Error($"ClientContext: GetModelShadowScale: _modelShadowScaleConfig is null");
//                 return 1f;
//             }
// #endif
//             if (_modelShadowScaleConfig)
//                 return _modelShadowScaleConfig.GetValue(assetName);
//             return 1f;
//         }

        // public void SetCameraShakeConfig(CameraShakeList cameraShakeList)
        // {
        //     _cameraShakeListData = cameraShakeList;
        // }
        //
        // public CameraShake GetCameraShakeConfig(string key)
        // {
        //     if (_cameraShakeListData)
        //         return _cameraShakeListData.GetValue(key);
        //     return null;
        // }
    }
}