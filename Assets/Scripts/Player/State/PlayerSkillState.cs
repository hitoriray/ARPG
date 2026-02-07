// using UnityEngine;

// namespace RayPlayerState
// {
//     public class PlayerSkillState : PlayerStateBase
//     {
//         private void PlaySkill()
//         {
//             // TODO: 测试技能播放逻辑
//             PlayerController.SkillBrain.ReleaseSkill(currentReleaseSkillIndex);
//         }

//         public override void Enter()
//         {
//             animationController.AddAnimationEvent("FootStep", OnFootStep);
//             PlaySkill();
//         }

//         public override void Exit()
//         {
//             animationController.RemoveAnimationEvent("FootStep", OnFootStep);
//         }
        
//         public override void Update()
//         {
//             // 检测移动打断
//             if (PlayerController.SkillBrain.CanInterrupt)
//             {
//                 Vector2 moveInput = InputManager.Instance.GetMoveInput();
//                 if (moveInput.x != 0 || moveInput.y != 0)
//                 {
//                     RayDebug.Log($"由于移动打断技能Combo");
//                     // 打断当前技能（包括重置普攻段数）+ 销毁临时武器
//                     PlayerController.SkillBrain.InterruptCurrentSkill();
//                     PlayerController.DestroyWeapon(-1);
//                     PlayerController.ChangeState(PlayerState.Move);
//                     return;
//                 }
//             }
            
//             if (CheckAndEnterSkillState())
//             {
//                 PlaySkill();
//             }
//         }
//     }
// }