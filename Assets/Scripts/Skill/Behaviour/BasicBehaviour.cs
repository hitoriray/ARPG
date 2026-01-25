using UnityEngine;
using Player.State;

namespace Skill.Behaviour
{
    public class BasicBehaviour : SkillBehaviourBase
    {
        private int attackIndex = -1;  // 当前的普攻段数索引
        public override SkillBehaviourBase DeepClone()
        {
            return new BasicBehaviour();
        }

        public override void Release()
        {
            base.Release();
            attackIndex += 1;
            if (attackIndex >= skillConfig.Clips.Length)
                attackIndex = 0;
            skillPlayer.StartPlaySkillConfig(this);
            skillPlayer.PlaySkillClip(skillConfig.Clips[attackIndex]);
        }
        
        public override void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            player.CharacterController.Move(deltaPos);
            player.ModelTransform.rotation *= deltaRot;
        }
        
        public override void OnSkillClipEnd()
        {
            attackIndex = -1;
            player.ChangeState(PlayerState.Idle);
        }

        public override void OnReleaseNew()
        {
            attackIndex = -1;
        }
    }
}