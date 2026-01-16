using JKFrame;
using UnityEngine;

namespace Player.State
{
    public class PlayerMoveState : PlayerStateBase
    {
        private CharacterController characterController;

        public override void Init(IStateMachineOwner owner, int stateType, StateMachine stateMachine)
        {
            base.Init(owner, stateType, stateMachine);
            characterController = PlayerController.GetComponent<CharacterController>();
        }

        public override void Enter()
        {
            // PlayerController.PlayAnimation("walk");
            PlayerController.PlayBlendAnimation("walk", "run");
            PlayerController.SetBlendAnimationWeight(1);
        }

        public override void Update()
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                PlayerController.SetBlendAnimationWeight(0);
            }
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                PlayerController.SetBlendAnimationWeight(1);
            }
            
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
                // 获取相机的旋转值
                float y = Camera.main.transform.rotation.eulerAngles.y;
                // 让input也旋转y角度
                Vector3 moveDir = Quaternion.Euler(0, y, 0) * input;
                Vector3 motion = Time.deltaTime * PlayerController.WalkSpeed * moveDir;
                motion.y = -9.8f;
                characterController.Move(motion);

                // 处理旋转
                PlayerController.ModelTransform.rotation = Quaternion.Slerp(PlayerController.ModelTransform.rotation, 
                    Quaternion.LookRotation(moveDir), Time.deltaTime * PlayerController.RotateSpeed);
                
            }
        }
    }
}