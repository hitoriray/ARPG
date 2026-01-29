using FixMath;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 速度组件，用于描述物体在世界中的运动速度
    /// </summary>
    public struct Velocity
    {
        public TSVector3 Value;
        
        public Velocity(TSVector3 value) => Value = value;
    }
}