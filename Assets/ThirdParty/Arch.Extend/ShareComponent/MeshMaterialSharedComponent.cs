using System;

namespace Arch.Extend.SharedComponent
{
    /// <summary>
    /// 网格和材质的共享组件，实现ISharedComponent接口
    /// 用于将使用相同网格和材质的实体分组到同一Chunk
    /// </summary>
    public struct MeshMaterialSharedComponent : ISharedComponent<MeshMaterialSharedComponent>
    {
        public string MeshName;
        public string MaterialName;
    
        public bool Equals(MeshMaterialSharedComponent other)
        {
            return MeshName == other.MeshName && MaterialName == other.MaterialName;
        }

        // 重写GetHashCode以确保相同内容的哈希码一致
        public override int GetHashCode()
        {
            return HashCode.Combine(MeshName, MaterialName);
        }

        public bool Equals(ISharedComponent other)
        {
            return other is MeshMaterialSharedComponent otherShared && Equals(otherShared);
        }
    }
}