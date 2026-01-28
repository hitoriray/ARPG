using Config;
using Player.State;
using UnityEngine;

namespace Skill.Behaviour
{
    public class AnbiAddBuffBehaviour : SkillBehaviourBase
    {
        public override SkillBehaviourBase DeepClone()
        {
            return new AnbiAddBuffBehaviour();
        }
        
        public override void Release(bool calcCdTime = true)
        {
            base.Release(calcCdTime);
            skillPlayer.StartPlaySkillConfig(this);
            skillPlayer.PlaySkillClip(skillConfig.Clips[0]);
        }
        
        public override void OnSkillClipEnd()
        {
            base.OnSkillClipEnd();
            player.ChangeState(PlayerState.Idle);
        }
        
        public override void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            deltaPos.y += Time.deltaTime * -9.8f;
            player.CharacterController.Move(deltaPos);
            player.ModelTransform.rotation *= deltaRot;
        }
    }
}