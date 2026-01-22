namespace Player.State
{
    public class PlayerSkillState : PlayerStateBase
    {
        public override void Enter()
        {
            // TODO: 测试技能播放逻辑
            PlayerController.SkillBrain.ReleaseSkill(currentReleaseSkillIndex);
        }
    }
}