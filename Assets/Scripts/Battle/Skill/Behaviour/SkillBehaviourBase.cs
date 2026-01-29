using System.Collections.Generic;
using Battle.ECS.View.Helper;
using Config;
using Data;
using JKFrame;
using UI;
using UnityEngine;

namespace Skill.Behaviour
{
    public abstract class SkillBehaviourBase
    {
        protected ICharacter owner;
        protected SkillConfig skillConfig;
        protected SkillBrainBase skillBrain;
        protected SkillPlayer skillPlayer;
        protected bool canRotate = false;
        protected bool playing = false; // 是否在技能播放中
        protected float cdTimer;
        protected SkillLearnedData skillLearnedData;
        public int skillIndex { get; private set; } // 角色配置中的技能索引
        private HashSet<IHitTarget> hitTargets;
        public virtual bool autoUpdateSlot { get => true; }
        public int SkillLv => skillLearnedData == null ? 1 : skillLearnedData.lv;

        public abstract SkillBehaviourBase DeepClone();

        public virtual void Init(ICharacter owner, SkillConfig skillConfig, SkillBrainBase skillBrain, SkillPlayer skillPlayer, SkillLearnedData skillLearnedData, int skillIndex)
        {
            this.owner = owner;
            this.skillConfig = skillConfig;
            this.skillBrain = skillBrain;
            this.skillPlayer = skillPlayer;
            this.skillLearnedData = skillLearnedData;
            this.skillIndex = skillIndex;
            hitTargets = new HashSet<IHitTarget>();
        }
        
        public virtual void OnUpdate()
        {
            UpdateCdTime();
            RotateOnUpdate();
            if (autoUpdateSlot)
            {
                UpdateSkillSlot();
            }
        }

        public virtual void UpdateCdTime()
        {
            if (cdTimer <= 0)
                return;
            cdTimer = Mathf.Clamp(cdTimer - Time.deltaTime, 0, float.MaxValue);
        }

        protected void UpdateSkillSlot()
        {
            if (TryGetSkillSlot(out var slot))
            {
                OnUpdateSkillSlot(slot);
            }
        }

        protected bool TryGetSkillSlot(out UI_ShortcutSkill_Slot slot)
        {
            return UISystem.GetWindow<UI_GameSceneMainWindow>().TryGetShortcutSkillSlot(skillIndex, out slot);
        }

        protected virtual void OnUpdateSkillSlot(UI_ShortcutSkill_Slot slot)
        {
            float max = skillConfig.GetCdTimeByLv(SkillLv);
            float value = 0;
            if (max != 0) value = cdTimer / max;
            slot.UpdateCdTime(value);
            slot.UpdateSkillReleaseState(CheckRelease());
        }

        public virtual float GetCdTime()
        {
            return skillConfig.GetCdTimeByLv(SkillLv);
        }

        public virtual bool CheckCdTime()
        {
            return cdTimer <= 0;
        }
        
        public virtual void Release(bool calcCdTime = true)
        {
            if (calcCdTime) cdTimer = GetCdTime();
            hitTargets.Clear();
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
            if (canRotate)
            {
                owner.OnSkillRotate();
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
            hitTargets.Clear();
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
            else if (evt.EventType == SkillEventType.AddBuff)
            {
                owner.AddBuff((BuffConfig)evt.ObjectArg, evt.IntArg);
            }
        }

        public virtual void OnAttackDetection(IHitTarget hitTarget, AttackData attackData)
        {
            // 避免重复命中
            if (hitTargets.Add(hitTarget))
            {
                OnHitTarget(hitTarget, attackData);
            }
        }

        public virtual void OnHitTarget(IHitTarget hitTarget, AttackData attackData)
        {
            if (attackData.detectionEvent.AttackHitConfig != null)
            {
                DoHitEffect(attackData);
            }
            hitTarget.OnHit(attackData);
        }

        protected void DoHitEffect(AttackData attackData)
        {
            var attackHitConfig = attackData.detectionEvent.AttackHitConfig;
            if (attackHitConfig != null)
            {
                if (attackHitConfig.HitAudioClip != null)
                {
                    AudioSystem.PlayOneShot(attackHitConfig.HitAudioClip, attackData.hitPoint);
                }

                if (attackHitConfig.HitEffectPrefab != null)
                {
                    if (false && !VfxEmitterHelper.EmitHitVfx(attackData.hitPoint, attackHitConfig.HitEffectPrefab, true))
                    {
                        Debug.Log("由Mono生成命中特效");
                        var effect = ProjectUtility.GetOrInstantiateGameObject(attackHitConfig.HitEffectPrefab, null);
                        effect.transform.position = attackData.hitPoint;
                        if (Camera.main != null)
                            effect.transform.LookAt(Camera.main.transform.position);
                        var ctrl = effect.GetComponent<EffectController>();
                        if (ctrl != null)
                            ctrl.Init();
                    }
                }
            }
        }

        public virtual void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
        }
        
        #endregion
    }
}
