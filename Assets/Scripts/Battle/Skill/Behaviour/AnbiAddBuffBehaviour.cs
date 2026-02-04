using Config;
using RayPlayerState;
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
            deltaPos.y += Time.deltaTime * -9.8f;
            owner.OnSkillMove(deltaPos);
            owner.OnSkillRotate(deltaRot);
        }
    }
}