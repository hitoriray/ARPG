using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BT;

namespace BT.Conditions
{
    [TaskCategory("Common")]
    public class HasReceivedEvent : Conditional
    {
        public SharedString EventKey;
        public SharedBool Consume;

        public override TaskStatus OnUpdate()
        {
            string key = EventKey.Value;
            if (string.IsNullOrEmpty(key))
                return TaskStatus.Failure;

            bool ok = Consume.Value ? BossAIEventBuffer.Consume(key) : BossAIEventBuffer.Peek(key);
            return ok ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
