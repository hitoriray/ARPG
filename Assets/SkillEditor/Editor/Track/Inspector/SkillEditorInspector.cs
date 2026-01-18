using SkillEditor.Editor.EditorWindow;
using SkillEditor.Editor.Track.AnimationTrack;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor.Editor.Track.Inspector
{
    [CustomEditor(typeof(SkillEditorWindow))]
    public class SkillEditorInspector : UnityEditor.Editor
    {
        public static SkillEditorInspector Instance;
        private static TrackItemBase currentTrackItem;
        private static SkillTrackBase currentTrack; 
        public static void SetTrackItem(TrackItemBase trackItem, SkillTrackBase track)
        {
            if (currentTrackItem != null)
            {
                currentTrackItem.OnUnselect();
            }

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
            root = new();
            Show();
            return root;
        }

        private void Show()
        {
            Clean();
            if (currentTrackItem == null)
                return;

            // TODO: for anim
            if (currentTrackItem.GetType() == typeof(AnimationTrackItem))
            {
                DrawAnimationTrackItem((AnimationTrackItem)currentTrackItem);
            }
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
        }

        #region 动画轨道

        private IntegerField durationField;
        private Toggle rootMotionToggle;
        private Label clipFrameCountLabel;
        private Label isLoopLabel;
    
        private void DrawAnimationTrackItem(AnimationTrackItem animationTrackItem)
        {
            trackItemFrameIndex = animationTrackItem.FrameIndex;
            // 动画资源
            ObjectField animationClipAssetField = new ObjectField("动画资源");
            animationClipAssetField.objectType = typeof(AnimationClip);
            animationClipAssetField.value = animationTrackItem.AnimationEvent.AnimationClip;
            animationClipAssetField.RegisterValueChangedCallback(OnAnimationClipAssetFieldValueChanged);
            root.Add(animationClipAssetField);
            // 根运动
            rootMotionToggle = new Toggle("应用根运动");
            rootMotionToggle.value = animationTrackItem.AnimationEvent.ApplyRootMotion;
            rootMotionToggle.RegisterValueChangedCallback(OnRootMotionToggleValueChanged);
            root.Add(rootMotionToggle);
            // 轨道长度
            durationField = new IntegerField("轨道长度");
            durationField.value = animationTrackItem.AnimationEvent.DurationFrame;
            durationField.RegisterValueChangedCallback(OnDurationFieldValueChanged);
            root.Add(durationField);
            // 过渡时间
            FloatField transitionField = new FloatField("过渡时间");
            transitionField.value = animationTrackItem.AnimationEvent.TransitionTime;
            transitionField.RegisterValueChangedCallback(OnTransitionFieldValueChanged);
            root.Add(transitionField);
            // 动画相关信息
            int clipFrameCount = (int)(animationTrackItem.AnimationEvent.AnimationClip.length * animationTrackItem.AnimationEvent.AnimationClip.frameRate);
            clipFrameCountLabel = new Label($"动画资源长度: {clipFrameCount}");
            root.Add(clipFrameCountLabel);
            isLoopLabel = new Label($"循环动画: {animationTrackItem.AnimationEvent.AnimationClip.isLooping}");
            root.Add(isLoopLabel);

            Button deleteBtn = new Button(OnDeleteBtnClicked);
            deleteBtn.text = "删除";
            deleteBtn.style.backgroundColor = new Color(1, 0, 0, 0.5f);
            root.Add(deleteBtn);
        }

        private void OnAnimationClipAssetFieldValueChanged(ChangeEvent<Object> evt)
        {
            AnimationClip clip = evt.newValue as AnimationClip;
            clipFrameCountLabel.text = $"动画资源长度: {(int)(clip.length * clip.frameRate)}";
            isLoopLabel.text = $"循环动画: {clip.isLooping}";
            (currentTrackItem as AnimationTrackItem).AnimationEvent.AnimationClip = clip;
            SkillEditorWindow.Instance.SaveSkillConfig();
            currentTrackItem.ResetView();
        }

        private void OnRootMotionToggleValueChanged(ChangeEvent<bool> evt)
        {
            (currentTrackItem as AnimationTrackItem).AnimationEvent.ApplyRootMotion = evt.newValue;
            SkillEditorWindow.Instance.SaveSkillConfig();
        }

        private void OnDurationFieldValueChanged(ChangeEvent<int> evt)
        {
            int value = evt.newValue;
            // 安全校验
            if ((currentTrack as AnimationTrack.AnimationTrack).CheckFrameIndexOnDrag(trackItemFrameIndex + value, trackItemFrameIndex, false))
            {
                // 修改数据，刷新视图
                SkillEditorWindow.Instance.SkillConfig.SkillAnimationData.FrameEventDict[trackItemFrameIndex].DurationFrame = value;
                (currentTrackItem as AnimationTrackItem).CheckFrameCount(); // 先刷新再保存，否则会刷新不了
                SkillEditorWindow.Instance.SaveSkillConfig();
                currentTrackItem.ResetView();
            }
            else
            {
                durationField.value = evt.previousValue;
            }
        }

        private void OnTransitionFieldValueChanged(ChangeEvent<float> evt)
        {
            if (evt.previousValue == evt.newValue)
                return;
            SkillEditorWindow.Instance.SkillConfig.SkillAnimationData.FrameEventDict[trackItemFrameIndex].TransitionTime = evt.newValue;
        }

        private void OnDeleteBtnClicked()
        {
            currentTrack.DeleteTrackItem(trackItemFrameIndex);
            Selection.activeObject = null;
        }

        #endregion
    }
}