using Sirenix.OdinInspector;
using UnityEngine;

namespace Config
{
    /// <summary>
    /// 动画帧事件
    /// </summary>
    public class SkillAnimationEvent : SkillFrameEventBase
    {
        [LabelText("动画资源")] public AnimationClip AnimationClip;
        [LabelText("应用根运动")] public bool ApplyRootMotion;
        [LabelText("过渡时间")] public float TransitionTime = 0.25f;
        
#if UNITY_EDITOR
        [LabelText("持续帧数")] public int DurationFrame;
#endif
    }
}