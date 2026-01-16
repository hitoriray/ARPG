using JKFrame;
using UnityEngine;

namespace Player.State
{
    public class PlayerMoveState : PlayerStateBase
    {
        private CharacterController characterController;
        private float runTransition;

        public override void Init(IStateMachineOwner owner, int stateType, StateMachine stateMachine)
        {
            base.Init(owner, stateType, stateMachine);
            characterController = PlayerController.GetComponent<CharacterController>();
        }

        public override void Enter()
        {
            runTransition = 0;
            PlayerController.PlayBlendAnimation("walk", "run", OnRootMotion);
            PlayerController.SetBlendAnimationWeight(1);
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
                    runTransition = Mathf.Clamp01(runTransition + Time.deltaTime * PlayerController.Walk2RunTransitionSpeed);
                else
                    runTransition = Mathf.Clamp01(runTransition - Time.deltaTime * PlayerController.Walk2RunTransitionSpeed);
                PlayerController.SetBlendAnimationWeight(1 - runTransition);
                
                // 获取相机的旋转值
                float y = Camera.main.transform.rotation.eulerAngles.y;
                // 让input也旋转y角度
                float speed = Mathf.Lerp(PlayerController.WalkSpeed, PlayerController.RunSpeed, runTransition);
                Vector3 moveDir = Quaternion.Euler(0, y, 0) * input;
                Vector3 motion = Time.deltaTime * speed * moveDir;
                motion.y = -9.8f * Time.deltaTime;
                characterController.Move(motion);

                // 处理旋转
                PlayerController.ModelTransform.rotation = Quaternion.Slerp(PlayerController.ModelTransform.rotation, 
                    Quaternion.LookRotation(moveDir), Time.deltaTime * PlayerController.RotateSpeed);
            }
        }

        public override void Exit()
        {
            PlayerController.ClearRootMotionAction();
        }
        
        private void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            Debug.Log(deltaPos);
        }
    }
}