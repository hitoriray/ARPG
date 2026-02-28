using Skill.Behaviour;
using UnityEngine;

namespace RayPlayer
{
    public class Skill2Behaviour : PlayerSkillBehaviourBase
    {
        public override SkillBehaviourBase DeepClone()
        {
            return new Skill2Behaviour();
        }
        
        public override void Release(bool calcCdTime = true)
        {
            base.Release(calcCdTime);
            skillPlayer.StartPlaySkillBehaviour(this);
            skillPlayer.PlaySkillClip(skillConfig.Clips[0]);
            skillBrain.AddOrUpdateShareData(AnbiSkillBrain.PerfectAttackClip1, true);
        }
        
        public override void OnSkillClipEnd()
        {
            skillBrain.AddOrUpdateShareData(AnbiSkillBrain.PerfectAttackClip1, false);
            owner.Change2IdleState();
        }
        
        public override void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            owner.OnSkillMove(deltaPos);
            owner.OnSkillRotate(deltaRot);
        }
    }
}
