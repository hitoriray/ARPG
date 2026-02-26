using System;
using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 4方向受伤动画配置
/// </summary>
[Serializable]
public class HurtData
{
    [LabelText("受伤-前方"), InspectorName("受伤-前方")]
    public ClipTransition hurtFront;

    [LabelText("受伤-后方"), InspectorName("受伤-后方")]
    public ClipTransition hurtBack;

    [LabelText("受伤-左方"), InspectorName("受伤-左方")]
    public ClipTransition hurtLeft;

    [LabelText("受伤-右方"), InspectorName("受伤-右方")]
    public ClipTransition hurtRight;

    [LabelText("硬直恢复时间"), Range(0.1f, 2f)]
    public float recoveryDuration = 0.5f;

    /// <summary>
    /// 根据受击方向（本地空间）获取对应的 ClipTransition
    /// </summary>
    /// <param name="localHitDir">受击方向在角色本地空间的向量</param>
    public ClipTransition GetClipByDirection(Vector3 localHitDir)
    {
        float angle = Mathf.Atan2(localHitDir.x, localHitDir.z) * Mathf.Rad2Deg;

        // angle 范围: -180 ~ 180
        // 正前方 = 0°, 正后方 = ±180°
        // 左方 = -90°, 右方 = 90°
        if (angle >= -45f && angle < 45f)
            return hurtFront ?? hurtFront;
        if (angle >= 45f && angle < 135f)
            return hurtRight ?? hurtFront;
        if (angle >= -135f && angle < -45f)
            return hurtLeft ?? hurtFront;

        // 剩余区间: angle >= 135 || angle < -135 → 后方
        return hurtBack ?? hurtFront;
    }
}
