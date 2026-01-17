using System;
using JKFrame;
using Player.Animation;
using UnityEngine;

namespace Player.State
{
    public class PlayerMoveState : PlayerStateBase
    {
        private CharacterController characterController;
        private AnimationController animationController;
        private float runTransition;
        private bool applyRootMotionForMove;

        public override void Init(IStateMachineOwner owner, int stateType, StateMachine stateMachine)
        {
            base.Init(owner, stateType, stateMachine);
            characterController = PlayerController.GetComponent<CharacterController>();
            animationController = PlayerController.AnimationController;
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
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            if (h == 0 && v == 0)
            {
                PlayerController.ChangeState(PlayerState.Idle);
            }
            else
            {
                // 处理移动
                Vector3 input = new Vector3(h, 0, v);
                if (Input.GetKey(KeyCode.LeftShift))
                    runTransition = Mathf.Clamp01(runTransition + Time.deltaTime * PlayerController.CharacterConfig.Walk2RunTransitionSpeed);
                else
                    runTransition = Mathf.Clamp01(runTransition - Time.deltaTime * PlayerController.CharacterConfig.Walk2RunTransitionSpeed);
                animationController.SetBlendWeight(1 - runTransition);
                
                // 获取相机的旋转值
                float y = Camera.main.transform.rotation.eulerAngles.y;
                // 让input也旋转y角度
                Vector3 moveDir = Quaternion.Euler(0, y, 0) * input;

                // 如果不是根运动
                if (!applyRootMotionForMove)
                {
                    float speed = Mathf.Lerp(PlayerController.WalkSpeed, PlayerController.RunSpeed, runTransition);
                    Vector3 motion = Time.deltaTime * speed * moveDir;
                    motion.y = -9.8f * Time.deltaTime;
                    characterController.Move(motion);
                }
                
                // 处理旋转
                PlayerController.ModelTransform.rotation = Quaternion.Slerp(PlayerController.ModelTransform.rotation, 
                    Quaternion.LookRotation(moveDir), Time.deltaTime * PlayerController.RotateSpeed);
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

        private void OnFootStep()
        {
            int randomIndex = UnityEngine.Random.Range(0, PlayerController.CharacterConfig.FootStepAudioClips.Length);
            AudioManager.Instance.PlayOnShot(PlayerController.CharacterConfig.FootStepAudioClips[randomIndex], PlayerController.transform.position, 1f);
        }
    }
}