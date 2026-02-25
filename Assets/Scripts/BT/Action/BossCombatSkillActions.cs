using BehaviorDesigner.Runtime.Tasks;

namespace BT.Actions
{
    [TaskCategory("Enemy/Boss")] public class PunishDashAttack : BossSkillActionBase { }
    [TaskCategory("Enemy/Boss")] public class QuickThrust : BossSkillActionBase { }
    [TaskCategory("Enemy/Boss")] public class HeavySweepAttack : BossSkillActionBase { }
    [TaskCategory("Enemy/Boss")] public class WarStompOrPommelStrike : BossSkillActionBase { }
    [TaskCategory("Enemy/Boss")] public class LeapThrustOrDashAttack : BossSkillActionBase { }
    [TaskCategory("Enemy/Boss")] public class CastTrackingMagic : BossSkillActionBase { }
    [TaskCategory("Enemy/Boss")] public class TeleportStrikeOrMassiveAOE : BossSkillActionBase { }
    [TaskCategory("Enemy/Boss")] public class UltimateComboInitiation : BossSkillActionBase { }
    [TaskCategory("Enemy/Boss")] public class ContinuousGapCloser : BossSkillActionBase { }
    [TaskCategory("Enemy/Boss")] public class AggressiveDash : BossSkillActionBase { }
    [TaskCategory("Enemy/Boss")] public class AOEBurst : BossSkillActionBase { }

    [TaskCategory("Enemy/Boss")]
    public class DelayedGroundExplosion : BossSkillActionBase
    {
        public override TaskStatus OnUpdate()
        {
            if (SkillIndex.Value < 0)
            {
                RayDebug.Warn("[BT] DelayedGroundExplosion 未配置技能索引，执行占位逻辑");
                return TaskStatus.Success;
            }
            return base.OnUpdate();
        }
    }
}
