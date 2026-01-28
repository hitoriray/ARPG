using System;
using Config;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SkillEditor
{
    [CustomEditor(typeof(SkillEditorWindow))]
    public class SkillEditorInspector : Editor
    {
        public static SkillEditorInspector Instance;
        private static TrackItemBase currentTrackItem;
        public static TrackItemBase CurrentTrackItem => currentTrackItem;
        private static SkillTrackBase currentTrack; 
        public static void SetTrackItem(TrackItemBase trackItem, SkillTrackBase track)
        {
            currentTrackItem?.OnUnselect();
            currentTrackItem = trackItem;
            currentTrackItem.OnSelect();
            currentTrack = track;
            // 避免打开了Inspector，不刷新数据
            if (Instance != null)
            {
                Instance.Show();
            }
        }

        private void OnDestroy()
        {
            if (currentTrackItem != null)
            {
                currentTrackItem.OnUnselect();
                currentTrackItem = null;
                currentTrack = null;
            }
        }

        private VisualElement root;

        public override VisualElement CreateInspectorGUI()
        {
            Instance = this;
            root = new VisualElement();
            Show();
            return root;
        }
        
        private SkillEventDataInspectorBase eventDataInspector;

        public void Show()
        {
            Clean();
            if (currentTrackItem == null)
                return;

            trackItemFrameIndex = currentTrackItem.FrameIndex;
            Type itemType = currentTrackItem.GetType();
            eventDataInspector = null;
            if (itemType == typeof(AnimationTrackItem))
            {
                eventDataInspector = new SkillAnimationEventInspector();
            }
            else if (itemType == typeof(AudioTrackItem))
            {
                eventDataInspector = new SkillAudioEventInspector();
            }
            else if (itemType == typeof(EffectTrackItem))
            {
                eventDataInspector = new SkillEffectEventInspector();
            }
            else if (itemType == typeof(AttackDetectionTrackItem))
            {
                eventDataInspector = new SkillAttackDetectionEventInspector();
            }
            else if (itemType == typeof(EventTrackItem))
            {
                eventDataInspector = new SkillCustomEventInspector();
            }
            
            eventDataInspector?.Draw(root, currentTrackItem, currentTrack);
        }

        private void Clean()
        {
            if (root != null)
            {
                for (int i = root.childCount - 1; i >= 0; i--)
                    root.RemoveAt(i);
            }
        }
        
        private int trackItemFrameIndex;
        public void SetTrackItemFrameIndex(int index)
        {
            trackItemFrameIndex = index;
            eventDataInspector.SetFrameIndex(trackItemFrameIndex);
        }
    }
}