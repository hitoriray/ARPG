using Player.State;
using UnityEngine;

namespace Skill.Behaviour
{
    public class AnbiSkillBehaviour : SkillBehaviourBase
    {
        protected float cdTimer;
        public float cdTime = 5f;

        public override SkillBehaviourBase DeepClone()
        {
            return new AnbiSkillBehaviour() { cdTime = cdTime };
        }
        
        public override void Release()
        {
            base.Release();
            skillPlayer.StartPlaySkillConfig(this);
            skillPlayer.PlaySkillClip(skillConfig.Clips[0]);
            cdTimer = cdTime;
        }

        public override bool CheckRelease()
        {
            return cdTimer <= 0 && base.CheckRelease();
        }

        public override void OnUpdate()
        {
            cdTimer -= Time.deltaTime;
            if (cdTimer < 0)
            {
                cdTimer = 0;
            }
        }

        public override void OnSkillClipEnd()
        {
            player.ChangeState(PlayerState.Idle);
        }
    }
}