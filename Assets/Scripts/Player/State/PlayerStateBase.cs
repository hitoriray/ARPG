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
            for (int i = 0; i < PlayerController.SkillBrain.SkillConfigCount; i++)
            {
                bool valid;
                // 默认0是普攻
                if (i == 0)
                {
                    valid = InputManager.Instance.GetBasicAttackKeyState() && 
                            PlayerController.SkillBrain.CheckReleaseSkill(i);
                }
                else
                {
                    valid = InputManager.Instance.GetSkillKeyState(i - 1) &&
                            PlayerController.SkillBrain.CheckReleaseSkill(i);
                }
                
                if (valid)
                {
                    currentReleaseSkillIndex = i;
                    PlayerController.ChangeState(PlayerState.Skill);
                    return true;
                }
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