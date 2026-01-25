using JKFrame;
using Player.Animation;
using UnityEngine;

namespace Player.State
{
    public abstract class PlayerStateBase : StateBase
    {
        protected PlayerController PlayerController;
        protected AnimationController animationController;
        protected static int currentReleaseSkillIndex;
        
        public override void Init(IStateMachineOwner owner)
        {
            base.Init(owner);
            PlayerController = owner as PlayerController;
            animationController = PlayerController.AnimationController;
        }

        // TODO: 临时测试逻辑
        protected virtual bool CheckAndEnterSkillState()
        {
            if (Input.GetMouseButtonDown(0) && PlayerController.SkillBrain.CheckReleaseSkill(0))
            {
                currentReleaseSkillIndex = 0;
                PlayerController.ChangeState(PlayerState.Skill);
                return true;
            }
            else if (Input.GetMouseButtonDown(1) && PlayerController.SkillBrain.CheckReleaseSkill(1))
            {
                currentReleaseSkillIndex = 1;
                PlayerController.ChangeState(PlayerState.Skill);
                return true;
            }

            return false;
        }
        
        protected void OnFootStep()
        {
            int randomIndex = UnityEngine.Random.Range(0, PlayerController.CharacterConfig.FootStepAudioClips.Length);
            AudioSystem.PlayOneShot(PlayerController.CharacterConfig.FootStepAudioClips[randomIndex], PlayerController.transform.position, false, 1f);
        }
    }
}