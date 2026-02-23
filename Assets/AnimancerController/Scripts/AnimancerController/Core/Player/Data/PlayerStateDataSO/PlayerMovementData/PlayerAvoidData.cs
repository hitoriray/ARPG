using Animancer;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Avoid 闪身配置（Walk 状态 + Shift）
/// 小幅身体回避，4方向，短无敌帧
/// </summary>
[System.Serializable]
public class PlayerAvoidData
{
    [Header("动画配置")]
    public ClipTransition avoidForward;
    public ClipTransition avoidBackward;
    public ClipTransition avoidLeft;
    public ClipTransition avoidRight;

    [Header("无敌帧")]
    [Tooltip("无敌帧时长（秒）")]
    [field: SerializeField, LabelText("无敌帧时长"), InspectorName("无敌帧时长"), Range(0f, 0.5f)]
    public float invincibleDuration { get; private set; } = 0.2f;

    [Header("方向判定阈值（锁敌状态下使用）")]
    [field: SerializeField, LabelText("方向判定阈值（锁敌状态下使用）"), InspectorName("方向判定阈值（锁敌状态下使用）"), Range(0.1f, 0.9f)]
    public float forwardThreshold { get; private set; } = 0.3f;

    [field: SerializeField, LabelText("侧向判定阈值（锁敌状态下使用）"), InspectorName("侧向判定阈值（锁敌状态下使用）"), Range(0.1f, 0.9f)]
    public float sideThreshold { get; private set; } = 0.5f;

    [Header("冷却")]
    [field: SerializeField, LabelText("冷却时间"), InspectorName("冷却时间"), Range(0.1f, 1f)]
    public float cooldown { get; private set; } = 0.3f;
}
