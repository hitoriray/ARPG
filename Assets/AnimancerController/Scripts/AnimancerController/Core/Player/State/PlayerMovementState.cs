using Animancer;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementState : StateBase
{
    protected PlayerStateMachine playerStateMachine;
    protected PlayerSO playerSO;

    public PlayerMovementState(PlayerStateMachine stateMachine) : base(stateMachine.player)
    {
        playerStateMachine = stateMachine;
        playerSO = player.playerSO;
    }

    public override void OnEnter()
    {
        AddEventListening();
    }

    protected override void AddEventListening() // 注册的顺序也决定了优先级
    {
        inputServer.inputMap.Player.Lock.started += OnLock;
    }


    protected override void RemoveEventListening()
    {
        inputServer.inputMap.Player.Lock.started -= OnLock;
    }

    public override void OnExit()
    {
        RemoveEventListening();
    }

    public override void OnUpdate()
    {
        //处理索敌
        if (reusableData.lockValueParameter.TargetValue == 1)
        {
            UpdateLockRotation(5, null);
            //更新参数
            UpdateLockValue();
        }

        //处理打断委托
        reusableData.inputInterruptionCB?.Invoke();
    }

    private void UpdateLockValue()
    {
        reusableData.lock_X_ValueParameter.TargetValue = inputServer.Move.x * reusableData.speedValueParameter.TargetValue;
        reusableData.lock_Y_ValueParameter.TargetValue = inputServer.Move.y * reusableData.speedValueParameter.TargetValue;
    }

    public override void OnAnimationEnd()
    {
    }

    public override void OnAnimationUpdate()
    {
    }

    private void OnLock(InputAction.CallbackContext context)
    {
        reusableData.lockValueParameter.TargetValue = reusableData.lockValueParameter.TargetValue == 0 ? 1 : 0;
        if (reusableData.lockValueParameter.TargetValue == 1)
        {
            reusableData.lockTarget.Value = GetValidCameraTransform();
        }
        else
        {
            reusableData.lockTarget.Value = null;
        }
    }

    /// <summary>
    /// C 键：切换站立/蹲伏
    /// </summary>
    protected void OnCrouch(InputAction.CallbackContext context)
    {
        reusableData.standValueParameter.TargetValue = reusableData.standValueParameter.TargetValue == 0 ? 1 : 0;
    }

    /// <summary>
    /// Ctrl 键：切换行走/跑步模式
    /// </summary>
    protected void OnToggleRun(InputAction.CallbackContext context)
    {
        reusableData.isRunMode = !reusableData.isRunMode;
    }

    /// <summary>
    /// Shift 键：Walk 模式=Avoid 闪身，Run 模式=Slide 滑步
    /// </summary>
    protected void OnDodge(InputAction.CallbackContext context)
    {
        if (reusableData.isRunMode)
            playerStateMachine.ChangeState(playerStateMachine.slideState);
        else
            playerStateMachine.ChangeState(playerStateMachine.avoidState);
    }

    /// <summary>
    /// Q 键：翻滚（Walk/Run 均可，最长无敌帧）
    /// </summary>
    protected void OnRoll(InputAction.CallbackContext context)
    {
        playerStateMachine.ChangeState(playerStateMachine.rollState);
    }

    /// <summary>
    /// 更新速度参数（基于 isRunMode），同时根据 CharacterConfig 的目标速度
    /// 计算 moveSpeedMultiplier，让 RootMotion 缩放后的实际移速精确等于配置值。
    /// Walk 动画基准 RootMotion 速度 = 1.287 m/s
    /// Run  动画基准 RootMotion 速度 = 3.364 m/s
    /// Walk=1, Run=2
    /// </summary>
    protected float UpdateSpeed()
    {
        const float baseWalkSpeed = 1.287f;
        const float baseRunSpeed  = 3.364f;

        if (reusableData.isRunMode)
        {
            reusableData.speedValueParameter.TargetValue = 2f;
            if (player.RunSpeed > 0f)
                player.moveSpeedMultiplier = player.RunSpeed / baseRunSpeed;
        }
        else
        {
            reusableData.speedValueParameter.TargetValue = 1f;
            if (player.WalkSpeed > 0f)
                player.moveSpeedMultiplier = player.WalkSpeed / baseWalkSpeed;
        }

        return reusableData.speedValueParameter.TargetValue;
    }

    protected float UpdateRotation(bool isUpdateRotationParameter = true, float rotationSmoothTime = 0.7f,
        bool isRotationCompensation = true, float rotationSize = 1.4f)
    {
        float angle = GetTargetAngle();
        if (isUpdateRotationParameter)
        {
            reusableData.rotationValueParameter.SmoothTime = rotationSmoothTime;
            reusableData.rotationValueParameter.TargetValue = angle * Mathf.Deg2Rad;
        }

        if (inputServer.Move != Vector2.zero)
        {
            if (isRotationCompensation)
            {
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation,
                    Quaternion.LookRotation(reusableData.targetDir), Time.deltaTime * rotationSize);
            }

            return angle;
        }

        return 0;
    }

    protected void UpdateLockRotation(float rotationSize, Transform lockTarget = null)
    {
        var cameraTransform = GetValidCameraTransform();
        if (cameraTransform == null)
        {
            return;
        }

        if (lockTarget == null)
        {
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation,
                Quaternion.LookRotation(Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up)),
                Time.deltaTime * rotationSize);
        }
        else
        {
            Vector3 dir = (lockTarget.position - player.transform.position).normalized;
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation,
                Quaternion.LookRotation(Vector3.ProjectOnPlane(dir, Vector3.up)), Time.deltaTime * rotationSize);
        }
    }

    protected void UpdateLockRotation(float rotationSize, Vector3 normal = default)
    {
        var cameraTransform = GetValidCameraTransform();
        if (cameraTransform == null)
        {
            return;
        }

        if (normal == default)
        {
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation,
                Quaternion.LookRotation(Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up)),
                Time.deltaTime * rotationSize);
        }
        else
        {
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation,
                Quaternion.LookRotation(Vector3.ProjectOnPlane(normal, Vector3.up)), Time.deltaTime * rotationSize);
        }
    }

    protected float GetTargetAngle()
    {
        reusableData.targetDir = GetTargetDir();
        reusableData.targetAngle.Value = ToolFunction.GetDeltaAngle(player.transform, reusableData.targetDir);
        return reusableData.targetAngle.Value;
    }

    protected Vector3 GetTargetDir()
    {
        var cameraTransform = GetValidCameraTransform();
        if (cameraTransform == null)
        {
            return new Vector3(inputServer.Move.x, 0, inputServer.Move.y);
        }

        return Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0) * new Vector3(inputServer.Move.x, 0, inputServer.Move.y);
    }

    private Transform GetValidCameraTransform()
    {
        if (cam != null)
        {
            // Try to sync with latest camera reference after scene switch.
            if (player.CameraTransform != null && cam != player.CameraTransform)
            {
                cam = player.CameraTransform;
            }
            return cam;
        }

        if (player.CameraTransform != null)
        {
            cam = player.CameraTransform;
            return cam;
        }

        if (Camera.main != null)
        {
            cam = Camera.main.transform;
            return cam;
        }

        return null;
    }

    /// <summary>
    /// 检测是否有输入打断（事件打断）
    /// </summary>
    protected virtual void OnInputInterruption()
    {
        RayDebug.Log("添加打断检测");
        reusableData.inputInterruptionCB = () =>
        {
            if (inputServer.Move != Vector2.zero)
            {
                if (player.isOnGround.Value)
                {
                    playerStateMachine.ChangeState(playerStateMachine.moveStartState);
                    reusableData.inputInterruptionCB = null;
                }
            }
        };
    }

    protected void OnJumpStart(InputAction.CallbackContext context)
    {
        reusableLogic.OnJump();
    }

    protected void OnEnterFall()
    {
        playerStateMachine.ChangeState(playerStateMachine.fallLoopState);
    }

    protected void OnMoveStart(InputAction.CallbackContext context)
    {
        Vector3 dir = GetTargetDir();
        if (Physics.Raycast(player.transform.position, player.transform.forward, out var hitInfo, 0.7f,
                player.whatIsGround))
        {
            if (Mathf.Abs(ToolFunction.GetDeltaAngle(dir, -hitInfo.normal)) < 30)
            {
                return;
            }
        }

        playerStateMachine.ChangeState(playerStateMachine.moveStartState);
    }

    protected void OnCheckFall(bool isGround)
    {
        if (!isGround)
        {
            timerServer.AddTimer(50, OnLandToFall);
        }
    }

    protected void OnFallToLand(bool onGround)
    {
        if (onGround)
        {
            playerStateMachine.ChangeState(playerStateMachine.landState);
        }
    }

    protected void OnLandToFall()
    {
        if (!player.isOnGround.Value)
        {
            OnEnterFall();
        }
        else
        {
            OnStateDefaultEnd();
        }
    }

    protected void InAirMove()
    {
        if (player.isOnGround.Value)
        {
            return;
        }

        reusableData.horizontalSpeed = Mathf.Lerp(reusableData.horizontalSpeed,
            inputServer.Move != Vector2.zero ? 2 : 0, 1 - Mathf.Exp(-8 * Time.deltaTime));
        if (reusableData.lockValueParameter.TargetValue == 1) //索敌
        {
            //控制水平移动
            player.AddHorizontalVelocityInAir(
                GetTargetDir() * reusableData.horizontalSpeed * reusableData.currentMidInAirMultiplier +
                reusableData.currentInertialVelocity / Time.deltaTime);
        }
        else
        {
            //控制水平移动
            player.AddHorizontalVelocityInAir(
                player.transform.forward * reusableData.horizontalSpeed * reusableData.currentMidInAirMultiplier +
                reusableData.currentInertialVelocity / Time.deltaTime);
        }
    }

    /// <summary>
    /// 在地面时刷新
    /// </summary>
    public void UpdateCashVelocity(Vector3 horizontalSpeed)
    {
        reusableData.cashIndex = (reusableData.cashIndex + 1) % PlayerReusableData.cashSize;
        reusableData.cashVelocity[reusableData.cashIndex] = horizontalSpeed;
    }

    /// <summary>
    /// 离开地面时获取
    /// </summary>
    public Vector3 GetInertialVelocity()
    {
        Vector3 inertialVelocity = Vector3.zero;
        for (int i = 0; i < reusableData.cashVelocity.Length; i++)
        {
            inertialVelocity += reusableData.cashVelocity[i];
        }

        return inertialVelocity / reusableData.cashVelocity.Length;
    }

    /// <summary>
    /// 默认播放Idle
    /// </summary>
    protected void OnStateDefaultEnd()
    {
        playerStateMachine.ChangeState(playerStateMachine.idleState);
    }
}
