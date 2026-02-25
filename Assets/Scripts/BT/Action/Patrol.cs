using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace BT.Actions
{
    [TaskCategory("Enemy/Boss")]
    public class Patrol : BossActionBase
    {
        public SharedTransformList Waypoints;
        public SharedFloat StopDistance;
        public SharedFloat MoveSpeedMultiplier;
        public SharedFloat MoveSpeedParam;
        public SharedBool PingPong;

        private int index;
        private int direction = 1;

        public override void OnStart()
        {
            base.OnStart();
            if (index < 0)
                index = 0;
        }

        public override TaskStatus OnUpdate()
        {
            if (!EnsureBoss())
                return TaskStatus.Failure;

            if (Waypoints.Value == null || Waypoints.Value.Count == 0)
                return TaskStatus.Failure;

            if (index >= Waypoints.Value.Count)
                index = 0;

            Transform wp = Waypoints.Value[index];
            if (wp == null)
                return TaskStatus.Failure;

            Vector3 toTarget = wp.position - boss.transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            float stop = StopDistance.Value > 0f ? StopDistance.Value : 1f;
            if (dist <= stop)
            {
                AdvanceIndex();
                return TaskStatus.Success;
            }

            float speedMult = MoveSpeedMultiplier.Value > 0f ? MoveSpeedMultiplier.Value : 1f;
            float speedParam = MoveSpeedParam.Value > 0f ? MoveSpeedParam.Value : 1f;
            boss.SetDesiredMove(toTarget.normalized, speedMult, speedParam);
            return TaskStatus.Running;
        }

        private void AdvanceIndex()
        {
            if (PingPong.Value && Waypoints.Value.Count > 1)
            {
                if (index == Waypoints.Value.Count - 1)
                    direction = -1;
                else if (index == 0)
                    direction = 1;

                index += direction;
            }
            else
            {
                index = (index + 1) % Waypoints.Value.Count;
            }
        }

        public override void OnEnd()
        {
            boss?.ClearDesiredMove();
        }
    }
}
