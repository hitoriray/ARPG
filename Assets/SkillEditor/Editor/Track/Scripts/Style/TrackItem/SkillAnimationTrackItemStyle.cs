using UnityEditor;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class SkillAnimationTrackItemStyle : SkillTrackItemStyleBase
    {
        private const string trackItemAssetPath = "Assets/SkillEditor/Editor/Track/Assets/AnimationTrack/AnimationTrackItem.uxml";
        private Label titleLabel;
        public VisualElement MainDragArea { get; protected set; }
        public VisualElement AnimationOverLine { get; protected set; }
        public void Init(SkillTrackStyleBase trackStyle, int startFrameIndex, float frameUnitWidth)
        {
            titleLabel = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(trackItemAssetPath).Instantiate().Query<Label>();
            Root = titleLabel;
            MainDragArea = Root.Q<VisualElement>("Main");
            AnimationOverLine = Root.Q<VisualElement>("OverLine");
            trackStyle.AddItem(Root);
        }

        public void SetTitle(string title)
        {
            titleLabel.text = title;
        }
    }
}