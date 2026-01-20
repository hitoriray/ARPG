using System;
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

            // TODO: 补充其他类型
            Type itemType = currentTrackItem.GetType();
            if (itemType == typeof(AnimationTrackItem))
            {
                DrawAnimationTrackItem((AnimationTrackItem)currentTrackItem);
            }
            else if (itemType == typeof(AudioTrackItem))
            {
                DrawAudioTrackItem((AudioTrackItem)currentTrackItem);
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
        private FloatField transitionField;
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
            durationField.RegisterCallback<FocusInEvent>(OnDurationFieldFocusIn);
            durationField.RegisterCallback<FocusOutEvent>(OnDurationFieldFocusOut);
            root.Add(durationField);
            // 过渡时间
            transitionField = new FloatField("过渡时间");
            transitionField.value = animationTrackItem.AnimationEvent.TransitionTime;
            transitionField.RegisterCallback<FocusInEvent>(OnTransitionFieldFocusIn);
            transitionField.RegisterCallback<FocusOutEvent>(OnTransitionFieldFocusOut);
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
            ((AnimationTrackItem)currentTrackItem).AnimationEvent.AnimationClip = clip;
            SkillEditorWindow.Instance.SaveSkillConfig();
            currentTrackItem.ResetView();
        }

        private void OnRootMotionToggleValueChanged(ChangeEvent<bool> evt)
        {
            ((AnimationTrackItem)currentTrackItem).AnimationEvent.ApplyRootMotion = evt.newValue;
            SkillEditorWindow.Instance.SaveSkillConfig();
        }
        
        #region DurationField事件
        private int oldDurationValue = 0;
        private void OnDurationFieldFocusIn(FocusInEvent evt)
        {
            oldDurationValue = durationField.value;
        }

        private void OnDurationFieldFocusOut(FocusOutEvent evt)
        {
            if (durationField.value != oldDurationValue)
            {
                // 安全校验
                if (((AnimationTrack)currentTrack).CheckFrameIndexOnDrag(trackItemFrameIndex + durationField.value, trackItemFrameIndex, false))
                {
                    // 修改数据，刷新视图
                    SkillEditorWindow.Instance.SkillConfig.SkillAnimationData.FrameEventDict[trackItemFrameIndex].DurationFrame = durationField.value;
                    (currentTrackItem as AnimationTrackItem)?.CheckFrameCount(); // 先刷新再保存，否则会刷新不了
                    SkillEditorWindow.Instance.SaveSkillConfig();
                    currentTrackItem.ResetView();
                }
                else
                {
                    durationField.value = oldDurationValue;
                }
            }
        }
        #endregion

        #region TransitionField事件
        private float oldTransitionValue = 0;
        private void OnTransitionFieldFocusIn(FocusInEvent evt)
        {
            oldTransitionValue = transitionField.value;
        }

        private void OnTransitionFieldFocusOut(FocusOutEvent evt)
        {
            if (transitionField.value != oldTransitionValue)
            {
                ((AnimationTrackItem)currentTrackItem).AnimationEvent.TransitionTime = transitionField.value;
            }
        }
        #endregion
        
        private void OnDeleteBtnClicked()
        {
            currentTrack.DeleteTrackItem(trackItemFrameIndex);
            Selection.activeObject = null;
        }

        #endregion
        
        #region 音效轨道

        private FloatField volumeField;
        private void DrawAudioTrackItem(AudioTrackItem audioTrackItem)
        {
            // 音效资源
            ObjectField audioClipAssetField = new ObjectField("音效资源");
            audioClipAssetField.objectType = typeof(AudioClip);
            audioClipAssetField.value = audioTrackItem.AudioEvent.AudioClip;
            audioClipAssetField.RegisterValueChangedCallback(OnAudioClipAssetFieldValueChanged);
            root.Add(audioClipAssetField);
            
            // 音量
            volumeField = new FloatField("播放音量");
            volumeField.value = audioTrackItem.AudioEvent.Volume;
            volumeField.RegisterCallback<FocusInEvent>(OnVolumeFieldFocusIn);
            volumeField.RegisterCallback<FocusOutEvent>(OnVolumeFieldFocusOut);
            root.Add(volumeField);
        }

        private void OnAudioClipAssetFieldValueChanged(ChangeEvent<Object> evt)
        {
            var clip = evt.newValue as AudioClip;
            ((AudioTrackItem)currentTrackItem).AudioEvent.AudioClip = clip;
            currentTrackItem.ResetView();
        }
        
        #region VolumeField
        private float oldVolumeValue = 0;
        private void OnVolumeFieldFocusIn(FocusInEvent evt)
        {
            oldVolumeValue = volumeField.value;
        }

        private void OnVolumeFieldFocusOut(FocusOutEvent evt)
        {
            if (volumeField.value != oldVolumeValue)
            {
                ((AudioTrackItem)currentTrackItem).AudioEvent.Volume = volumeField.value;
            }
        }
        #endregion

        #endregion
    }
}