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
            PlaySkill();
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