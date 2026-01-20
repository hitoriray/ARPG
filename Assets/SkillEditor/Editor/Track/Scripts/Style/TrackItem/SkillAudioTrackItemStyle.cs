using Config;
using UnityEditor;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class SkillAudioTrackItemStyle : SkillTrackItemStyleBase
    {
        private const string trackItemAssetPath = "Assets/SkillEditor/Editor/Track/Assets/TrackItem/AudioTrackItem.uxml";
        private Label titleLabel;
        public VisualElement MainDragArea { get; protected set; }
        public bool IsInit { get; private set; }
        public void Init(float frameUnitWidth, SkillAudioEvent audioEvent, SkillMultiLineTrackStyle.ChildTrack childTrack)
        {
            // 没有资源的话就不需要初始化
            if (IsInit || audioEvent.AudioClip == null)
                return;
            titleLabel = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(trackItemAssetPath).Instantiate().Query<Label>();
            Root = titleLabel;
            MainDragArea = Root.Q<VisualElement>("Main");
            childTrack.InitContent(Root);
            IsInit = true;
        }

        public void ResetView(float frameUnitWidth, SkillAudioEvent audioEvent)
        {
            SetTitle(audioEvent.AudioClip.name);
            SetWidth(frameUnitWidth * audioEvent.AudioClip.length * SkillEditorWindow.Instance.SkillConfig.FrameRate);
            SetPositionX(frameUnitWidth * audioEvent.FrameIndex);
        }
        
        public void SetTitle(string title)
        {
            titleLabel.text = title;
        }
    }
}