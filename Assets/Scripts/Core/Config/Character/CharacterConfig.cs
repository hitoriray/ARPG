using System;
using System.Collections.Generic;
using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "CharacterConfig", menuName = "Config/Character/Character Config")]
    public class CharacterConfig : ConfigBase
    {
        [LabelText("走路速度")] public float WalkSpeed;
        [LabelText("奔跑速度")] public float RunSpeed;
        [LabelText("走路到奔跑的过渡速度")] public float Walk2RunTransitionSpeed;
        [LabelText("旋转速度")] public float RotateSpeed;
        [LabelText("移动起步转向速度")] public float MoveStartTurnSpeed = 1.8f;
        [LabelText("移动循环转向速度")] public float MoveLoopTurnSpeed = 1.4f;
        [LabelText("锁定朝向转向速度")] public float LockTurnSpeed = 5f;
        [LabelText("为移动应用RootMotion")] public bool ApplyRootMotionForMove;
        [LabelText("碰撞体配置")] public CharacterControllerProfile ControllerProfile;
        [LabelText("角色Avatar")] public Avatar Avatar;
        
        [LabelText("PlayerSO")] public PlayerSO PlayerSO;
        [LabelText("Generic移动配置")] public GenericLocomotionConfig GenericLocomotionConfig;
        [LabelText("脚步声音资源")] public AudioClip[] FootStepAudioClips;
        [LabelText("全部技能")] public List<SkillConfig> SkillConfigList;
        [LabelText("基础生命值")] public float hpBaseValue;
        [LabelText("基础魔力值")] public float mpBaseValue;
        [LabelText("基础攻击力")] public float attackBaseValue;
        [LabelText("基础防御力")] public float defenseBaseValue;
        
        [LabelText("等级成长配置")] public LevelGrowthConfig LevelGrowthConfig;
    }
    
    [Serializable]
    public struct CharacterControllerProfile
    {
        [LabelText("胶囊高度")] public float height;
        [LabelText("胶囊半径")] public float radius;
        [LabelText("胶囊中心")] public Vector3 center;
        [LabelText("台阶高度")] public float stepOffset;
        [LabelText("Skin Width")] public float skinWidth;
        [LabelText("地面检测半径")] public float groundRadius;
        [LabelText("地面检测偏移")] public float groundDetectedOffset;
    }
}
