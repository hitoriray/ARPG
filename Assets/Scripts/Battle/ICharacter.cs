using Config;

public interface ICharacter : IHitTarget
{
    public float GetAttackValue(SkillAttackDetectionEvent detectionEvent);
}