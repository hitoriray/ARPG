using System;
using JKFrame;
using Player.Animation;
using UnityEngine;

namespace Player.State
{
    public class PlayerMoveState : PlayerStateBase
    {
        private CharacterController characterController;
        private float runTransition;
        private bool applyRootMotionForMove;

        public override void Init(IStateMachineOwner owner)
        {
            base.Init(owner);
            characterController = PlayerController.CharacterController;
            applyRootMotionForMove = PlayerController.CharacterConfig.ApplyRootMotionForMove;
        }

        public override void Enter()
        {
            runTransition = 0;
            Action<Vector3, Quaternion> rootMotionAction = null;
            if (applyRootMotionForMove)
                rootMotionAction = OnRootMotion;
            PlayerController.PlayBlendAnimation("walk", "run", rootMotionAction);
            animationController.SetBlendWeight(1);
            
            animationController.AddAnimationEvent("FootStep", OnFootStep);
        }

        public override void Update()
        {
            if (UISystem.CheckMouseOnUI())
            {
                PlayerController.ChangeState(PlayerState.Idle);
                return;
            }
            if (CheckAndEnterSkillState())
                return;
            
            Vector2 moveInput = InputManager.Instance.GetMoveInput();
            if (moveInput.x == 0 && moveInput.y == 0)
            {
                PlayerController.ChangeState(PlayerState.Idle);
                return;
            }

            // 处理移动
            Vector3 input = new Vector3(moveInput.x, 0, moveInput.y);
            if (Input.GetKey(KeyCode.LeftShift))
                runTransition = Mathf.Clamp01(runTransition + Time.deltaTime * PlayerController.CharacterConfig.Walk2RunTransitionSpeed);
            else
                runTransition = Mathf.Clamp01(runTransition - Time.deltaTime * PlayerController.CharacterConfig.Walk2RunTransitionSpeed);
            animationController.SetBlendWeight(1 - runTransition);
            
            // 获取相机的旋转值
            float y = Camera.main.transform.rotation.eulerAngles.y;
            // 让input也旋转y角度
            Vector3 moveDir = Quaternion.Euler(0, y, 0) * input;
            // 处理旋转
            PlayerController.Rotate(input);
            // 如果不是根运动
            if (!applyRootMotionForMove)
            {
                float speed = Mathf.Lerp(PlayerController.WalkSpeed, PlayerController.RunSpeed, runTransition);
                Vector3 motion = Time.deltaTime * speed * moveDir;
                motion.y = -9.8f * Time.deltaTime;
                characterController.Move(motion);
            }
        }

        public override void Exit()
        {
            if (applyRootMotionForMove)
            {
                animationController.ClearRootMotionAction();
            }
            animationController.RemoveAnimationEvent("FootStep", OnFootStep);
        }
        
        private void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            // 此时的速度是影响动画的播放速度来达到实际位移速度的变化
            float speed = Mathf.Lerp(PlayerController.WalkSpeed, PlayerController.RunSpeed, runTransition);
            animationController.Speed = speed;
            deltaPos.y = -9.8f * Time.deltaTime;
            characterController.Move(deltaPos);
        }
    }
}