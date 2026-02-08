using RayPlayer;
using UnityEngine;
using RayPlayerState;

namespace Skill.Behaviour
{
    public class BasicBehaviour : PlayerSkillBehaviourBase
    {
        private int attackIndex = -1;  // 当前的普攻段数索引
        [SerializeField] private int standAttackCount = 3; // 标准的普攻段数
        [SerializeField] private int perfectClipIndex = 3; // 特殊攻击索引
        public override bool autoUpdateSlot => false;
        
        public override SkillBehaviourBase DeepClone()
        {
            return new BasicBehaviour
            {
                standAttackCount = standAttackCount,
                perfectClipIndex = perfectClipIndex,
            };
        }

        public override void Release(bool calcCdTime = true)
        {
            base.Release(false); // 普攻永远是false，所以其实可以写死false
            // 特殊技能
            if (skillBrain.TryGetShareData(AnbiSkillBrain.PerfectAttackClip1, out bool hasPerfectClip) && hasPerfectClip)
            {
                skillBrain.AddOrUpdateShareData(AnbiSkillBrain.PerfectAttackClip1, false);
                attackIndex = perfectClipIndex;
            }
            else
            {
                attackIndex += 1;
                if (attackIndex >= standAttackCount)
                    attackIndex = 0;
            }
            skillPlayer.StartPlaySkillBehaviour(this);
            RayDebug.Log($"当前播放普攻{attackIndex}");
            skillPlayer.PlaySkillClip(skillConfig.Clips[attackIndex]);
        }
        
        public override void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            owner.OnSkillMove(deltaPos);
            owner.OnSkillRotate(deltaRot);
        }
        
        public override void OnSkillClipEnd()
        {
            base.OnSkillClipEnd();
            owner.Change2IdleState();
        }

        public override void OnClipEndOrReleaseNewSkill()
        {
            base.OnClipEndOrReleaseNewSkill();
            if (skillBrain.TryGetShareData(AnbiSkillBrain.ContinueBasicAttackDataKey, out bool canContinue)
                && canContinue == false)
            {
                attackIndex = -1;
            }
            skillBrain.AddOrUpdateShareData(AnbiSkillBrain.ContinueBasicAttackDataKey, false);
        }
        
        public override void OnInterrupt()
        {
            base.OnInterrupt();
            // 打断时重置普攻段数
            RayDebug.Log($"打断普攻段数, attackIndex={attackIndex}");
            attackIndex = -1;
            skillBrain.AddOrUpdateShareData(AnbiSkillBrain.ContinueBasicAttackDataKey, false);
        }
    }
}
