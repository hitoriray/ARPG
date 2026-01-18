using System.Collections.Generic;
using Codice.CM.Interfaces;
using Config.Skill;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor.Editor.Track.AnimationTrack
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