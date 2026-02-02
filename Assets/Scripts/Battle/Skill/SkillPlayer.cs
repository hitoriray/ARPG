using System.Collections;
using System.Collections.Generic;
using Config;
using Battle.ECS;
using Battle.ECS.Core.Helper;
using Battle.ECS.View.Helper;
using JKFrame;
using Player.Animation;
using Scene;
using Sirenix.OdinInspector;
using Skill.Behaviour;
using UnityEngine;

namespace Skill
{
    /// <summary>
    /// 技能播放器
    /// </summary>
    public class SkillPlayer : SerializedMonoBehaviour
    {
        private AnimationController animationController;
        
        private SkillClip skillClip;
        private int currentFrameIndex;
        private float playerTotalTime;
        private float frameRate;
        private bool isPlaying = false;
        public bool IsPlaying => isPlaying;

        private ICharacter owner;
        private Transform modelTransform;
        public Transform ModelTransform => modelTransform;
        
        public LayerMask attackDetectionLayer; // 攻击检测的Layer

        public void Init(ICharacter owner, AnimationController animationController, Transform modelTransform)
        {
            this.owner = owner;
            this.animationController = animationController;
            this.modelTransform = modelTransform;
            WeaponController[] weaponControllers = transform.GetComponentsInChildren<WeaponController>();
            foreach (var weaponController in weaponControllers)
            {
                weaponDict.Add(weaponController.WeaponName, weaponController);
            }

            foreach (var skillWeapon in weaponDict.Values)
            {
                skillWeapon.Init(attackDetectionLayer, OnWeaponDetection);
            }
        }
        
        #region 武器

        [SerializeField] private Dictionary<string, WeaponController> weaponDict = new();
        public Dictionary<string, WeaponController> WeaponDict => weaponDict;

        private void OnWeaponDetection(IHitTarget other, AttackData attackData)
        {
            if (GameSceneManager.Instance.isEcs)
            {
                bool ok = WeaponHitEmitterHelper.Emit(skillBehaviour, other, attackData);
                if (ok) return;
            }
            RayDebug.Log("由Mono触发WeaponDetection");
            skillBehaviour.OnAttackDetection(other, attackData);
        }

        #endregion
        
        private SkillBehaviourBase skillBehaviour;

        public void StartPlaySkillBehaviour(SkillBehaviourBase skillBehaviour)
        {
            this.skillBehaviour = skillBehaviour;
        }
        
        /// <summary>
        /// 播放技能片段
        /// </summary>
        public void PlaySkillClip(SkillClip skillClip)
        {
            this.skillClip = skillClip;
            currentFrameIndex = -1;
            frameRate = skillClip.FrameRate;
            playerTotalTime = 0;
            isPlaying = true;
            TickSkill();
        }

        private void Clean()
        {
            skillClip = null;
        }

        private void Update()
        {
            if (IsPlaying)
            {
                playerTotalTime += Time.deltaTime;
                int targetFrameIndex = (int)(playerTotalTime * frameRate);
                // 防止一帧延迟过大，追帧
                while (currentFrameIndex < targetFrameIndex)
                {
                    // 驱动一次技能
                    TickSkill();
                }
                
                // 如果到达最后一帧，技能结束
                if (targetFrameIndex >= skillClip.FrameCount)
                {
                    isPlaying = false;
                    skillBehaviour.OnSkillClipEnd();
                    Clean();
                }
            }
        }

        private void TickSkill()
        {
            currentFrameIndex++;
            skillBehaviour.OnTickSkill(currentFrameIndex);
            TickSkillCustomEvent();
            TickSkillAnimationEvent();
            TickSkillAudioEvent();
            TickSkillEffectEvent();
            TickSkillAttackDetectionEvent();
        }

        /// <summary>
        /// 驱动自定义事件
        /// </summary>
        private void TickSkillCustomEvent()
        {
            if (skillClip.SkillCustomEventData.FrameData.TryGetValue(currentFrameIndex, out var customEvent))
            {
                customEvent = skillBehaviour.BeforeSkillCustomEvent(customEvent);
                if (customEvent != null)
                {
                    skillBehaviour.AfterSkillCustomEvent(customEvent);
                }
            }
        }
        
        /// <summary>
        /// 驱动动画
        /// </summary>
        private void TickSkillAnimationEvent()
        {
            if (animationController != null && skillClip.SkillAnimationData.FrameData.TryGetValue(currentFrameIndex, out var animationEvent))
            {
                animationEvent = skillBehaviour.BeforeSkillAnimationEvent(animationEvent);
                if (animationEvent != null)
                {
                    animationController.PlaySingleAnimation(animationEvent.AnimationClip, 1, true, animationEvent.TransitionTime);
                    if (animationEvent.ApplyRootMotion)
                    {
                        animationController.SetRootMotionAction(skillBehaviour.OnRootMotion);
                    }
                    else
                    {
                        animationController.ClearRootMotionAction();
                    }
                    skillBehaviour.AfterSkillAnimationEvent(animationEvent);
                }
            }
        }
        
        /// <summary>
        /// 驱动音效
        /// </summary>
        private void TickSkillAudioEvent()
        {
            for (int i = 0; i < skillClip.SkillAudioData.FrameData.Count; i++)
            {
                var audioEvent = skillClip.SkillAudioData.FrameData[i];
                audioEvent = skillBehaviour.BeforeSkillAudioEvent(audioEvent);
                if (audioEvent != null)
                {
                    if (audioEvent.AudioClip != null && audioEvent.FrameIndex == currentFrameIndex)
                    {
                        // 播放音效，从头播放
                        AudioSystem.PlayOneShot(audioEvent.AudioClip, transform.position, false, audioEvent.Volume);
                    }
                    skillBehaviour.AfterSkillAudioEvent(audioEvent);
                }
            }
        }
        
        /// <summary>
        /// 驱动特效
        /// </summary>
        private void TickSkillEffectEvent()
        {
            for (int i = 0; i < skillClip.SkillEffectData.FrameData.Count; i++)
            {
                var effectEvent = skillClip.SkillEffectData.FrameData[i];
                effectEvent = skillBehaviour.BeforeSkillEffectEvent(effectEvent);
                if (effectEvent != null)
                {
                    if (effectEvent.Prefab != null && effectEvent.FrameIndex == currentFrameIndex)
                    {
                        // 交给ECS生成特效（若ECS未就绪则回落到原逻辑）
                        bool success = false;
                        if (GameSceneManager.Instance.isEcs)
                        {
                            success = VfxEmitterHelper.EmitSkillVfx(modelTransform, effectEvent, skillClip.FrameRate);
                        }
                        if (!success)
                        {
                            RayDebug.Log("由Mono生成技能特效");
                            var effectObj = PoolSystem.GetGameObject(effectEvent.Prefab.name);
                            if (effectObj == null)
                            {
                                effectObj = GameObject.Instantiate(effectEvent.Prefab);
                                effectObj.name = effectEvent.Prefab.name;
                            }

                            effectObj.transform.position = modelTransform.TransformPoint(effectEvent.Position);
                            effectObj.transform.rotation =
                                Quaternion.Euler(modelTransform.eulerAngles + effectEvent.Rotation);
                            effectObj.transform.localScale = effectEvent.Scale;
                            if (effectEvent.AutoDestroy)
                            {
                                StartCoroutine(
                                    AutoDestructEffectGameObject((float)effectEvent.Duration / skillClip.FrameRate,
                                        effectObj));
                            }
                        }
                    }
                    skillBehaviour.AfterSkillEffectEvent(effectEvent);
                }
            }
        }
        
        /// <summary>
        /// 驱动伤害检测
        /// </summary>
        private void TickSkillAttackDetectionEvent()
        {
#if UNITY_EDITOR
            if (drawAttackDetectionGizmos)
            {
                currentAttackDetectionList.Clear();
            }
#endif
            for (int i = 0; i < skillClip.SkillAttackDetectionData.FrameData.Count; i++)
            {
                var detectionEvent = skillClip.SkillAttackDetectionData.FrameData[i];
                detectionEvent = skillBehaviour.BeforeSkillAttackDetectionEvent(detectionEvent);
                if (detectionEvent != null)
                {
                    var detectionType = detectionEvent.GetAttackDetectionType();
                    // 只有武器需要关注开头和结尾帧
                    if (detectionType == AttackDetectionType.Weapon)
                    {
                        if (detectionEvent.FrameIndex == currentFrameIndex)
                        {
                            // 驱动武器开启
                            var weaponDetectionData = (WeaponDetectionData)detectionEvent.AttackDetectionData;
                            if (weaponDict.TryGetValue(weaponDetectionData.WeaponName, out var weapon))
                            {
                                AttackData attackData = new AttackData()
                                {
                                    detectionEvent = detectionEvent,
                                    source = owner,
                                    attackValue = owner.GetAttackValue(detectionEvent),
                                };
                                weapon.StartDetection(attackData);
                            }
                            else
                            {
                                RayDebug.Error($"没有指定的武器: {weaponDetectionData.WeaponName}");
                            }
                        }

                        if (currentFrameIndex == detectionEvent.FrameIndex + detectionEvent.DurationFrame)
                        {
                            // 驱动武器关闭
                            var weaponDetectionData = (WeaponDetectionData)detectionEvent.AttackDetectionData;
                            if (weaponDict.TryGetValue(weaponDetectionData.WeaponName, out var weapon))
                            {
                                weapon.StopDetection();
                            }
                            else
                            {
                                RayDebug.Error("没有指定的武器");
                            }
                        }
                    }
                    // 其他形状每一帧都做检测
                    else
                    {
                        // 当前帧在范围内
                        if (currentFrameIndex >= detectionEvent.FrameIndex &&
                            currentFrameIndex <= detectionEvent.FrameIndex + detectionEvent.DurationFrame)
                        {
                            bool success = false;
                            if (GameSceneManager.Instance.isEcs)
                            {
                                success = AttackDetectionEmitterHelper.Emit(modelTransform, detectionEvent, skillBehaviour, owner, attackDetectionLayer);
                            }
                            if (!success)
                            {
                                RayDebug.Log("由Mono触发ShapeDetection");
                                var colliders = SkillAttackDetectionHelper.ShapeDetection(modelTransform,
                                    detectionEvent.AttackDetectionData, detectionType, attackDetectionLayer);
                                if (colliders == null)
                                    break;
                                foreach (var col in colliders)
                                {
                                    if (col != null)
                                    {
                                        IHitTarget hitTarget = col.GetComponentInChildren<IHitTarget>();
                                        if (hitTarget != null)
                                        {
                                            Vector3 pos = ((ShapeDetectionDataBase)detectionEvent.AttackDetectionData).Position;
                                            AttackData attackData = new AttackData()
                                            {
                                                detectionEvent = detectionEvent,
                                                source = owner,
                                                attackValue = owner.GetAttackValue(detectionEvent),
                                                // TODO: hitPoint = col.ClosestPoint(pos),
                                                hitPoint = pos,
                                            };
                                            skillBehaviour.OnAttackDetection(hitTarget, attackData);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    skillBehaviour.AfterSkillAttackDetectionEvent(detectionEvent);
#if UNITY_EDITOR
                    if (drawAttackDetectionGizmos)
                    {
                        // 当前帧在范围内
                        if (currentFrameIndex >= detectionEvent.FrameIndex &&
                            currentFrameIndex <= detectionEvent.FrameIndex + detectionEvent.DurationFrame)
                        {
                            currentAttackDetectionList.Add(detectionEvent);
                        }
                    }
#endif
                }
            }
        }
        
        private IEnumerator AutoDestructEffectGameObject(float time, GameObject obj)
        {
            yield return new WaitForSeconds(time);
            if (obj != null) obj.GameObjectPushPool();
        }
        
        #region Editor
#if UNITY_EDITOR
        [SerializeField] private bool drawAttackDetectionGizmos;
        private List<SkillAttackDetectionEvent> currentAttackDetectionList = new();
        private void OnDrawGizmos()
        {
            if (drawAttackDetectionGizmos && currentAttackDetectionList.Count > 0)
            {
                foreach (var detectionEvent in currentAttackDetectionList)
                {
                    SkillGizmosTool.DrawDetectionGizmos(detectionEvent, this);
                }
            }
        }
#endif
        #endregion
    }
}
