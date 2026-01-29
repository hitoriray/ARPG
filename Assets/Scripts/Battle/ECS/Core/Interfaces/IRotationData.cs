using FixMath;

namespace Battle.ECS.Core.Interfaces
{
    public interface IRotationData
    {
        bool IsDirty { get; }
        bool IsDirectly { get; }
        TSQuaternion Quaternion { get; }
    }
}