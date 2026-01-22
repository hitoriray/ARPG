using Config;
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

        public abstract SkillBehaviourBase DeepClone();

        public virtual void Init(PlayerController player, SkillConfig skillConfig, SkillBrainBase skillBrain, SkillPlayer skillPlayer)
        {
            this.player = player;
            this.skillConfig = skillConfig;
            this.skillBrain = skillBrain;
            this.skillPlayer = skillPlayer;
        }
        
        public virtual void OnUpdate()
        {
        }
        
        public virtual void Release()
        {
            ApplyCosts();
        }
        
        public virtual bool CheckRelease()
        {
            return CheckCost();
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
        
        #region 技能驱动时的事件

        public virtual void OnTickSkill(int frameIndex)
        {
        }

        public virtual void OnSkillClipEnd()
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