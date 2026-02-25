using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace BT.Actions
{
    /// <summary>
    /// 距离评分（区间内三角峰值），用于Utility Selector
    /// </summary>
    [TaskCategory("Common")]
    public class UtilityScoreByDistanceRange : BehaviorDesigner.Runtime.Tasks.Action
    {
        public SharedTransform Target;
        public SharedFloat MinDistance;   // >=0
        public SharedFloat MaxDistance;   // > MinDistance
        public SharedFloat PeakDistance;  // <=0 使用区间中点
        public SharedFloat PeakScore;     // 评分上限
        public SharedFloat OutsideScore;  // 区间外评分
        public SharedFloat OutScore;

        public override TaskStatus OnUpdate()
        {
            float score = 0f;
            if (Target.Value != null)
            {
                float dist = Vector3.Distance(transform.position, Target.Value.position);
                float min = Mathf.Max(0f, MinDistance.Value);
                float max = MaxDistance.Value;
                if (max <= min)
                    max = min + 0.01f;

                if (dist < min || dist > max)
                {
                    score = OutsideScore.Value;
                }
                else
                {
                    float peak = PeakDistance.Value;
                    if (peak <= 0f)
                        peak = (min + max) * 0.5f;
                    peak = Mathf.Clamp(peak, min, max);

                    float t = (dist - min) / (max - min);
                    float peakT = (peak - min) / (max - min);
                    float denom = Mathf.Max(peakT, 1f - peakT);
                    float tri = denom > 0f ? 1f - Mathf.Abs(t - peakT) / denom : 0f;
                    tri = Mathf.Clamp01(tri);

                    float peakScore = PeakScore.Value <= 0f ? 1f : PeakScore.Value;
                    score = tri * peakScore;
                }
            }
            else
            {
                score = OutsideScore.Value;
            }

            OutScore.Value = score;
            return TaskStatus.Success;
        }
    }
}
