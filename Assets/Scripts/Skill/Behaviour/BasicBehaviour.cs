namespace Skill.Behaviour
{
    public class BasicBehaviour : SkillBehaviourBase
    {
        public override SkillBehaviourBase DeepClone()
        {
            return new BasicBehaviour();
        }
    }
}