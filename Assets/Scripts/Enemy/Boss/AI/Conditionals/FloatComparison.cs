using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace Enemy.Boss.AI.Conditionals
{
    [TaskCategory("Boss")]
    [TaskDescription("比较两个浮点数 (支持大于、小于、等于等操作)。")]
    public class FloatComparison : Conditional
    {
        public enum Operation
        {
            LessThan,
            LessThanOrEqualTo,
            EqualTo,
            NotEqualTo,
            GreaterThanOrEqualTo,
            GreaterThan
        }

        [Tooltip("参与比较的第一个数值 (例如距离)")]
        public SharedFloat float1;

        [Tooltip("比较操作符")]
        public Operation operation;

        [Tooltip("参与比较的第二个数值 (例如 3.0)")]
        public SharedFloat float2;

        public override TaskStatus OnUpdate()
        {
            switch (operation)
            {
                case Operation.LessThan:
                    return float1.Value < float2.Value ? TaskStatus.Success : TaskStatus.Failure;
                case Operation.LessThanOrEqualTo:
                    return float1.Value <= float2.Value ? TaskStatus.Success : TaskStatus.Failure;
                case Operation.EqualTo:
                    return UnityEngine.Mathf.Approximately(float1.Value, float2.Value) ? TaskStatus.Success : TaskStatus.Failure;
                case Operation.NotEqualTo:
                    return !UnityEngine.Mathf.Approximately(float1.Value, float2.Value) ? TaskStatus.Success : TaskStatus.Failure;
                case Operation.GreaterThanOrEqualTo:
                    return float1.Value >= float2.Value ? TaskStatus.Success : TaskStatus.Failure;
                case Operation.GreaterThan:
                    return float1.Value > float2.Value ? TaskStatus.Success : TaskStatus.Failure;
            }

            return TaskStatus.Failure;
        }

        public override void OnReset()
        {
            operation = Operation.LessThan;
            float1 = 0;
            float2 = 0;
        }
    }
}
