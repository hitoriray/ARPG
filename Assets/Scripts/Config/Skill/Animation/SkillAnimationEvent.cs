using UnityEngine;

namespace Config
{
    /// <summary>
    /// 动画帧事件
    /// </summary>
    public class SkillAnimationEvent : SkillFrameEventBase
    {
        public AnimationClip AnimationClip;
        public bool ApplyRootMotion;
        public float TransitionTime = 0.25f;
        
#if UNITY_EDITOR
        public int DurationFrame;
#endif
    }
}