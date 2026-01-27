using Config;
using Data;
using Player;
using UnityEngine;

namespace Skill.Behaviour
{
    public abstract class SkillBehaviourBase
    {
        protected PlayerController player;
        protected SkillConfig skillConfig;
        protected SkillBrainBase skillBrain;
        protected SkillPlayer skillPlayer;
        protected bool canRotate = false;
        protected bool playing = false; // 是否在技能播放中
        protected float cdTimer;
        protected SkillLearnedData skillLearnedData;

        public abstract SkillBehaviourBase DeepClone();

        public virtual void Init(PlayerController player, SkillConfig skillConfig, SkillBrainBase skillBrain, SkillPlayer skillPlayer, SkillLearnedData skillLearnedData)
        {
            this.player = player;
            this.skillConfig = skillConfig;
            this.skillBrain = skillBrain;
            this.skillPlayer = skillPlayer;
            this.skillLearnedData = skillLearnedData;
        }
        
        public virtual void OnUpdate()
        {
            UpdateCdTime();
            RotateOnUpdate();
        }

        public virtual void UpdateCdTime()
        {
            if (cdTimer <= 0)
                return;
            cdTimer = Mathf.Clamp(cdTimer - Time.deltaTime, 0, float.MaxValue);
        }

        public virtual float GetCdTime()
        {
            return skillConfig.GetCdTimeByLv(skillLearnedData.lv);
        }

        public virtual bool CheckCdTime()
        {
            return cdTimer <= 0;
        }
        
        public virtual void Release(bool calcCdTime = true)
        {
            if (calcCdTime) cdTimer = GetCdTime();
            canRotate = false;
            playing = true;
            skillBrain.SetCanReleaseFlag(false);
            ApplyCosts();
        }
        
        public virtual bool CheckRelease()
        {
            return CheckCost() && CheckCdTime();
        }

        public virtual void ApplyCosts()
        {
            foreach (var item in skillConfig.ReleaseCostDict)
            {
                skillBrain.ApplyCost(item.Key, item.Value);
            }
        }

        public virtual bool CheckCost()
        {
            foreach (var item in skillConfig.ReleaseCostDict)
            {
                if (skillBrain.CheckCost(item.Key, item.Value) == false)
                {
                    return false;
                }
            }
            return true;
        }

        protected virtual void RotateOnUpdate()
        {
            // TODO: 怪物不能基于玩家的控制进行旋转
            if (canRotate)
            {
                Vector2 moveInput = InputManager.Instance.GetMoveInput();
                if (moveInput.x != 0 || moveInput.y != 0)
                {
                    player.Rotate(new Vector3(moveInput.x, 0, moveInput.y));
                }
            }
        }

        public virtual void OnReleaseNewSkill()
        {
            OnClipEndOrReleaseNewSkill();
        }
        
        public virtual void OnSkillClipEnd()
        {
            OnClipEndOrReleaseNewSkill();
        }

        public virtual void OnClipEndOrReleaseNewSkill()
        {
            playing = false;
        }
        
        #region 技能驱动时的事件

        public virtual void OnTickSkill(int frameIndex)
        {
        }
        
        public virtual SkillAnimationEvent BeforeSkillAnimationEvent(SkillAnimationEvent evt)
        {
            return evt;
        }
        
        public virtual SkillAudioEvent BeforeSkillAudioEvent(SkillAudioEvent evt)
        {
            return evt;
        }
        
        public virtual SkillEffectEvent BeforeSkillEffectEvent(SkillEffectEvent evt)
        {
            return evt;
        }
        
        public virtual SkillAttackDetectionEvent BeforeSkillAttackDetectionEvent(SkillAttackDetectionEvent evt)
        {
            return evt;
        }
        
        public virtual SkillCustomEvent BeforeSkillCustomEvent(SkillCustomEvent evt)
        {
            return evt;
        }
        
        public virtual void AfterSkillAnimationEvent(SkillAnimationEvent evt)
        {
        }
        
        public virtual void AfterSkillAudioEvent(SkillAudioEvent evt)
        {
        }
        
        public virtual void AfterSkillEffectEvent(SkillEffectEvent evt)
        {
        }
        
        public virtual void AfterSkillAttackDetectionEvent(SkillAttackDetectionEvent evt)
        {
        }
        
        public virtual void AfterSkillCustomEvent(SkillCustomEvent evt)
        {
            if (evt.EventType == SkillEventType.CanSkillRelease)
            {
                skillBrain.SetCanReleaseFlag(true);
            }
            else if (evt.EventType == SkillEventType.CanRotate)
            {
                canRotate = true;
            }
            else if (evt.EventType == SkillEventType.CannotRotate)
            {
                canRotate = false;
            }
        }

        public virtual void OnAttackDetection(Collider col)
        {
        }

        public virtual void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
        }
        
        #endregion
    }
}