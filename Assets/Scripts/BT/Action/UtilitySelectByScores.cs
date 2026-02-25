using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace BT.Actions
{
    [TaskCategory("Common")]
    public class UtilitySelectByScores : BehaviorDesigner.Runtime.Tasks.Action
    {
        public SharedFloat Score0;
        public SharedFloat Score1;
        public SharedFloat Score2;
        public SharedFloat Score3;

        public SharedInt SelectedIndex;
        public SharedBool RandomizeOnTie;
        public SharedFloat RecalcInterval;
        public SharedFloat NextRecalcTime;

        public override TaskStatus OnUpdate()
        {
            float interval = RecalcInterval.Value;
            if (interval > 0f && Time.time < NextRecalcTime.Value)
                return TaskStatus.Success;

            float s0 = Score0.Value;
            float s1 = Score1.Value;
            float s2 = Score2.Value;
            float s3 = Score3.Value;

            float max = s0;
            int idx = 0;
            if (s1 > max) { max = s1; idx = 1; }
            if (s2 > max) { max = s2; idx = 2; }
            if (s3 > max) { max = s3; idx = 3; }

            if (RandomizeOnTie.Value)
            {
                const float eps = 0.001f;
                int count = 0;
                int pick = -1;
                if (Mathf.Abs(s0 - max) <= eps) { count++; if (Random.value < 1f / count) pick = 0; }
                if (Mathf.Abs(s1 - max) <= eps) { count++; if (Random.value < 1f / count) pick = 1; }
                if (Mathf.Abs(s2 - max) <= eps) { count++; if (Random.value < 1f / count) pick = 2; }
                if (Mathf.Abs(s3 - max) <= eps) { count++; if (Random.value < 1f / count) pick = 3; }
                if (pick >= 0)
                    idx = pick;
            }

            SelectedIndex.Value = idx;

            if (interval > 0f)
                NextRecalcTime.Value = Time.time + interval;

            return TaskStatus.Success;
        }
    }
}
