using System;
using Config;
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

        public void Init(AnimationController animationController)
        {
            this.animationController = animationController;
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
        }
    }
}