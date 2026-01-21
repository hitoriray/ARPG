using System;
using System.Collections;
using Config;
using JKFrame;
using Player.Animation;
using UnityEngine;

namespace Player.Skill
{
    // 技能播放器
    public class SkillPlayer : MonoBehaviour
    {
        private AnimationController animationController;
        
        private SkillConfig skillConfig;
        private int currentFrameIndex;
        private float playerTotalTime;
        private float frameRate;
        private bool isPlaying = false;
        public bool IsPlaying => isPlaying;

        private Transform modelTransform;

        public void Init(AnimationController animationController, Transform modelTransform)
        {
            this.animationController = animationController;
            this.modelTransform = modelTransform;
        }
        
        private Action skillEndAction;
        private Action<Vector3, Quaternion> rootMotionAction;

        public void PlaySkill(SkillConfig skillConfig, Action skillEndAction, Action<Vector3, Quaternion> rootMotionAction = null)
        {
            this.skillConfig = skillConfig;
            currentFrameIndex = -1;
            frameRate = skillConfig.FrameRate;
            playerTotalTime = 0;
            isPlaying = true;
            this.skillEndAction = skillEndAction;
            this.rootMotionAction = rootMotionAction;
            TickSkill();
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
                if (targetFrameIndex >= skillConfig.FrameCount)
                {
                    isPlaying = false;
                    skillConfig = null;
                    if (rootMotionAction != null)
                    {
                        animationController.ClearRootMotionAction();
                    }
                    rootMotionAction = null;
                    skillEndAction?.Invoke();
                }
            }
        }

        private void TickSkill()
        {
            currentFrameIndex++;
            // 驱动动画
            if (animationController != null &&
                skillConfig.SkillAnimationData.FrameEventDict.TryGetValue(currentFrameIndex, out var animationEvent))
            {
                animationController.PlaySingleAnimation(animationEvent.AnimationClip, 1, true, animationEvent.TransitionTime);

                if (animationEvent.ApplyRootMotion)
                {
                    animationController.SetRootMotionAction(rootMotionAction);
                }
                else
                {
                    animationController.ClearRootMotionAction();
                }
            }
            // 驱动音效
            foreach (var audioEvent in skillConfig.SkillAudioData.FrameData)
            {
                if (audioEvent.AudioClip != null && audioEvent.FrameIndex == currentFrameIndex)
                {
                    // 播放音效，从头播放
                    AudioManager.Instance.PlayOneShot(audioEvent.AudioClip, transform.position, audioEvent.Volume);
                }
            }
            // 驱动特效
            foreach (var effectEvent in skillConfig.SkillEffectData.FrameData)
            {
                if (effectEvent.Prefab != null && effectEvent.FrameIndex == currentFrameIndex)
                {
                    // 实例化特效
                    var effectObj = PoolManager.Instance.GetGameObject(effectEvent.Prefab.name);
                    if (effectObj != null)
                    {
                        effectObj = GameObject.Instantiate(effectEvent.Prefab);
                        effectObj.name = effectEvent.Prefab.name;
                    }

                    effectObj.transform.position = modelTransform.TransformPoint(effectEvent.Position);
                    effectObj.transform.rotation = Quaternion.Euler(modelTransform.eulerAngles + effectEvent.Rotation);
                    effectObj.transform.localScale = effectEvent.Scale;
                    if (effectEvent.AutoDestroy)
                    {
                        StartCoroutine(AutoDestructEffectGameObject(effectEvent.Duration, effectObj));
                    }
                }
            }
        }

        private IEnumerator AutoDestructEffectGameObject(float time, GameObject obj)
        {
            yield return new WaitForSeconds(time);
            obj.JKGameObjectPushPool();
        }
    }
}