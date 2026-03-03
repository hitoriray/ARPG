using System.Collections.Generic;
using Config;
using UnityEditor;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class EventTrack : SkillTrackBase
    {
        private SkillSingleLineTrackStyle trackStyle;
        private readonly Dictionary<int, EventTrackItem> trackItemDict = new();

        public SkillCustomEventData CustomEventData => SkillEditorWindow.Instance.SkillClip.SkillCustomEventData;
        
        public override void Init(VisualElement menuParent, VisualElement trackParent, float frameWidth)
        {
            base.Init(menuParent, trackParent, frameWidth);
            trackStyle = new SkillSingleLineTrackStyle();
            trackStyle.Init(menuParent, trackParent, "事件配置");
            trackStyle.contentRoot.RegisterCallback<MouseDownEvent>(OnContentRootMouseDown);
            ResetView();
        }

        private void OnContentRootMouseDown(MouseDownEvent evt)
        {
            int frameIndex = SkillEditorWindow.Instance.GetFrameIndexByPos(evt.localMousePosition.x);
            if (CustomEventData.FrameData.ContainsKey(frameIndex))
                return;
            // 换位置
            if (EventTrackItem.currentSelectedItem != null)
            {
                SkillEditorWindow.Instance.RecordUndoSnapshot();
                EventTrackItem.currentSelectedItem.ChangeFrameIndex(frameIndex);
            }
            // 添加轨道
            else
            {
                // 双击左键才允许新增
                if (evt.button != 0 || evt.clickCount < 2)
                    return;
                SkillEditorWindow.Instance.RecordUndoSnapshot();
                SkillCustomEvent skillCustomEvent = new SkillCustomEvent();
                CustomEventData.FrameData.Add(frameIndex, skillCustomEvent);
                SkillEditorWindow.Instance.SaveSkillConfig();
                CreateEventTrackItem(frameIndex, skillCustomEvent);
            }
        }

        public override void ResetView(float frameWidth)
        {
            base.ResetView(frameWidth);
            
            // 销毁当前已有的
            foreach (var (_, trackItem) in trackItemDict)
            {
                trackStyle.RemoveItem(trackItem.ItemStyle.Root);
            }
            trackItemDict.Clear();

            if (SkillEditorWindow.Instance.SkillClip == null)
                return;
            // 根据数据绘制TrackItem
            foreach (var (startFrameIndex, animationEvent) in CustomEventData.FrameData)
            {
                CreateEventTrackItem(startFrameIndex, animationEvent);
            }
        }

        private void CreateEventTrackItem(int startFrameIndex, SkillCustomEvent animationEvent)
        {
            EventTrackItem trackItem = new();
            trackItem.Init(this, trackStyle, startFrameIndex, frameWidth, animationEvent);
            trackItemDict.Add(startFrameIndex, trackItem);
        }
        
        /// <summary>
        /// 将oldIndex的数据变为newIndex，其实就是修改skillConfig中字典的索引
        /// </summary>
        /// <param name="oldIndex"></param>
        /// <param name="newIndex"></param>
        
        public void SetFrameIndex(int oldIndex, int newIndex)
        {
            if (CustomEventData.FrameData.Remove(oldIndex, out var customEvent))
            {
                CustomEventData.FrameData.Add(newIndex, customEvent);
                trackItemDict.Remove(oldIndex, out var trackItem);
                trackItemDict.Add(newIndex, trackItem);
            }
        }
        
        #region 重载方法
        public override void DeleteTrackItem(int frameIndex)
        {
            SkillEditorWindow.Instance.RecordUndoSnapshot();
            CustomEventData.FrameData.Remove(frameIndex);
            if (trackItemDict.Remove(frameIndex, out var trackItem))
            {
                trackStyle.RemoveItem(trackItem.ItemStyle.Root);
            }
        }

        public override void Destroy()
        {
            trackStyle.Destroy();
        }

        #endregion
    }
}
