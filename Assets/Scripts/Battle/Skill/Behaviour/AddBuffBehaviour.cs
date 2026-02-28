using UnityEngine;

namespace Skill.Behaviour
{
    public class AddBuffBehaviour : SkillBehaviourBase
    {
        public override SkillBehaviourBase DeepClone()
        {
            return new AddBuffBehaviour();
        }
        
        public override void Release(bool calcCdTime = true)
        {
            base.Release(calcCdTime);
            skillPlayer.StartPlaySkillBehaviour(this);
            skillPlayer.PlaySkillClip(skillConfig.Clips[0]);
        }
        
        public override void OnSkillClipEnd()
        {
            base.OnSkillClipEnd();
            owner.Change2IdleState();
        }
        
        public override void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            owner.OnSkillMove(deltaPos);
            owner.OnSkillRotate(deltaRot);
        }
    }
}
