using Config;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class EventTrackItem : TrackItemBase<EventTrack>
    {
        private SkillCustomEvent customEvent;
        public SkillCustomEvent CustomEvent => customEvent;
        
        private SkillEventTrackItemStyle trackItemStyle;
        public static EventTrackItem currentSelectedItem;

        public void Init(EventTrack eventTrack, SkillTrackStyleBase parentTrackStyle, int startFrameIndex, float frameUnitWidth, SkillCustomEvent customEvent)
        {
            track = eventTrack;
            this.frameIndex = startFrameIndex;
            this.frameUnitWidth = frameUnitWidth;
            this.customEvent = customEvent;

            trackItemStyle = new SkillEventTrackItemStyle();
            ItemStyle = trackItemStyle;
            trackItemStyle.Init(parentTrackStyle);

            normalColor = new Color(0.388f, 0.850f, 0.905f, 0.5f);
            selectColor = new Color(0.388f, 0.850f, 0.905f, 1f);
            OnUnselect();
            trackItemStyle.Root.RegisterCallback<MouseDownEvent>(OnMouseDown);

            ResetView(frameUnitWidth);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (currentSelectedItem == this) OnUnselect();
            Select();
        }

        public override void OnSelect()
        {
            currentSelectedItem = this;
            base.OnSelect();
        }

        public override void OnUnselect()
        {
            currentSelectedItem = null;
            base.OnUnselect();
        }

        public override void ResetView(float frameUnitWidth)
        {
            base.ResetView(frameUnitWidth);
            
            // 位置计算
            trackItemStyle.SetPositionX(frameIndex * frameUnitWidth - frameUnitWidth / 2);
            trackItemStyle.SetWidth(frameUnitWidth);
        }

        public void ChangeFrameIndex(int newIndex)
        {
            track.SetFrameIndex(frameIndex, newIndex);
            frameIndex = newIndex;
            SkillEditorInspector.Instance.SetTrackItemFrameIndex(frameIndex);
            ResetView();
        }
    }
}