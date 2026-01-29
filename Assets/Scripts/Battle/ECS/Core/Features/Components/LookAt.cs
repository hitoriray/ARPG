using Arch.Core;
using FixMath;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 用于使实体始终朝向目标
    /// </summary>
    public struct LookAt
    {
        public readonly Entity Target;
        public TSVector3 TargetPos;

        public LookAt(Entity target, TSVector3 targetPos)
        {
            Target = target;
            TargetPos = targetPos;
        }
    }
}