using UnityEditor;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class SkillEventTrackItemStyle : SkillTrackItemStyleBase
    {
        private const string trackItemAssetPath = "Assets/SkillEditor/Editor/Track/Assets/TrackItem/EventTrackItem.uxml";
        
        public void Init(SkillTrackStyleBase trackStyle)
        {
            Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(trackItemAssetPath).Instantiate().Query<Label>();
            trackStyle.AddItem(Root);
        }
    }
}