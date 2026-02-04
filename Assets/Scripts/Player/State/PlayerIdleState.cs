using JKFrame;
using UnityEngine;

namespace RayPlayerState
{
    public class PlayerIdleState : PlayerStateBase
    {
        public override void Enter()
        {
            // 播放待机动画
            PlayerController.PlayAnimation("idle");
        }

        public override void Update()
        {
            if (UISystem.CheckMouseOnUI())
                return;
            if (CheckAndEnterSkillState())
                return;
            
            PlayerController.CharacterController.Move(new Vector3(0, -9.8f * Time.deltaTime, 0));
            // 检测玩家输入
            Vector2 moveInput = InputManager.Instance.GetMoveInput();
            if (moveInput.x != 0 || moveInput.y != 0)
            {
                PlayerController.ChangeState(PlayerState.Move);
            }
        }
    }
}