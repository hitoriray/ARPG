using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Player
{
    public class AnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip animationClip;
        private PlayableGraph playableGraph;

        private void Start()
        {
            // 1.创建图
            playableGraph = PlayableGraph.Create("Player Animation");
            
            // 2.设置图的时间更新模式
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            
            // 3.创建ClipPlayable，然后去包裹一个AnimationClip
            var clipPlayable = AnimationClipPlayable.Create(playableGraph, animationClip);
            
            // 4.创建Output
            var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);
            
            // 5.连接ClipPlayable和Output
            playableOutput.SetSourcePlayable(clipPlayable);
            
            // 6.播放playableGraph
            playableGraph.Play();
        }

        private void OnDestroy()
        {
            playableGraph.Destroy();
        }
    }
}
