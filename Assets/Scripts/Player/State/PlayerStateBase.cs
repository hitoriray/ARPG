using JKFrame;
using RayAnimation;
using RayPlayer;

namespace RayPlayerState
{
    public abstract class PlayerStateBase : JKFrame.StateBase
    {
        protected PlayerController PlayerController;
        protected static int currentReleaseSkillIndex;
        
        public override void Init(IStateMachineOwner owner)
        {
            base.Init(owner);
            PlayerController = owner as PlayerController;
        }

        // TODO: 临时测试逻辑
        protected virtual bool CheckAndEnterSkillState()
        {
            if (UISystem.CheckMouseOnUI())
                return false;
            
            for (int i = 0; i < PlayerController.SkillBrain.SkillCount; i++)
            {
                bool valid = false;
                // 实际对应角色配置中的技能索引
                int skillIndex = PlayerController.SkillBrain.GetSkillIndex(i);
                // 默认0是普攻
                if (i == 0) // 鼠标普攻的专门检测
                {
                    valid = InputManager.Instance.GetBasicAttackKeyState() && PlayerController.SkillBrain.CheckReleaseSkill(i);
                    if (valid)
                    {
                        InputManager.Instance.ResetBasicAttackKeyCodeCacheTimer();
                    }
                }

                if (valid == false) // 有可能普攻也放在技能快捷栏中
                {
                    valid = InputManager.Instance.GetSkillKeyState(skillIndex) && PlayerController.SkillBrain.CheckReleaseSkill(i);
                }
                
                if (valid)
                {
                    currentReleaseSkillIndex = i;
                    InputManager.Instance.ResetSkillKeyCodeCacheTimer(skillIndex);
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