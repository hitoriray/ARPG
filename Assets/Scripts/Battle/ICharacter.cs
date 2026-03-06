using System;
using Animancer;
using Attribute;
using Battle.ECS;
using Config;
using UnityEngine;

public interface ICharacter : IHitTarget, IDeathCallback
{
    float GetAttackValue(SkillAttackDetectionEvent detectionEvent);
    void TryReleaseBasicAttack();
    void TryReleaseSkillBySkillIndex(int skillIndex);

    void OnSkillRotate();
    void AddBuff(BuffConfig buffConfig, int stack);
    void CreateWeapon(int slotIndex, GameObject weaponPrefab);
    void DestroyWeapon(int slotIndex);
    void Change2IdleState();
    void OnSkillMove(Vector3 deltaPos);
    void OnSkillRotate(Quaternion deltaRot);
    
    CharacterAttribute CharacterAttribute { get; }
    CharacterConfig CharacterConfig { get; }
    AnimancerComponent Animancer { get; }
    AnimancerLayer SkillLayer { get; }
    Transform ModelTransform { get; }

    void EnterSkillMode(bool upperBody);
    void ExitSkillMode();
    void SetSkillRootMotion(Action<Vector3, Quaternion> handler, bool applyRootMotion);
    void ClearSkillRootMotion();
    
    /// <summary> 是否为玩家操控角色（用于决策UI、技能槽等行为） </summary>
    bool IsPlayerControlled { get; }
}