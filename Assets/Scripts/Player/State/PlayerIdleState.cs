using JKFrame;
using UnityEngine;

namespace Player.State
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
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            if (h != 0 || v != 0)
            {
                PlayerController.ChangeState(PlayerState.Move);
            }
        }
    }
}