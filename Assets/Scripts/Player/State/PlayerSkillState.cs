namespace Player.State
{
    public class PlayerSkillState : PlayerStateBase
    {
        private void PlaySkill()
        {
            // TODO: 测试技能播放逻辑
            PlayerController.SkillBrain.ReleaseSkill(currentReleaseSkillIndex);
        }

        public override void Enter()
        {
            animationController.AddAnimationEvent("FootStep", OnFootStep);
            PlaySkill();
        }

        public override void Exit()
        {
            animationController.RemoveAnimationEvent("FootStep", OnFootStep);
        }
        
        public override void Update()
        {
            if (CheckAndEnterSkillState())
            {
                PlaySkill();
            }
        }
    }
}