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
        protected bool canRotate = false;

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
            RotateOnUpdate();
        }
        
        public virtual void Release()
        {
            canRotate = false;
            skillBrain.SetCanReleaseFlag(false);
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

        protected virtual void RotateOnUpdate()
        {
            // TODO: 怪物不能基于玩家的控制进行旋转
            if (canRotate)
            {
                float h = Input.GetAxis("Horizontal");
                float v = Input.GetAxis("Vertical");
                if (h != 0 || v != 0)
                {
                    player.Rotate(new Vector3(h, 0, v));
                }
            }
        }

        public virtual void OnReleaseNew()
        {
            
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