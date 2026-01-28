using Sirenix.OdinInspector;
using UnityEngine;

namespace Config
{
    public class SkillAudioEvent
    {
#if UNITY_EDITOR
        [LabelText("轨道名称")] public string TrackName = "音效轨道";
#endif
        [LabelText("起始帧")] public int FrameIndex = -1;
        [LabelText("音效资源")] public AudioClip AudioClip;
        [LabelText("音量")] public float Volume;
    }
}