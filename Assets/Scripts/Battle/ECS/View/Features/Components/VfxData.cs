using FixMath;
using UnityEngine;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 特效生成数据组件（用于生成特效）
    /// </summary>
    public struct VfxData
    {
        public GameObject Prefab;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public bool AutoDestroy;
        public FP Duration;
        public bool LookAtCamera;
    }
}
