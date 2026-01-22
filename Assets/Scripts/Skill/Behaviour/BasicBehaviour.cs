using UnityEngine;
using Player.State;

namespace Skill.Behaviour
{
    public class BasicBehaviour : SkillBehaviourBase
    {
        public override SkillBehaviourBase DeepClone()
        {
            return new BasicBehaviour();
        }

        public override void Release()
        {
            base.Release();
            skillPlayer.StartPlaySkillConfig(this);
            skillPlayer.PlaySkillClip(skillConfig.Clips[0]);
        }

        /// <summary>
        /// TODO：目前只是拿到伤害检测的数据，没有实际的技能行为
        /// </summary>
        public override void OnAttackDetection(Collider collider)
        {
            Debug.Log(collider.gameObject.name);
        }

        public override void OnSkillClipEnd()
        {
            player.ChangeState(PlayerState.Idle);
        }

        public override void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            player.CharacterController.Move(deltaPos);
            player.ModelTransform.rotation *= deltaRot;
        }
    }
}