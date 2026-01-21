using Config;
using UnityEditor;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class SkillAttackDetectionTrackItemStyle : SkillTrackItemStyleBase
    {
        private const string trackItemAssetPath = "Assets/SkillEditor/Editor/Track/Assets/TrackItem/AudioTrackItem.uxml";
        private Label titleLabel;
        public VisualElement MainDragArea { get; protected set; }
        public bool IsInit { get; private set; }
        
        public void Init(float frameUnitWidth, SkillAttackDetectionEvent attackDetectionEvent, SkillMultiLineTrackStyle.ChildTrack childTrack)
        {
            // 没有资源的话就不需要初始化
            if (IsInit)
                return;
            titleLabel = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(trackItemAssetPath).Instantiate().Query<Label>();
            Root = titleLabel;
            MainDragArea = Root.Q<VisualElement>("Main");
            childTrack.InitContent(Root);
            IsInit = true;
        }

        public void ResetView(float frameUnitWidth, SkillAttackDetectionEvent attackDetectionEvent)
        {
            if (IsInit == false)
                return;
            SetTitle("");
            SetWidth(frameUnitWidth * attackDetectionEvent.DurationFrame);
            SetPositionX(frameUnitWidth * attackDetectionEvent.FrameIndex);
        }
        
        public void SetTitle(string title)
        {
            titleLabel.text = title;
        }
    }
}