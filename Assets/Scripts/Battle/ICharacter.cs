using Config;
using Player.Animation;
using UnityEngine;

public interface ICharacter : IHitTarget
{
    public float GetAttackValue(SkillAttackDetectionEvent detectionEvent);

    public void OnSkillRotate();
    public void AddBuff(BuffConfig buffConfig, int stack);
    public void Change2IdleState();
    public void OnSkillMove(Vector3 deltaPos);
    public void OnSkillRotate(Quaternion deltaRot);
    public AnimationController AnimationController { get; }
    public Transform ModelTransform { get; }
}