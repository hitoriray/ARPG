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
        private bool isFirstPlay = true;
        private bool isPlayingClip1 = true; // 当前是否是播放的clip1
        private Coroutine transitionCoroutine;
        
        public void Init()
        {
            // 创建图
            graph = PlayableGraph.Create("Player Animation");
            
            // 设置图的时间更新模式
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            
            // 创建混合器
            mixer = AnimationMixerPlayable.Create(graph, 2);
            
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

        public void PlayAnimation(AnimationClip clip, float fixedTime = 0.25f)
        {
            if (isFirstPlay)
            {
                // 如果是第一次播放，不存在过渡情况，直接连接
                clipPlayable1 = AnimationClipPlayable.Create(graph, clip);
                graph.Connect(clipPlayable1, 0, mixer, 0);
                mixer.SetInputWeight(0, 1);
                isFirstPlay = false;
                isPlayingClip1 = true;
            }
            else
            {
                // 1 -> 2
                if (isPlayingClip1)
                {
                    // 解除已有的2和mixer的连接
                    graph.Disconnect(mixer, 1);
                    // 给2设置新的clip动画
                    clipPlayable2 = AnimationClipPlayable.Create(graph, clip);
                    graph.Connect(clipPlayable2, 0, mixer, 1);
                    // mixer.SetInputWeight(0, 0);
                    // mixer.SetInputWeight(1, 1);
                }
                // 2 -> 1
                else
                {
                    // 解除已有的1和mixer的连接
                    graph.Disconnect(mixer, 0);
                    // 给1设置新的clip动画
                    clipPlayable1 = AnimationClipPlayable.Create(graph, clip);
                    graph.Connect(clipPlayable1, 0, mixer, 0);
                    // mixer.SetInputWeight(1, 0);
                    // mixer.SetInputWeight(0, 1);
                }
                // 设置过渡
                if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
                transitionCoroutine = StartCoroutine(TransitionAnimation(fixedTime, isPlayingClip1));
                
                isPlayingClip1 = !isPlayingClip1;
            }
            
            if (graph.IsPlaying() == false)
            {
                graph.Play();
            }
        }

        /// <summary>
        /// 动画过渡
        /// </summary>
        private IEnumerator TransitionAnimation(float fixedTime, bool isCurrentClip1)
        {
            // 当前的权重
            float currentWeight = 1;
            float speed = 1/fixedTime;

            if (isCurrentClip1)
            {
                mixer.SetInputWeight(0, 0);
            }
            else
            {
                mixer.SetInputWeight(1, 0);
            }
            
            while (currentWeight > 0)
            {
                yield return null;
                currentWeight -= Mathf.Clamp01(currentWeight - Time.deltaTime * speed);
                // 当前在播放1，新动画是2，所以是从1->2
                if (isCurrentClip1)
                {
                    mixer.SetInputWeight(0, currentWeight); // 减少
                    mixer.SetInputWeight(1, 1 - currentWeight); // 增加
                }
                else
                {
                    mixer.SetInputWeight(0, 1 - currentWeight); // 增加
                    mixer.SetInputWeight(1, currentWeight); // 减少
                }
            }
        }
    }
}
