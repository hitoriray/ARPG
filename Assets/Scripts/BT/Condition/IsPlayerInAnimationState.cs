using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using RayPlayer;

namespace BT.Conditions
{
    [TaskCategory("Enemy/Boss")]
    public class IsPlayerInAnimationState : Conditional
    {
        public SharedTransform Target;
        public SharedString AnimationName;
        public SharedBool CheckBaseLayer;
        public SharedBool CheckSkillLayer;

        public override TaskStatus OnUpdate()
        {
            if (Target.Value == null)
                return TaskStatus.Failure;

            string name = AnimationName.Value;
            if (string.IsNullOrEmpty(name))
                return TaskStatus.Failure;

            var player = Target.Value.GetComponentInParent<PlayerController>();
            if (player == null || player.Animancer == null)
                return TaskStatus.Failure;

            if (CheckBaseLayer.Value)
            {
                if (IsLayerPlaying(player.Animancer.Layers[0], name))
                    return TaskStatus.Success;
            }

            if (CheckSkillLayer.Value)
            {
                if (IsLayerPlaying(player.SkillLayer, name))
                    return TaskStatus.Success;
            }

            return TaskStatus.Failure;
        }

        private static bool IsLayerPlaying(Animancer.AnimancerLayer layer, string clipName)
        {
            if (layer == null)
                return false;

            var state = layer.CurrentState;
            var clip = state != null ? state.Clip : null;
            return clip != null && clip.name == clipName;
        }
    }
}
