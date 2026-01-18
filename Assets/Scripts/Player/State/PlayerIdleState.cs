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
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            if (h != 0 || v != 0)
            {
                PlayerController.ChangeState(PlayerState.Move);
            }
            
            // TODO: change to skill
            if (Input.GetMouseButtonDown(0))
            {
                PlayerController.ChangeState(PlayerState.Skill);
            }
        }
    }
}