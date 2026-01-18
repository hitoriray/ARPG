using System.Collections.Generic;
using System.Linq;
using Config;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class AnimationTrack : SkillTrackBase
    {
        public override string MenuAssetPath => "Assets/SkillEditor/Editor/Track/AnimationTrack/AnimationTrackMenu.uxml";
        public override string TrackAssetPath => "Assets/SkillEditor/Editor/Track/AnimationTrack/AnimationTrackContent.uxml";

        private Dictionary<int, AnimationTrackItem> trackItemDict = new();

        public SkillAnimationData AnimationData => SkillEditorWindow.Instance.SkillConfig.SkillAnimationData;
        
        public override void Init(VisualElement menuParent, VisualElement trackParent, float frameWidth)
        {
            base.Init(menuParent, trackParent, frameWidth);
            track.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            track.RegisterCallback<DragExitedEvent>(OnDragExited);
            ResetView();
        }

        public override void ResetView(float frameWidth)
        {
            base.ResetView(frameWidth);
            
            // 销毁当前已有的
            foreach (var (_, trackItem) in trackItemDict)
            {
                track.Remove(trackItem.Root);
            }
            trackItemDict.Clear();

            if (SkillEditorWindow.Instance.SkillConfig == null)
                return;
            // 根据数据绘制TrackItem
            foreach (var (startFrameIndex, animationEvent) in AnimationData.FrameEventDict)
            {
               CreateAnimationTrackItem(startFrameIndex, animationEvent);
            }
        }

        private void CreateAnimationTrackItem(int startFrameIndex, SkillAnimationEvent animationEvent)
        {
            AnimationTrackItem trackItem = new();
            trackItem.Init(this, track, startFrameIndex, frameWidth, animationEvent);
            trackItemDict.Add(startFrameIndex, trackItem);
        }

        #region 拖拽资源
        private void OnDragUpdate(DragUpdatedEvent evt)
        {
            var objs = DragAndDrop.objectReferences;
            AnimationClip clip = objs[0] as AnimationClip;
            if (clip != null)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }
        }
        
        private void OnDragExited(DragExitedEvent evt)
        {
            var objs = DragAndDrop.objectReferences;
            AnimationClip clip = objs[0] as AnimationClip;
            if (clip != null)
            {
                int selectFrameIndex = SkillEditorWindow.Instance.GetFrameIndexByPos(evt.localMousePosition.x);
                // 检查选中帧不在任何已有的的TrackItem之间
                bool canPlace = true;
                int durationFrame = -1; // -1表示可以用原本的 AnimationClip 的持续时间
                int clipFrameCount = (int)(clip.length * clip.frameRate);
                int nextTrackItem = -1;
                int currentOffset = int.MaxValue;

                foreach (var (startIndex, animationEvent) in AnimationData.FrameEventDict)
                {
                    // 不允许选中帧在TrackItem之间（动画事件的起点到终点之间）
                    if (selectFrameIndex > startIndex && selectFrameIndex < startIndex + animationEvent.DurationFrame)
                    {
                        canPlace = false;
                        break;
                    }
                    // 找到右侧最近的TrackItem
                    if (startIndex > selectFrameIndex)
                    {
                        int tempOffset = startIndex - selectFrameIndex;
                        if (tempOffset < currentOffset)
                        {
                            currentOffset = tempOffset;
                            nextTrackItem = startIndex;
                        }
                    }
                }
                
                // 实际的放置
                if (canPlace)
                {
                    // 如果右边有其他的TrackItem，要考虑Track不能重叠的问题
                    if (nextTrackItem != -1)
                    {
                        int offset = clipFrameCount - currentOffset;
                        if (offset < 0) durationFrame = clipFrameCount;
                        else durationFrame = currentOffset;
                    }
                    else
                    {
                        durationFrame = clipFrameCount;
                    }

                    // 构建动画事件数据
                    SkillAnimationEvent animationEvent = new()
                    {
                        AnimationClip = clip,
                        DurationFrame = durationFrame,
                        TransitionTime = 0.25f
                    };

                    // 保存新增的动画数据
                    AnimationData.FrameEventDict.Add(selectFrameIndex, animationEvent);
                    SkillEditorWindow.Instance.SaveSkillConfig();
                    
                    // 创建一个新的Item
                    CreateAnimationTrackItem(selectFrameIndex, animationEvent);
                }
            }
        }
        #endregion
        
        public bool CheckFrameIndexOnDrag(int targetIndex, int selfIndex, bool isLeft)
        {
            foreach (var (startIndex, animationEvent) in AnimationData.FrameEventDict)
            {
                // 规避拖拽时考虑自身
                if (startIndex == selfIndex)
                    continue;
                
                // 向左移动 && 原先在其右边 && 目标没有重叠
                if (isLeft && selfIndex > startIndex && targetIndex < startIndex + animationEvent.DurationFrame)
                {
                    return false;
                }
                // 向右移动 && 原先在其左边 && 目标没有重叠
                else if (isLeft == false && selfIndex < startIndex && targetIndex > startIndex)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 将oldIndex的数据变为newIndex，其实就是修改skillConfig中字典的索引
        /// </summary>
        /// <param name="oldIndex"></param>
        /// <param name="newIndex"></param>
        public void SetFrameIndex(int oldIndex, int newIndex)
        {
            if (AnimationData.FrameEventDict.Remove(oldIndex, out var animationEvent))
            {
                AnimationData.FrameEventDict.Add(newIndex, animationEvent);
                trackItemDict.Remove(oldIndex, out var animationTrackItem);
                trackItemDict.Add(newIndex, animationTrackItem);
                SkillEditorWindow.Instance.SaveSkillConfig();
            }
        }

        public override void TickView(int frameIndex)
        {
            GameObject previewGameObject = SkillEditorWindow.Instance.CurrentPreviewCharacterObj;
            Animator animator = previewGameObject.GetComponent<Animator>();
            var frameData = AnimationData.FrameEventDict;
            
            #region 关于根运动的计算

            SortedDictionary<int, SkillAnimationEvent> sortedFrameData = new(frameData);
            int[] keys = sortedFrameData.Keys.ToArray();
            Vector3 rootMotionTotalPos = Vector3.zero;
            for (int i = 0; i < keys.Length; i++)
            {
                int key = keys[i];
                var animationEvt = sortedFrameData[key];
                // 只考虑根运动配置的动画
                if (animationEvt.ApplyRootMotion == false)
                    continue;

                int nextKeyFrame = 0;
                if (i + 1 < keys.Length)
                {
                    nextKeyFrame = keys[i + 1];
                }
                // 最后一个动画
                else
                {
                    nextKeyFrame = SkillEditorWindow.Instance.SkillConfig.FrameCount;
                }

                bool isBreak = false; // 标记是否是最后一次采样
                if (nextKeyFrame > frameIndex)
                {
                    nextKeyFrame = frameIndex;
                    isBreak = true;
                }
                // 持续的帧数 = 下一个动画的帧数 - 当前动画的开始时间
                int durationFrameCount = nextKeyFrame - key;
                if (durationFrameCount > 0)
                {
                    // 获取动画资源的总帧数
                    var clipFrameCnt = animationEvt.AnimationClip.length *
                                         SkillEditorWindow.Instance.SkillConfig.FrameRate;
                    // 计算当前的播放进度
                    var totalProgress = durationFrameCount / clipFrameCnt;
                    // 播放次数
                    int playTimes = 0;
                    // 最终不完整的一次播放的进度
                    float lastProgress = 0;
                    // 只有循环动画才需要采样多次
                    if (animationEvt.AnimationClip.isLooping)
                    {
                        playTimes = (int)totalProgress;
                        lastProgress = totalProgress - (int)totalProgress;
                    }
                    else
                    {
                        if (totalProgress >= 1)
                        {
                            playTimes = 1;
                            lastProgress = 0;
                        }
                        // 因为总进度小于1，所以本身就是最后一次播放进度
                        else
                        {
                            lastProgress = totalProgress;
                            playTimes = 0;
                        }
                    }
                    
                    // 做采样计算
                    animator.applyRootMotion = true;
                    if (playTimes >= 1)
                    {
                        // 采样一次动画的完整进度
                        animationEvt.AnimationClip.SampleAnimation(previewGameObject, animationEvt.AnimationClip.length);
                        Vector3 samplePos = previewGameObject.transform.position;
                        rootMotionTotalPos += samplePos * playTimes;
                    }

                    if (lastProgress > 0)
                    {
                        // 采样一次动画的不完整进度
                        animationEvt.AnimationClip.SampleAnimation(previewGameObject, lastProgress * animationEvt.AnimationClip.length);
                        Vector3 samplePos = previewGameObject.transform.position;
                        rootMotionTotalPos += samplePos;
                    }
                }

                if (isBreak) break;
            }
            
            #endregion
            
            #region 关于当前帧的姿态
            // 找到距离这一帧左边最近的一个动画，也就是当前要播放的动画
            int currentOffset = int.MaxValue;
            int animationEventIndex = -1;
            foreach (var (startIndex, _) in frameData)
            {
                int tmpOffset = frameIndex - startIndex;
                if (tmpOffset > 0 && tmpOffset < currentOffset)
                {
                    currentOffset = tmpOffset;
                    animationEventIndex = startIndex;
                }
            }

            if (animationEventIndex == -1)
                return;
            
            var animationEvent = frameData[animationEventIndex];
            // 动画资源的总帧数
            float clipFrameCount = animationEvent.AnimationClip.length * animationEvent.AnimationClip.frameRate;
            // 计算当前的播放进度
            float progress = currentOffset / clipFrameCount;
            // 循环动画的处理
            if (progress > 1 && animationEvent.AnimationClip.isLooping)
            {
                progress -= (int)progress;
            }

            animator.applyRootMotion = animationEvent.ApplyRootMotion;
            animationEvent.AnimationClip.SampleAnimation(previewGameObject, progress * animationEvent.AnimationClip.length);
            #endregion

            previewGameObject.transform.position = rootMotionTotalPos;
        }

        #region 重载方法
        public override void DeleteTrackItem(int frameIndex)
        {
            AnimationData.FrameEventDict.Remove(frameIndex);
            if (trackItemDict.Remove(frameIndex, out var animationTrackItem))
            {
                track.Remove(animationTrackItem.Root);
            }
            SkillEditorWindow.Instance.SaveSkillConfig();
        }

        public override void OnConfigChanged()
        {
            foreach (var item in trackItemDict.Values)
            {
                item.OnConfigChanged();
            }
        }
        #endregion
    }
}