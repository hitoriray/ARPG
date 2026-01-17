using System.Collections.Generic;
using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Config.GameScene
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

        public AnimationClip GetAnimationClipByName(string clipName)
        {
            return StandAnimationDict.GetValueOrDefault(clipName);
        }
    }
}