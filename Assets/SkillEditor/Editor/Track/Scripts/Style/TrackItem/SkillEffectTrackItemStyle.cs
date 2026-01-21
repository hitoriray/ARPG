using Config;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class SkillEffectTrackItemStyle : SkillTrackItemStyleBase
    {
        private const string trackItemAssetPath = "Assets/SkillEditor/Editor/Track/Assets/TrackItem/AudioTrackItem.uxml";
        private Label titleLabel;
        public VisualElement MainDragArea { get; protected set; }
        public bool IsInit { get; private set; }
        public void Init(float frameUnitWidth, SkillEffectEvent effectEvent, SkillMultiLineTrackStyle.ChildTrack childTrack)
        {
            // 没有资源的话就不需要初始化
            if (IsInit || effectEvent.Prefab == null)
                return;
            titleLabel = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(trackItemAssetPath).Instantiate().Query<Label>();
            Root = titleLabel;
            MainDragArea = Root.Q<VisualElement>("Main");
            childTrack.InitContent(Root);
            IsInit = true;
        }

        public void ResetView(float frameUnitWidth, SkillEffectEvent effectEvent)
        {
            if (IsInit == false)
                return;
            if (effectEvent.Prefab != null)
            {
                SetTitle(effectEvent.Prefab.name);
                SetWidth(frameUnitWidth * effectEvent.Duration * SkillEditorWindow.Instance.SkillConfig.FrameRate);
                SetPositionX(frameUnitWidth * effectEvent.FrameIndex);
            }
            else
            {
                SetTitle("");
                SetWidth(0);
                SetPositionX(0);
            }
        }
        
        public void SetTitle(string title)
        {
            titleLabel.text = title;
        }
    }
}