using Player.State;
using UnityEngine;

namespace Skill.Behaviour
{
    public class AnbiSkill2Behaviour : SkillBehaviourBase
    {
        public override SkillBehaviourBase DeepClone()
        {
            return new AnbiSkill2Behaviour();
        }
        
        public override void Release(bool calcCdTime = true)
        {
            base.Release(calcCdTime);
            skillPlayer.StartPlaySkillConfig(this);
            skillPlayer.PlaySkillClip(skillConfig.Clips[0]);
            skillBrain.AddOrUpdateShareData(AnbiSkillBrain.PerfectAttackClip1, true);
        }
        
        public override void OnSkillClipEnd()
        {
            skillBrain.AddOrUpdateShareData(AnbiSkillBrain.PerfectAttackClip1, false);
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