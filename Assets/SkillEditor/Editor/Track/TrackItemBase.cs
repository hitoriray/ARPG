using SkillEditor.Editor.EditorWindow;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor.Editor.Track
{
    public abstract class TrackItemBase
    {
        protected float frameUnitWidth;

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
        public Label Root { get; protected set; }
        protected int frameIndex;
        public int FrameIndex => frameIndex;


        public override void Select()
        {
            SkillEditorWindow.Instance.ShowTrackItemOnInspector(this, track);
        }
        public override void OnSelect()
        {
            Root.style.backgroundColor = selectColor;
        }
        public override void OnUnselect()
        {
            Root.style.backgroundColor = normalColor;
        }
    }
}