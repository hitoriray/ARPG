using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Player
{
    public class AnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        private PlayableGraph graph;
        private AnimationMixerPlayable mixer;
        private AnimationClipPlayable clipPlayable1;
        private AnimationClipPlayable clipPlayable2;
        private AnimationMixerPlayable blendMixer;
        private List<AnimationClipPlayable> blendClipPlayables = new(10);
        private bool isCurrentBlendAnim;
        
        private bool isPlayingClip1 = true; // 当前是否是播放的clip1
        private Coroutine transitionCoroutine;
        
        #region 生命周期
        public void Init()
        {
            // 创建图
            graph = PlayableGraph.Create("Player Animation");
            
            // 设置图的时间更新模式
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            
            // 创建混合器
            mixer = AnimationMixerPlayable.Create(graph, 3);
            
            // 创建Output
            AnimationPlayableOutput playableOutput = AnimationPlayableOutput.Create(graph, "Animation", animator);
            
            // 连接混合器和Output
            playableOutput.SetSourcePlayable(mixer);
        }

        private void OnDestroy()
        {
            graph.Destroy();
        }

        private void OnDisable()
        {
            graph.Stop();
        }
        
        #endregion

        /// <summary>
        /// 播放动画
        /// </summary>
        public void PlayAnimation(AnimationClip clip, float speed = 1, bool refreshAnimation = false, float transitionFixedTime = 0.25f)
        {
            if (clipPlayable1.Equals(default))
            {
                // 如果是第一次播放，不存在过渡情况，直接连接
                clipPlayable1 = AnimationClipPlayable.Create(graph, clip);
                clipPlayable1.SetSpeed(speed);
                graph.Connect(clipPlayable1, 0, mixer, 0);
                mixer.SetInputWeight(0, 1);
                isPlayingClip1 = true;
            }
            else
            {
                // 设置过渡
                if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);

                // 从blend动画切换到普通动画
                if (isCurrentBlendAnim)
                {
                    // 解除已有的1和mixer的连接
                    graph.Disconnect(mixer, 0);
                    // 给1设置新的clip动画
                    clipPlayable1 = AnimationClipPlayable.Create(graph, clip);
                    clipPlayable1.SetSpeed(speed);
                    graph.Connect(clipPlayable1, 0, mixer, 0);
                    transitionCoroutine = StartCoroutine(TransitionAnimation(2, 0, transitionFixedTime));
                    isPlayingClip1 = true;
                    return;
                }
                
                // 1 -> 2
                if (isPlayingClip1)
                {
                    // 避免重复播放同一个动画
                    if (refreshAnimation && clipPlayable1.GetAnimationClip() == clip)
                        return;
                    // 解除已有的2和mixer的连接
                    graph.Disconnect(mixer, 1);
                    // 给2设置新的clip动画
                    clipPlayable2 = AnimationClipPlayable.Create(graph, clip);
                    clipPlayable2.SetSpeed(speed);
                    graph.Connect(clipPlayable2, 0, mixer, 1);
                    transitionCoroutine = StartCoroutine(TransitionAnimation(0, 1, transitionFixedTime));
                }
                // 2 -> 1
                else
                {
                    // 避免重复播放同一个动画
                    if (refreshAnimation && clipPlayable2.GetAnimationClip() == clip)
                        return;
                    // 解除已有的1和mixer的连接
                    graph.Disconnect(mixer, 0);
                    // 给1设置新的clip动画
                    clipPlayable1 = AnimationClipPlayable.Create(graph, clip);
                    clipPlayable1.SetSpeed(speed);
                    graph.Connect(clipPlayable1, 0, mixer, 0);
                    transitionCoroutine = StartCoroutine(TransitionAnimation(1, 0, transitionFixedTime));
                }
                
                isPlayingClip1 = !isPlayingClip1;
            }
            
            if (graph.IsPlaying() == false)
            {
                graph.Play();
            }
        }
        
        /// <summary>
        /// 播放混合动画
        /// </summary>
        public void PlayBlendAnimation(List<AnimationClip> clips, float speed = 1, float transitionFixedTime = 0.25f)
        {
            ResetBlend(clips.Count);
            isCurrentBlendAnim = true;
            for (int i = 0; i < clips.Count; i++)
            {
                CreateAndConnectBlendPlayable(clips[i], i, speed);
            }
            
            // 如果是第一次播放，不存在过渡情况，直接连接
            if (clipPlayable1.Equals(default))
                return;

            // 设置过渡
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(isPlayingClip1 
                ? TransitionAnimation(0, 2, transitionFixedTime) 
                : TransitionAnimation(1, 2, transitionFixedTime));
        }

        public void PlayBlendAnimation(AnimationClip clip1, AnimationClip clip2, float speed = 1, float transitionFixedTime = 0.25f)
        {
            ResetBlend(2);
            isCurrentBlendAnim = true;
            CreateAndConnectBlendPlayable(clip1, 0, speed);
            CreateAndConnectBlendPlayable(clip2, 1, speed);
            
            // 如果是第一次播放，不存在过渡情况，直接连接
            if (clipPlayable1.Equals(default))
                return;

            // 设置过渡
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(isPlayingClip1 
                ? TransitionAnimation(0, 2, transitionFixedTime) 
                : TransitionAnimation(1, 2, transitionFixedTime));
        }
        
        public void SetBlendWeight(List<float> weights)
        {
            for (int i = 0; i < weights.Count; i++)
            {
                blendMixer.SetInputWeight(i, weights[i]);
            }
        }

        public void SetBlendWeight(float clip1Weight)
        {
            blendMixer.SetInputWeight(0, clip1Weight);
            blendMixer.SetInputWeight(1, 1 - clip1Weight);
        }

        public void ResetBlend(int animationCount)
        {
            // 创建混合器
            graph.Disconnect(mixer, 2);
            blendMixer =  AnimationMixerPlayable.Create(graph, animationCount);
            graph.Connect(blendMixer, 0, mixer, 2);
            blendClipPlayables.Clear();
        }
        
        /// <summary>
        /// 动画过渡
        /// </summary>
        private IEnumerator TransitionAnimation(int inputIndex1, int inputIndex2, float fixedTime)
        {
            if (fixedTime == 0)
            {
                mixer.SetInputWeight(inputIndex1, 0);
                mixer.SetInputWeight(inputIndex2, 1);
            }
            
            // 当前的权重
            float currentWeight = 1;
            float speed = 1 / fixedTime;
            while (currentWeight > 0)
            {
                currentWeight -= Mathf.Clamp01(currentWeight - Time.deltaTime * speed);
                mixer.SetInputWeight(inputIndex1, currentWeight);
                mixer.SetInputWeight(inputIndex2, 1 - currentWeight);
                
                yield return null;
            }
        }

        private AnimationClipPlayable CreateAndConnectBlendPlayable(AnimationClip clip, int index, float speed)
        {
            var clipPlayable = AnimationClipPlayable.Create(graph, clip);
            clipPlayable.SetSpeed(speed);
            blendClipPlayables.Add(clipPlayable);
            graph.Connect(clipPlayable, 0, blendMixer, index);
            return clipPlayable;
        }
    }
}
