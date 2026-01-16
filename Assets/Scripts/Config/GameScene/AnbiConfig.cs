using System.Collections.Generic;
using JKFrame;
using UnityEngine;

namespace Config.GameScene
{
    [CreateAssetMenu(fileName = "AnbiConfig", menuName = "Config/Game Scene/Anbi Config")]
    public class AnbiConfig : ConfigBase
    {
        public float WalkSpeed;
        public float RotateSpeed;
        public Dictionary<string, AnimationClip> StandAnimationDict;

        public AnimationClip GetAnimationClipByName(string clipName)
        {
            return StandAnimationDict.GetValueOrDefault(clipName);
        }
    }
}