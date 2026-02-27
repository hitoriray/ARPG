using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class PlayerMovementData
{
    [field: SerializeField, LabelText("待机数据"), InspectorName("待机数据")]
    public PlayerIdleData PlayerIdleData { get; private set; }

    [field: SerializeField, LabelText("起步数据"), InspectorName("起步数据")]
    public PlayerMoveStartData PlayerMoveStartData { get; private set; }

    [field: SerializeField, LabelText("移动循环数据"), InspectorName("移动循环数据")]
    public PlayerMoveLoopData PlayerMoveLoopData { get; private set; }

    [field: SerializeField, LabelText("移动结束数据"), InspectorName("移动结束数据")]
    public PlayerMoveEndData PlayerMoveEndData { get; private set; }

    [field: SerializeField, LabelText("攀爬数据"), InspectorName("攀爬数据")]
    public PlayerClimbData PlayerClimbData { get; private set; }

    [field: SerializeField, LabelText("挂墙数据"), InspectorName("挂墙数据")]
    public PlayerHangWallData PlayerHangWallData { get; set; }

    [field: SerializeField, LabelText("跳跃/下落/落地数据"), InspectorName("跳跃/下落/落地数据")]
    public PlayerJumpFallAndLandData PlayerJumpFallAndLandData { get; private set; }

    // 闪避配置（Walk+Shift / Run+Shift / Q键）
    [field: SerializeField, LabelText("闪避配置"), InspectorName("闪避配置")]
    public PlayerAvoidData PlayerAvoidData { get; private set; }

    [field: SerializeField, LabelText("滑铲配置"), InspectorName("滑铲配置")]
    public PlayerSlideData PlayerSlideData { get; private set; }

    [field: SerializeField, LabelText("翻滚配置"), InspectorName("翻滚配置")]
    public PlayerRollData PlayerRollData { get; private set; }

    [field: SerializeField, LabelText("受伤配置"), InspectorName("受伤配置")]
    public HurtData PlayerHurtData { get; private set; }

    [field: SerializeField, LabelText("死亡动画"), InspectorName("死亡动画")]
    public Animancer.ClipTransition DeathClip { get; private set; }

    [field: SerializeField, LabelText("软转向搜索范围"), InspectorName("软转向搜索范围")]
    public float SoftLockRadius { get; private set; } = 6f;

    [field: SerializeField, LabelText("软转向速度(度/秒)"), InspectorName("软转向速度")]
    public float SoftLockRotateSpeed { get; private set; } = 720f;
}
