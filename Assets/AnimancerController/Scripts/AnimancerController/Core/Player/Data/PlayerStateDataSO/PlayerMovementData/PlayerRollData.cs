using Animancer;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Roll 翻滚配置（Q 键，Walk/Run 均可触发）
/// 大幅身体位移，4方向，最长无敌帧，是紧急躲避大招的主要选择
/// </summary>
[System.Serializable]
public class PlayerRollData
{
    [Header("动画配置")]
    public ClipTransition rollForward;
    public ClipTransition rollBackward;
    public ClipTransition rollLeft;
    public ClipTransition rollRight;

    [Header("无敌帧")]
    [Tooltip("无敌帧时长（秒），Roll 通常最长")]
    [field: SerializeField, LabelText("无敌帧时长"), InspectorName("无敌帧时长"), Range(0f, 1f)]
    public float invincibleDuration { get; private set; } = 0.4f;

    [Header("方向判定阈值（锁敌状态下使用）")]
    [field: SerializeField, LabelText("方向判定阈值（锁敌状态下使用）"), InspectorName("方向判定阈值（锁敌状态下使用）"), Range(0.1f, 0.9f)]
    public float forwardThreshold { get; private set; } = 0.3f;

    [field: SerializeField, LabelText("侧向判定阈值（锁敌状态下使用）"), InspectorName("侧向判定阈值（锁敌状态下使用）"), Range(0.1f, 0.9f)]
    public float sideThreshold { get; private set; } = 0.5f;

    [Header("冷却")]
    [field: SerializeField, LabelText("冷却时间"), InspectorName("冷却时间"), Range(0.1f, 1.5f)]
    public float cooldown { get; private set; } = 0.5f;
}
