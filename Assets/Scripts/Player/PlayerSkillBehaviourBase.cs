using Config;
using Data;
using Skill;
using Skill.Behaviour;

namespace RayPlayer
{
    public abstract class PlayerSkillBehaviourBase : SkillBehaviourBase
    {
        protected PlayerController player;
        public override void Init(ICharacter owner, SkillConfig skillConfig, SkillBrainBase skillBrain, SkillPlayer skillPlayer,
            SkillLearnedData skillLearnedData, int skillIndex)
        {
            base.Init(owner, skillConfig, skillBrain, skillPlayer, skillLearnedData, skillIndex);
            player = owner as PlayerController;
        }
    }
}
