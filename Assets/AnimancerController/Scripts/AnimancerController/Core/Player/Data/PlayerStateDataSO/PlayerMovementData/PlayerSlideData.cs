using Animancer;
using UnityEngine;

/// <summary>
/// Slide 滑步配置（Run 状态 + Shift）
/// 大位移穿插，4方向，无无敌帧（进攻性动作）
/// </summary>
[System.Serializable]
public class PlayerSlideData
{
    [Header("动画配置")]
    public ClipTransition slideForward;
    public ClipTransition slideBackward;
    public ClipTransition slideLeft;
    public ClipTransition slideRight;

    [Header("无敌帧")]
    [Tooltip("无敌帧时长（秒），Slide 通常为 0（进攻性动作）")]
    [field: SerializeField, Range(0f, 0.5f)]
    public float invincibleDuration { get; private set; } = 0f;

    [Header("方向判定阈值（锁敌状态下使用）")]
    [field: SerializeField, Range(0.1f, 0.9f)]
    public float forwardThreshold { get; private set; } = 0.3f;

    [field: SerializeField, Range(0.1f, 0.9f)]
    public float sideThreshold { get; private set; } = 0.5f;

    [Header("冷却")]
    [field: SerializeField, Range(0.1f, 1f)]
    public float cooldown { get; private set; } = 0.4f;
}
