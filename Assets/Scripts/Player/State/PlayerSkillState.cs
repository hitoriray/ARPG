using Config;
using JKFrame;
using UnityEngine;

namespace Player.State
{
    public class PlayerSkillState : PlayerStateBase
    {
        private SkillClip skillClip;
        private CharacterController characterController;

        public override void Init(IStateMachineOwner owner)
        {
            base.Init(owner);
            characterController = PlayerController.CharacterController;
        }

        public override void Enter()
        {
            // TODO: play skill
            skillClip = ResSystem.LoadAsset<SkillClip>("AnbiSkillConfig");
            PlayerController.SkillPlayer.PlaySkillClip(skillClip, OnSkillEnd, OnWeaponDetectionAction, OnRootMotion);
        }

        /// <summary>
        /// TODO：目前只是拿到伤害检测的数据，没有实际的技能行为
        /// </summary>
        private void OnWeaponDetectionAction(Collider collider)
        {
            Debug.Log(collider.gameObject.name);
        }

        private void OnSkillEnd()
        {
            PlayerController.ChangeState(PlayerState.Idle);
        }

        private void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            // deltaPos.y -= 9.8f * Time.deltaTime; // 这个不一定是-9.8，主要还是看技能的情况
            characterController.Move(deltaPos);
            PlayerController.transform.rotation *= deltaRot;
        }

        public override void Exit()
        {
            base.Exit();
        }
    }
}