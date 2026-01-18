using Config;
using JKFrame;
using UnityEngine;

namespace Player.State
{
    public class PlayerSkillState : PlayerStateBase
    {
        private SkillConfig skillConfig;
        private CharacterController characterController;

        public override void Init(IStateMachineOwner owner, int stateType, StateMachine stateMachine)
        {
            base.Init(owner, stateType, stateMachine);
            characterController = PlayerController.CharacterController;
        }

        public override void Enter()
        {
            // TODO: play skill
            skillConfig = ResManager.LoadAsset<SkillConfig>("AnbiSkillConfig");
            PlayerController.SkillPlayer.PlaySkill(skillConfig, OnSkillEnd, OnRootMotion);
        }

        private void OnSkillEnd()
        {
            PlayerController.ChangeState(PlayerState.Idle);
        }

        private void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            deltaPos.y -= 9.8f * Time.deltaTime; // 这个不一定是-9.8，主要还是看技能的情况
            characterController.Move(deltaPos);
            PlayerController.transform.rotation *= deltaRot;
        }

        public override void Exit()
        {
            base.Exit();
        }
    }
}