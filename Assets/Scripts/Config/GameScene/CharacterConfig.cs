using System.Collections.Generic;
using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "CharacterConfig", menuName = "Config/Game Scene/Character Config")]
    public class CharacterConfig : ConfigBase
    {
        [LabelText("走路速度")] public float WalkSpeed;
        [LabelText("奔跑速度")] public float RunSpeed;
        [LabelText("走路到奔跑的过渡速度")] public float Walk2RunTransitionSpeed;
        [LabelText("旋转速度")] public float RotateSpeed;
        [LabelText("为移动应用RootMotion")] public bool ApplyRootMotionForMove;
        [LabelText("标准动画配置")] public Dictionary<string, AnimationClip> StandAnimationDict;
        [LabelText("脚步声音资源")] public AudioClip[] FootStepAudioClips;
        [LabelText("全部技能")] public List<SkillConfig> SkillConfigList;
        [LabelText("基础生命值")] public float hpBaseValue;
        [LabelText("基础魔力值")] public float mpBaseValue;
        [LabelText("基础攻击力")] public float attackBaseValue;

        public AnimationClip GetAnimationClipByName(string clipName)
        {
            return StandAnimationDict.GetValueOrDefault(clipName);
        }
    }
}