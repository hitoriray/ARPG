using System.Collections.Generic;
using Battle.ECS.View.Helper;
using Config;
using Data;
using JKFrame;
using UnityEngine;

namespace Skill.Behaviour
{
    public abstract class SkillBehaviourBase : SkillBehaviourLogicBase
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

        protected virtual void UpdateSkillSlot()
        {
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
            skillBrain.SetCanInterruptFlag(false);  // 释放技能时重置打断标志
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
        
        /// <summary>
        /// 技能被打断时调用（如移动打断）
        /// 子类可重写以处理打断时的清理逻辑
        /// </summary>
        public virtual void OnInterrupt()
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
            switch (evt.EventType)
            {
                case SkillEventType.CanSkillRelease:
                    skillBrain.SetCanReleaseFlag(true);
                    break;
                case SkillEventType.CanRotate:
                    canRotate = true;
                    break;
                case SkillEventType.CannotRotate:
                    canRotate = false;
                    break;
                case SkillEventType.AddBuff:
                    owner.AddBuff((BuffConfig)evt.ObjectArg, evt.IntArg);
                    break;
                case SkillEventType.CreateWeapon:
                    owner.CreateWeapon(evt.IntArg, evt.ObjectArg as GameObject);
                    break;
                case SkillEventType.DestroyWeapon:
                    owner.DestroyWeapon(evt.IntArg);
                    break;
                case SkillEventType.CanInterrupt:
                    skillBrain.SetCanInterruptFlag(true);   // 开启移动打断
                    break;
                case SkillEventType.BreakCombo:
                    skillBrain.ResetCombo();                // 立即重置连段
                    break;
            }
        }

        public virtual void OnAttackDetection(IHitTarget hitTarget, AttackData attackData)
        {
            RayDebug.Info($"[OnAttackDetection] 检测到目标: {hitTarget.GetType().Name}, 来源: {attackData.source?.GetType().Name}, 攻击值: {attackData.attackValue}");
            // 避免重复命中
            if (hitTargets.Add(hitTarget))
            {
                OnHitTarget(hitTarget, attackData);
            }
            else
            {
                RayDebug.Info($"[OnAttackDetection] 目标已在命中列表中，跳过重复命中: {hitTarget.GetType().Name}");
            }
        }

        public virtual void OnHitTarget(IHitTarget hitTarget, AttackData attackData)
        {
            RayDebug.Info($"[OnHitTarget] 命中目标: {hitTarget.GetType().Name}, hitPoint: {attackData.hitPoint}, 攻击值: {attackData.attackValue}");
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
                    bool success = VfxEmitterHelper.EmitHitVfx(attackData.hitPoint, attackHitConfig.HitEffectPrefab, true);
                    if (!success)
                    {
                        RayDebug.Log("由Mono生成命中特效");
                        var effect = ProjectUtility.GetOrInstantiateGameObject(attackHitConfig.HitEffectPrefab, null);
                        effect.transform.position = attackData.hitPoint;
                        if (Camera.main != null)
                            effect.transform.LookAt(Camera.main.transform.position);
                        var effectController = effect.GetComponent<EffectController>();
                        if (effectController == null)
                        {
                            effectController = effect.AddComponent<EffectController>();
                            effectController.destroyTime = 3f;
                        }
                        effectController.Init();
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
