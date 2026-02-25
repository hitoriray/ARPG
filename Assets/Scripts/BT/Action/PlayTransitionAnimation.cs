using Animancer;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace BT.Actions
{
    [TaskCategory("Enemy/Boss")]
    public class PlayTransitionAnimation : BossActionBase
    {
        public SharedObject AnimationClip; // AnimationClip
        public SharedFloat FadeTime;
        public SharedBool WaitForEnd;

        private AnimancerState state;

        public override void OnStart()
        {
            base.OnStart();
            state = null;

            if (!EnsureBoss())
                return;

            var clip = AnimationClip.Value as AnimationClip;
            if (clip == null)
                return;

            float fade = FadeTime.Value;
            state = boss.Animancer.Play(clip, Mathf.Max(0f, fade));
        }

        public override TaskStatus OnUpdate()
        {
            if (!EnsureBoss())
                return TaskStatus.Failure;

            var clip = AnimationClip.Value as AnimationClip;
            if (clip == null)
                return TaskStatus.Failure;

            if (!WaitForEnd.Value)
                return TaskStatus.Success;

            if (state == null)
                return TaskStatus.Failure;

            return state.NormalizedTime < 1f ? TaskStatus.Running : TaskStatus.Success;
        }
    }
}
