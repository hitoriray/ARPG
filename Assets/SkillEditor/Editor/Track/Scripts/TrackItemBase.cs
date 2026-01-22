using UnityEngine;

namespace SkillEditor
{
    public abstract class TrackItemBase
    {
        protected float frameUnitWidth;
        protected int frameIndex;
        public int FrameIndex => frameIndex;

        public abstract void Select();
        public abstract void OnSelect();
        public abstract void OnUnselect();

        public virtual void OnConfigChanged() {}
        public virtual void ResetView()
        {
            ResetView(frameUnitWidth);
        }
        public virtual void ResetView(float frameUnitWidth)
        {
            this.frameUnitWidth = frameUnitWidth;
        }
    }

    public abstract class TrackItemBase<T> : TrackItemBase where T : SkillTrackBase
    {
        protected T track;
        protected Color normalColor;
        protected Color selectColor;
        public SkillTrackItemStyleBase ItemStyle { get; protected set; }
        
        public override void Select()
        {
            SkillEditorWindow.Instance.ShowTrackItemOnInspector(this, track);
        }
        public override void OnSelect()
        {
            ItemStyle.SetBGColor(selectColor);
        }
        public override void OnUnselect()
        {
            ItemStyle.SetBGColor(normalColor);
        }
    }
}