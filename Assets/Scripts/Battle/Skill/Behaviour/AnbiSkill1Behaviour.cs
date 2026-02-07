using RayPlayer;
using RayPlayerState;
using UnityEngine;

namespace Skill.Behaviour
{
    public class AnbiSkill1Behaviour : PlayerSkillBehaviourBase
    {
        #region 配置
        public float standingTime = 5;  // 等待下一段技能的释放的空窗时间
        private Color normalColor = new Color(0, 0, 0, 0.8f);
        private Color standingColor = new Color(1, 0, 0, 0.8f);
        #endregion
        
        private int attackIndex = -1; // -1意味着没有进入技能，0,1,2...代表在技能中（并不是技能播放中）
        public override bool autoUpdateSlot => false;
        
        public override SkillBehaviourBase DeepClone()
        {
            return new AnbiSkill1Behaviour();
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
            if (playing)
            {
                // 播放状态: 技能处于最后一段的技能，已经在计算CD中
                if (attackIndex == skillConfig.Clips.Length - 1)
                {
                    cdTimer = Mathf.Clamp(cdTimer - Time.deltaTime, 0, float.MaxValue);
                    if (IsInteger(cdTimer))
                        RayDebug.Log($"播放状态：技能处于最后一段，已经在计算CD中:{cdTimer}/{GetCdTime()}");
                }
                // 播放状态:技能没有处于最后一段的技能，不计算任何CD
            }
            else
            {
                cdTimer = Mathf.Clamp(cdTimer - Time.deltaTime, 0, float.MaxValue);

                // 技能处于某一段，但是可能会超时
                if (attackIndex != -1)
                {
                    // 已经超时，应该进入到完整CD
                    // 技能已经播放完某一段，但是没有播放完整个技能，同时开始已经进入CD了！
                    if (cdTimer <= 0)
                    {
                        cdTimer = GetCdTime();
                        attackIndex = -1;
                        RayDebug.Log("技能没有完全释放完毕，但是开始进入完整CD了！");
                    }
                    // 技能没有播放完某一段，正在计算内部CD
                    else
                    {
                        if (IsInteger(cdTimer))
                            RayDebug.Log($"技能没有完全释放完毕，正在计算内部CD:{cdTimer}/{standingTime}");
                    }
                }
                // else
                // {
                //     if (cdTimer > 0 && IsInteger(cdTimer))
                //         RayDebug.Log($"技能没有在释放，正在计算CD:{cdTimer}/{GetCdTime()}");
                // }
            }

            if (TryGetSkillSlot(out var slot))
            {
                int iconIndex = attackIndex + 1; // 预期的下一个技能索引
                if (iconIndex >= skillConfig.skillIcons.Length)
                    iconIndex = 0;
                slot.UpdateIcon(skillConfig.skillIcons[iconIndex]);
                bool standing = iconIndex != 0;
                float showMaxCd = standing ? standingTime : GetCdTime();
                slot.UpdateCdTimeAndMaskColor(cdTimer / showMaxCd, standing ? standingColor : normalColor);
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
            // 如果技能已结束，则不需要触发standingTime
            if (attackIndex < 0)
                return;
            // 当前结束的是最后一段，说明技能全部结束了
            if (attackIndex == skillConfig.Clips.Length - 1)
            {
                attackIndex = -1;
            }
            // 结束的是中间的某一段技能
            else
            {
                cdTimer = standingTime;
            }
        }
        
        public override void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            deltaPos.y += Time.deltaTime * -9.8f;
            owner.OnSkillMove(deltaPos);
            owner.OnSkillRotate(deltaRot);
        }
    }
}