using UnityEditor;
using UnityEngine.UIElements;

namespace SkillEditor.Editor.Track
{
    public abstract class SkillTrackBase
    {
        protected VisualElement menuParent;
        protected VisualElement trackParent;
        
        protected VisualElement menu;
        protected VisualElement track;
        
        public abstract string MenuAssetPath { get; }
        public abstract string TrackAssetPath { get; }
        
        public virtual void Init(VisualElement menuParent, VisualElement trackParent)
        {
            this.menuParent = menuParent;
            this.trackParent = trackParent;
            
            menu = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MenuAssetPath).Instantiate().Query().ToList()[1];
            menuParent.Add(menu);
            
            track = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TrackAssetPath).Instantiate().Query().ToList()[1];
            trackParent.Add(track);
        }
    }
}