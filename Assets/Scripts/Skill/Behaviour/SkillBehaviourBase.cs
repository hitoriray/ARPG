using Config;
using UnityEngine;

namespace Skill.Behaviour
{
    public abstract class SkillBehaviourBase
    {
        public abstract SkillBehaviourBase DeepClone();
        
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

        public virtual void OnWeaponDetection(Collider col)
        {
        }
        
        #endregion
    }
}