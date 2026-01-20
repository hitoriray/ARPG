using UnityEngine;

namespace Config
{
    public class SkillAudioEvent
    {
#if UNITY_EDITOR
        public string TrackName = "音效轨道";
#endif
        public int FrameIndex = -1;
        public int PlayCount;  // 决定播放次数
        public AudioClip AudioClip;
        public float Volume;
    }
}