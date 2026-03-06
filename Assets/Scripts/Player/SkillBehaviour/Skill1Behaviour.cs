using Skill.Behaviour;
using UnityEngine;

namespace RayPlayer
{
    /// <summary>
    /// 多段技能（独立计算cd）
    /// </summary>
    [System.Serializable]
    public class Skill1Behaviour : PlayerSkillBehaviourBase
    {
        #region 配置
        public float standingTime = 5;  // 等待下一段技能的释放的空窗时间
        private Color normalColor = new Color(0f, 0f, 0f, 0.7f); // 半透黑色遮罩
        private Color standingColor = new Color(1f, 0.84f, 0f, 0.25f); // 半透淡金光遮罩
        #endregion
        
        private int attackIndex = -1; // -1意味着没有进入技能，0,1,2...代表在技能中（并不是技能播放中）
        public override bool autoUpdateSlot => false;
        
        public override SkillBehaviourBase DeepClone()
        {
            return new Skill1Behaviour();
        }
        
        public override void Release(bool calcCdTime = true)
        {
            base.Release(false); // 如果有一套自己的计算cd的逻辑，那么也需要写死false，表示不需要基类的计算
            playing = true;
            attackIndex += 1;
            // 如果技能是最后一段，立刻进入完整的cd
            if (attackIndex == skillConfig.Clips.Length - 1)
            {
                cdTimer = GetCdTime();
            }
            else
            {
                cdTimer = standingTime;
            }
            skillPlayer.StartPlaySkillBehaviour(this);
            skillPlayer.PlaySkillClip(skillConfig.Clips[attackIndex]);
            // 让普攻连续
            skillBrain.AddOrUpdateShareData(AnbiSkillBrain.ContinueBasicAttackDataKey, true);
        }

        public override bool CheckRelease()
        {
            bool checkCd = true;
            // 未释放技能的状态 or 释放最后一段的状态
            if (attackIndex == -1)
            {
                checkCd = cdTimer <= 0;
            }
            else if (attackIndex == skillConfig.Clips.Length - 1)
            {
                checkCd = cdTimer <= 0;
            }
            return checkCd && base.CheckCost();
        }

        public override void UpdateCdTime()
        {
            if (cdTimer > 0f)
            {
                cdTimer = Mathf.Clamp(cdTimer - Time.deltaTime, 0, float.MaxValue);
            }

            // 如果当前在中间段且倒计时结束，说明连击超时，进入完整CD
            if (attackIndex != -1 && attackIndex < skillConfig.Clips.Length - 1)
            {
                if (cdTimer <= 0f)
                {
                    cdTimer = GetCdTime();
                    attackIndex = -1;
                    playing = false; // 超时重置状态
                }
            }

            // UI 刷新
            if (TryGetSkillSlot(out var baseSlot))
            {
                int iconIndex = attackIndex + 1;
                if (iconIndex >= skillConfig.skillIcons.Length)
                    iconIndex = 0;
                baseSlot.UpdateIcon(skillConfig.skillIcons[iconIndex]);

                bool standing = (attackIndex != -1 && attackIndex < skillConfig.Clips.Length - 1);
                
                // 开启/关闭连击特效
                if (baseSlot is UI.UI_ShortcutSkill_Slot uiSlot)
                {
                    uiSlot.SetComboGlow(standing);
                }

                float showMaxCd = standing ? standingTime : GetCdTime();
                baseSlot.UpdateCdTimeAndMaskColor(cdTimer / showMaxCd, standing ? standingColor : normalColor);
            }
        }
        
        bool IsInteger(float x)
        {
            return Mathf.Abs(x - Mathf.Round(x)) < 1e-1f;
        }

        public override void OnSkillClipEnd()
        {
            base.OnSkillClipEnd();
            owner.Change2IdleState();
        }
        
        public override void OnClipEndOrReleaseNewSkill()
        {
            base.OnClipEndOrReleaseNewSkill();
            playing = false;

            // 动画结束时不额外干涉 cdTimer，让 UpdateCdTime 自然流转
            if (attackIndex == skillConfig.Clips.Length - 1)
            {
                attackIndex = -1; // 最后一段动画放完，回归正常空闲状态（此时 cdTimer 已在走大CD）
            }
        }
        
        public override void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            owner.OnSkillMove(deltaPos);
            owner.OnSkillRotate(deltaRot);
        }
    }
}
