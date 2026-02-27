using System;
using Animancer;
using Battle.ECS;
using Config;
using Manager;
using RayAnimation;
using UnityEngine;

public interface ICharacter : IHitTarget, IDeathCallback
{
    float GetAttackValue(SkillAttackDetectionEvent detectionEvent);

    void OnSkillRotate();
    void AddBuff(BuffConfig buffConfig, int stack);
    void CreateWeapon(int slotIndex, GameObject weaponPrefab);
    void DestroyWeapon(int slotIndex);
    void Change2IdleState();
    void OnSkillMove(Vector3 deltaPos);
    void OnSkillRotate(Quaternion deltaRot);
    
    AnimancerComponent Animancer { get; }
    AnimancerLayer SkillLayer { get; }
    Transform ModelTransform { get; }

    void EnterSkillMode(bool upperBody);
    void ExitSkillMode();
    void SetSkillRootMotion(Action<Vector3, Quaternion> handler, bool applyRootMotion);
    void ClearSkillRootMotion();
}