using System;
using System.Collections;
using System.Collections.Generic;
using JKFrame;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Player.Animation
{
    public class AnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private bool logAnimationState = true;
        private PlayableGraph graph;
        private AnimationMixerPlayable mixer;

        private AnimationNodeBase previousNode; // 上一个动画节点
        private AnimationNodeBase currentNode;  // 当前动画节点
        private int inputPort0 = 0;
        private int inputPort1 = 1;
        
        
        private Coroutine transitionCoroutine;

        private float speed;
        public float Speed
        {
            get => speed;
            set
            {
                speed = value;
                currentNode.SetSpeed(value);
            }
        }
        
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
        /// 播放单个动画
        /// </summary>
        public void PlaySingleAnimation(AnimationClip clip, float speed = 1, bool refreshAnimation = false,
            float transitionFixedTime = 0.25f)
        {
            SingleAnimationNode singleAnimationNode = null;
            // first play, no transition
            if (currentNode == null)
            {
                singleAnimationNode = PoolManager.Instance.GetObject<SingleAnimationNode>();
                singleAnimationNode.Init(graph, mixer, clip, speed, inputPort0);
                mixer.SetInputWeight(inputPort0, 1);
            }
            else
            {
                var preNode = currentNode as SingleAnimationNode;
                // 如果不需要刷新相同动画
                if (refreshAnimation == false && preNode != null && preNode.GetAnimationClip() == clip)
                    return;
                
                // 销毁掉当前可能被占用的节点
                DestroyNode(previousNode);
                singleAnimationNode = PoolManager.Instance.GetObject<SingleAnimationNode>();
                singleAnimationNode.Init(graph, mixer, clip, speed, inputPort1);
                previousNode = currentNode;
                StartTransitionAnimation(transitionFixedTime);
            }

            this.speed = speed;
            currentNode = singleAnimationNode;
            
            if (graph.IsPlaying() == false)
                graph.Play();

            LogCurrentState("PlaySingle");
        }
        
        /// <summary>
        /// 播放混合动画
        /// </summary>
        public void PlayBlendAnimation(List<AnimationClip> clips, float speed = 1, float transitionFixedTime = 0.25f)
        {
            BlendAnimationNode blendAnimationNode = PoolManager.Instance.GetObject<BlendAnimationNode>();

            // first play, no transition
            if (currentNode == null)
            {
                blendAnimationNode.Init(graph, mixer, clips, speed, inputPort0);
                mixer.SetInputWeight(inputPort0, 1);
            }
            else
            {
                DestroyNode(previousNode);
                blendAnimationNode.Init(graph, mixer, clips, speed, inputPort1);
                previousNode = currentNode;
                StartTransitionAnimation(transitionFixedTime);
            }
            
            this.speed = speed;
            currentNode = blendAnimationNode;
            if (graph.IsPlaying() == false)
                graph.Play();

            LogCurrentState("PlayBlendList");
        }

        public void PlayBlendAnimation(AnimationClip clip1, AnimationClip clip2, float speed = 1, float transitionFixedTime = 0.25f)
        {
            BlendAnimationNode blendAnimationNode = PoolManager.Instance.GetObject<BlendAnimationNode>();

            // first play, no transition
            if (currentNode == null)
            {
                blendAnimationNode.Init(graph, mixer, clip1, clip2, speed, inputPort0);
                mixer.SetInputWeight(inputPort0, 1);
            }
            else
            {
                DestroyNode(previousNode);
                blendAnimationNode.Init(graph, mixer, clip1, clip2, speed, inputPort1);
                previousNode = currentNode;
                StartTransitionAnimation(transitionFixedTime);
            }
            
            this.speed = speed;
            currentNode = blendAnimationNode;
            if (graph.IsPlaying() == false)
                graph.Play();

            LogCurrentState("PlayBlend");
        }
        
        public void SetBlendWeight(List<float> weights)
        {
            (currentNode as BlendAnimationNode)?.SetBlendWeight(weights);
        }

        public void SetBlendWeight(float clip1Weight)
        {
            (currentNode as BlendAnimationNode)?.SetBlendWeight(clip1Weight);
        }

        /// <summary>
        /// 开始过渡动画
        /// </summary>
        private void StartTransitionAnimation(float fixedTime)
        {
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(TransitionAnimation(fixedTime));
        }
        
        /// <summary>
        /// 动画过渡
        /// </summary>
        private IEnumerator TransitionAnimation(float fixedTime)
        {
            // 交换端口号
            var (lastInputPort0, lastInputPort1) = (inputPort0, inputPort1);
            (inputPort0, inputPort1) = (inputPort1, inputPort0);

            // 硬切判断
            if (fixedTime <= 0)
            {
                mixer.SetInputWeight(lastInputPort0, 0);
                mixer.SetInputWeight(lastInputPort1, 1);
                LogCurrentState("TransitionInstant");
                transitionCoroutine = null;
                yield break;
            }
            
            // 当前的权重
            float currentWeight = 1;
            while (currentWeight > 0)
            {
                currentWeight = Mathf.MoveTowards(currentWeight, 0, Time.deltaTime / fixedTime);
                mixer.SetInputWeight(lastInputPort0, currentWeight);
                mixer.SetInputWeight(lastInputPort1, 1 - currentWeight);
                
                yield return null;
            }

            mixer.SetInputWeight(lastInputPort0, 0);
            mixer.SetInputWeight(lastInputPort1, 1);
            LogCurrentState("TransitionEnd");
            transitionCoroutine = null;
        }
        
        public void DestroyNode(AnimationNodeBase node)
        {
            if (node != null)
            {
                graph.Disconnect(mixer, node.InputPort);
                node.PushPool();
            }
        }
        
        #region Root Motion
        private Action<Vector3, Quaternion> rootMotionAction;
        private void OnAnimatorMove()
        {
            rootMotionAction?.Invoke(animator.deltaPosition, animator.deltaRotation);
        }

        public void SetRootMotionAction(Action<Vector3, Quaternion> rootMotionAction)
        {
            this.rootMotionAction = rootMotionAction;
        }

        public void ClearRootMotionAction()
        {
            rootMotionAction = null;
        }
        #endregion
        
        #region 动画事件

        private readonly Dictionary<string, Action> eventDict = new();
        private void AnimationEvent(string eventName)
        {
            if (eventDict.TryGetValue(eventName, out var action))
            {
                action?.Invoke();
            }
        }

        public void AddAnimationEvent(string eventName, Action action)
        {
            if (eventDict.TryGetValue(eventName, out var _action))
            {
                _action += action;
            }
            else
            {
                eventDict.Add(eventName, action);
            }
        }

        public void RemoveAnimationEvent(string eventName)
        {
            eventDict.Remove(eventName);
        }
        
        public void RemoveAnimationEvent(string eventName, Action action)
        {
            if (eventDict.TryGetValue(eventName, out var _action))
            {
                _action -= action;
            }
        }

        public void ClearAllAnimationEvent()
        {
            eventDict.Clear();
        }
        
        #endregion

        private void LogCurrentState(string reason)
        {
            if (!logAnimationState)
                return;

            string nodeInfo;
            if (currentNode is SingleAnimationNode singleNode)
            {
                nodeInfo = $"Single({singleNode.GetClipName()})";
            }
            else if (currentNode is BlendAnimationNode blendNode)
            {
                int count = blendNode.GetBlendInputCount();
                var blendInfo = new System.Text.StringBuilder();
                for (int i = 0; i < count; i++)
                {
                    if (i > 0) blendInfo.Append(", ");
                    blendInfo.Append($"{blendNode.GetBlendClipName(i)}={blendNode.GetBlendWeight(i):0.###}");
                }
                nodeInfo = $"Blend({blendInfo})";
            }
            else
            {
                nodeInfo = "<none>";
            }

            float w0 = mixer.GetInputWeight(0);
            float w1 = mixer.GetInputWeight(1);
            float w2 = mixer.GetInputWeight(2);
            Debug.Log($"[Animation] {reason} node={nodeInfo} mixer(0={w0:0.###},1={w1:0.###},2={w2:0.###})");
        }
    }
}
