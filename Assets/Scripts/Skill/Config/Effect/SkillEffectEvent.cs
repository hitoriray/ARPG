using Sirenix.OdinInspector;
using UnityEngine;

namespace Config
{
    public class SkillEffectEvent
    {
#if UNITY_EDITOR
        [LabelText("轨道名称")] public string TrackName = "特效轨道";
#endif
        [LabelText("起始帧")] public int FrameIndex = -1;
        [LabelText("特效预制体")] public GameObject Prefab;
        [LabelText("位置")] public Vector3 Position;
        [LabelText("旋转")] public Vector3 Rotation;
        [LabelText("缩放")] public Vector3 Scale;
        [LabelText("持续帧数")] public int Duration;
        [LabelText("自动销毁")] public bool AutoDestroy;
    }
}