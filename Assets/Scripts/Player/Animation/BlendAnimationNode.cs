using System.Collections.Generic;
using JKFrame;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Player.Animation
{
    [Pool]
    public class BlendAnimationNode : AnimationNodeBase
    {
        private AnimationMixerPlayable blendMixer;
        private List<AnimationClipPlayable> blendClipPlayables = new(10);

        public void Init(PlayableGraph graph, AnimationMixerPlayable outputMixer, List<AnimationClip> clips,
            float speed, int inputPort)
        {
            blendMixer = AnimationMixerPlayable.Create(graph, clips.Count);
            InputPort = inputPort;
            graph.Connect(blendMixer, 0, outputMixer, inputPort);
            for (int i = 0; i < clips.Count; i++)
            {
                CreateAndConnectBlendPlayable(graph, clips[i], i, speed);
            }
        }

        public void Init(PlayableGraph graph, AnimationMixerPlayable outputMixer, AnimationClip clip1,
            AnimationClip clip2,
            float speed, int inputPort)
        {
            blendMixer = AnimationMixerPlayable.Create(graph, 2);
            InputPort = inputPort;
            graph.Connect(blendMixer, 0, outputMixer, inputPort);
            CreateAndConnectBlendPlayable(graph, clip1, 0, speed);
            CreateAndConnectBlendPlayable(graph, clip2, 1, speed);
        }

        private AnimationClipPlayable CreateAndConnectBlendPlayable(PlayableGraph graph, AnimationClip clip, int index,
            float speed)
        {
            var clipPlayable = AnimationClipPlayable.Create(graph, clip);
            clipPlayable.SetSpeed(speed);
            blendClipPlayables.Add(clipPlayable);
            graph.Connect(clipPlayable, 0, blendMixer, index);
            return clipPlayable;
        }

        public override void SetSpeed(float speed)
        {
            foreach (var clipPlayable in blendClipPlayables)
            {
                clipPlayable.SetSpeed(speed);
            }
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


        public override void PushPool()
        {
            blendClipPlayables.Clear();
            base.PushPool();
        }
    }
}