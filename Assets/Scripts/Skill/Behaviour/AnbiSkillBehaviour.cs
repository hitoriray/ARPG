using Player.State;
using UnityEngine;

namespace Skill.Behaviour
{
    public class AnbiSkillBehaviour : SkillBehaviourBase
    {
        #region 配置
        public float standingTime = 5;  // 等待下一段技能的释放的空窗时间
        #endregion
        
        private int attackIndex = -1; // -1意味着没有进入技能，0,1,2...代表在技能中（并不是技能播放中）

        public override SkillBehaviourBase DeepClone()
        {
            return new AnbiSkillBehaviour();
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
            skillPlayer.StartPlaySkillConfig(this);
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
                Debug.Log("正在播放技能");
                if (attackIndex == skillConfig.Clips.Length - 1)
                {
                    cdTimer = Mathf.Clamp(cdTimer - Time.deltaTime, 0, float.MaxValue);
                    Debug.Log($"播放状态：技能处于最后一段，已经在计算CD中:{cdTimer}/{GetCdTime()}");
                }
                else
                {
                    Debug.Log("播放状态：技能未处于最后一段，不计算任何CD");
                }
                return;
            }
            cdTimer = Mathf.Clamp(cdTimer - Time.deltaTime, 0, float.MaxValue);
            
            // 技能处于某一段，但是可能会超时
            if (attackIndex != -1)
            {
                // 已经超时，应该进入到完整CD
                if (cdTimer <= 0)
                {
                    cdTimer = GetCdTime();
                    attackIndex = -1;
                    Debug.Log("技能没有完全释放完毕，但是开始进入完整CD了！");
                }
                else
                {
                    Debug.Log($"技能没有完全释放完毕，正在计算内部CD:{cdTimer}/{standingTime}");
                }
            }
            else
            {
                Debug.Log($"技能没有在释放，正在计算CD:{cdTimer}/{GetCdTime()}");
            }
        }

        public override void OnSkillClipEnd()
        {
            base.OnSkillClipEnd();
            player.ChangeState(PlayerState.Idle);
        }
        
        public override void OnClipEndOrReleaseNewSkill()
        {
            base.OnClipEndOrReleaseNewSkill();
            playing = false;
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
            player.CharacterController.Move(deltaPos);
            player.ModelTransform.rotation *= deltaRot;
        }
    }
}