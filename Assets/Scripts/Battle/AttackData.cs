using Config;
using UnityEngine;

public struct AttackData
{
    public SkillAttackDetectionEvent detectionEvent;
    public ICharacter source;
    public Vector3 hitPoint;
    public float attackValue;
}