using UnityEngine;

public class PlayerMoveEndState : PlayerMovementState
{
    public PlayerMoveEndData moveEndData;
    float angle;
    float speed;

    public PlayerMoveEndState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        moveEndData = playerSO.playerMovementData.PlayerMoveEndData;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        angle = reusableData.rotationValueParameter.CurrentValue;
        speed = reusableData.speedValueParameter.CurrentValue;
        //判断前面有没有墙
        if (CheckWall())
        {
            return;
        }

        //判断是站还是蹲
        CheckLeftOrRightFoot();
    }

    private bool CheckWall()
    {
        if (Physics.Raycast(player.transform.position + Vector3.up, player.transform.forward, out var hitInfo,
                reusableData.checkWallDistance, player.whatIsGround))
        {
            //还有距离判断
            float distance = Vector3.Distance(player.transform.position + Vector3.up, hitInfo.point);
            if (distance > 0.45f && distance < reusableData.checkWallDistance)
            {
                animancer.Play(moveEndData.moveToWall).Events(player).OnEnd = OnStateDefaultEnd;
                return true;
            }
        }

        return false;
    }

    private void CheckLeftOrRightFoot()
    {
        Transform leftFoot = player.animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rightFoot = player.animator.GetBoneTransform(HumanBodyBones.RightFoot);

        // Generic rig 没有 Humanoid Avatar，GetBoneTransform 返回 null，用奇偶帧做轻量fallback
        if (leftFoot == null || rightFoot == null)
        {
            bool useLeft = (Time.frameCount % 2 == 0);
            animancer.Play(useLeft ? moveEndData.moveEnd_L : moveEndData.moveEnd_R)
                     .Events(player).OnEnd = OnStateDefaultEnd;
            return;
        }

        Vector3 leftFootLocalPos  = player.transform.InverseTransformPoint(leftFoot.position);
        Vector3 rightFootLocalPos = player.transform.InverseTransformPoint(rightFoot.position);

        if (leftFootLocalPos.z > rightFootLocalPos.z)
        {
            RayDebug.Log("左腿在前");
            animancer.Play(moveEndData.moveEnd_L).Events(player).OnEnd = OnStateDefaultEnd;
        }
        else
        {
            RayDebug.Log("右腿在前");
            animancer.Play(moveEndData.moveEnd_R).Events(player).OnEnd = OnStateDefaultEnd;
        }
    }

    protected override void AddEventListening()
    {
        base.AddEventListening();
        inputServer.inputMap.Player.Jump.started += OnJumpStart;
        inputServer.inputMap.Player.Move.started += OnMoveStart;
        inputServer.inputMap.Player.Crouch.started += OnCrouch;
        inputServer.inputMap.Player.Shift.started += OnDodge;
        inputServer.inputMap.Player.ToggleRun.started += OnToggleRun;
        inputServer.inputMap.Player.Roll.started += OnRoll;
        player.isOnGround.ValueChanged += OnCheckFall;
    }

    protected override void RemoveEventListening()
    {
        base.RemoveEventListening();
        inputServer.inputMap.Player.Jump.started -= OnJumpStart;
        inputServer.inputMap.Player.Move.started -= OnMoveStart;
        inputServer.inputMap.Player.Crouch.started -= OnCrouch;
        inputServer.inputMap.Player.Shift.started -= OnDodge;
        inputServer.inputMap.Player.ToggleRun.started -= OnToggleRun;
        inputServer.inputMap.Player.Roll.started -= OnRoll;
        player.isOnGround.ValueChanged -= OnCheckFall;
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        reusableData.rotationValueParameter.TargetValue = angle;
        reusableData.speedValueParameter.TargetValue = speed;
    }
}