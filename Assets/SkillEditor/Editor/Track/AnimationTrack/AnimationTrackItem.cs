using Config.Skill;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor.Editor.Track.AnimationTrack
{
    public class AnimationTrackItem : TrackItemBase
    {
        private const string trackItemAssetPath =
            "Assets/SkillEditor/Editor/Track/AnimationTrack/AnimationTrackItem.uxml";

        private AnimationTrack animationTrack;
        private int frameIndex;
        private float frameUnitWidth;
        private SkillAnimationEvent animationEvent;

        public Label Root { get; private set; }
        private VisualElement mainDragArea;
        private VisualElement animationOverLine;

        public void Init(AnimationTrack animationTrack, VisualElement parent, int startFrameIndex, float frameUnitWidth,
            SkillAnimationEvent animationEvent)
        {
            this.animationTrack = animationTrack;
            this.frameIndex = startFrameIndex;
            this.frameUnitWidth = frameUnitWidth;
            this.animationEvent = animationEvent;

            Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(trackItemAssetPath).Instantiate().Query<Label>();
            mainDragArea = Root.Q<VisualElement>("Main");
            animationOverLine = Root.Q<VisualElement>("OverLine");
            parent.Add(Root);
            ResetView(frameUnitWidth);
        }

        public void ResetView(float frameUnitWidth)
        {
            this.frameUnitWidth = frameUnitWidth;
            Root.text = animationEvent.AnimationClip.name;

            // 位置计算
            Vector3 mainPos = Root.transform.position;
            mainPos.x = frameIndex * frameUnitWidth;
            Root.transform.position = mainPos;
            Root.style.width = animationEvent.DurationFrame * frameUnitWidth;
            
            // 计算动画结束线的位置
            int animationClipFrameCount = (int)(animationEvent.AnimationClip.length * animationEvent.AnimationClip.frameRate);
            if (animationClipFrameCount > animationEvent.DurationFrame)
            {
                animationOverLine.style.display = DisplayStyle.None;
            }
            else
            {
                animationOverLine.style.display = DisplayStyle.Flex;
                Vector3 overLinePos = animationOverLine.transform.position;
                overLinePos.x = animationClipFrameCount * frameUnitWidth - 1;
                animationOverLine.transform.position = overLinePos;
            }
        }
    }
}