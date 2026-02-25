using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BT;
using UnityEngine;

namespace BT.Conditions
{
    [TaskCategory("Common")]
    public class CanHearObject : Conditional
    {
        public SharedTransform Target;          // 兼容旧用法（可当作声源）
        public SharedTransform Source;          // 建议使用的声源
        public SharedVector3 SourcePoint;       // 直接传声源位置
        public SharedBool UsePoint;             // true 时使用 SourcePoint
        public SharedFloat HearRange;
        public SharedString EventKey; // optional
        public SharedBool ConsumeEvent;

        public override TaskStatus OnUpdate()
        {
            bool hasEvent = false;
            string key = EventKey.Value;
            if (!string.IsNullOrEmpty(key))
                hasEvent = ConsumeEvent.Value ? BossAIEventBuffer.Consume(key) : BossAIEventBuffer.Peek(key);

            Vector3? sourcePos = null;
            if (UsePoint.Value)
            {
                sourcePos = SourcePoint.Value;
            }
            else if (Source.Value != null)
            {
                sourcePos = Source.Value.position;
            }
            else if (Target.Value != null)
            {
                sourcePos = Target.Value.position;
            }

            if (sourcePos == null)
                return hasEvent ? TaskStatus.Success : TaskStatus.Failure;

            float range = HearRange.Value;
            float dist = Vector3.Distance(transform.position, sourcePos.Value);

            if (range > 0f && dist > range)
                return TaskStatus.Failure;

            return hasEvent || string.IsNullOrEmpty(key) ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
