using Animancer;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum ObstructHeightLevel
{
    Low = 0,
    LowMedium = 1,
    Medium = 2,
    MediumHigh = 3,
    High = 4,
}

public enum ClimbType
{
    Vault,  // 翻越/跨栏
    Climb   // 攀爬
}

public enum MatchType
{
    Root,
    RootY,
}

public struct ClimbTargetMatchInfo
{
    public Vector3 TargetPos; //爬上去的目标位置
    public Vector3 InitPos; //开始进行目标位置匹配的初始位置
    public bool setTargetMatchInitPos; //是否完成最后的匹配操作

    public ClimbTargetMatchInfo(Vector3 targetPos)
    {
        this.TargetPos = targetPos;

        InitPos = Vector3.zero;
        setTargetMatchInitPos = false;
    }
}

/// <summary>
/// 可变数据复用类，缓存可读可写数据
/// </summary>
public class PlayerReusableData
{
    public float currentRotationTime;

    // Animancer控制混合树Mixer用到的参数
    public SmoothedFloatParameter standValueParameter { get; set; }
    public SmoothedFloatParameter rotationValueParameter { get; set; }
    public SmoothedFloatParameter speedValueParameter { get; set; }
    public SmoothedFloatParameter lockValueParameter { get; set; }
    public SmoothedFloatParameter lock_X_ValueParameter { get; set; }

    public SmoothedFloatParameter lock_Y_ValueParameter { get; set; }

    //锁敌
    public BindableProperty<Transform> lockTarget { get; set; } = new();

    public int drawTargetId = -1;
    public int drawCurrentId = -1;
    public Vector3 targetDir;
    public BindableProperty<float> targetAngle = new();
    public BindableProperty<string> currentState = new();

    //IdleState
    public ManualMixerState standIdleMixerState;
    public ManualMixerState crouchIdleMixerState;
    public List<AnimancerState> standIdleList = new();
    public List<AnimancerState> crouchIdleList = new();
    public int currentStandIdleIndex;
    public int currentCrouchIdleIndex;

    public bool isLockIdle = false;

    //攀爬
    public ObstructHeightLevel ObstructHeightLevel;
    public ClimbType ClimbType;

    public ClipTransition targetClimbClip;

    //跳跃
    public float horizontalSpeed;

    //跳跃惯性
    public Vector3 currentInertialVelocity;
    public int cashIndex = 0;
    public static readonly int cashSize = 3;
    public Vector3[] cashVelocity = new Vector3[cashSize];

    //HangWall
    public float originalCCRadius;
    public Vector3 vaultPos;

    public RaycastHit hit;

    //打断点检测事件
    public Action inputInterruptionCB { get; set; }

    //检测墙的距离
    public float checkWallDistance = 0.6f;

    //是否原地跳跃
    public bool isInPlaceJump;

    //外力跳跃
    public float jumpExternalForce = 15;

    //
    public float currentMidInAirMultiplier = 0.6f;

    // 移动模式控制（CapsLock 切换）
    public bool isRunMode = false; // false=行走(1)，true=跑步(2)

    // 无敌帧标记（预留接口，战斗系统可检测此字段）
    public bool isInvincible = false;

    // 所有闪避动作（Avoid/Slide/Roll）共享冷却时间戳
    public float lastEvasiveActionTime = -999f;

    // 受伤方向（世界空间，从攻击方向指向角色）
    public Vector3 lastHitDirection;

    public PlayerReusableData(AnimancerComponent animancerComponent, PlayerSO playerSO)
    {
        standValueParameter  = new SmoothedFloatParameter(animancerComponent, playerSO.playerParameterData.standValueParameter,0.15f);
        standValueParameter.Parameter.Value = 1;

        rotationValueParameter = new SmoothedFloatParameter(animancerComponent,playerSO.playerParameterData.rotationValueParameter,0.2f);
        speedValueParameter = new SmoothedFloatParameter(animancerComponent, playerSO.playerParameterData.speedValueParameter, 1f);
        lockValueParameter = new SmoothedFloatParameter(animancerComponent,playerSO.playerParameterData.LockValueParameter,0.1f);
        lock_X_ValueParameter = new SmoothedFloatParameter(animancerComponent, playerSO.playerParameterData.Lock_X_ValueParameter, 0.3f);
        lock_Y_ValueParameter = new SmoothedFloatParameter(animancerComponent, playerSO.playerParameterData.Lock_Y_ValueParameter, 0.3f);
    }
}