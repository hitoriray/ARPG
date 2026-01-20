using UnityEditor;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class SkillSingleLineTrackStyle : SkillTrackStyleBase
    {
        private const string MenuAssetPath = "Assets/SkillEditor/Editor/Track/Assets/SingleLineTrackStyle/SingleLineTrackMenu.uxml";
        private const string TrackAssetPath = "Assets/SkillEditor/Editor/Track/Assets/SingleLineTrackStyle/SingleLineTrackContent.uxml";

        public void Init(VisualElement menuParent, VisualElement contentParent, string title)
        {
            this.menuParent = menuParent;
            this.contentParent = contentParent;
            menuRoot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MenuAssetPath).Instantiate().Query().ToList()[1];
            menuParent.Add(menuRoot);
            titleLabel = (Label)menuRoot;
            titleLabel.text = title;
            
            contentRoot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TrackAssetPath).Instantiate().Query().ToList()[1];
            contentParent.Add(contentRoot);
        }
    }
}
