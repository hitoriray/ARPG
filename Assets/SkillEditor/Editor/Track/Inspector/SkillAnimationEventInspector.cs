using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class SkillAnimationEventInspector : SkillEventDataInspectorBase<AnimationTrackItem, AnimationTrack>
    {
        private IntegerField durationField;
        private FloatField transitionField;
        private Toggle rootMotionToggle;
        private Label clipFrameCountLabel;
        private Label isLoopLabel;

        protected override void OnDraw()
        {
            // 动画资源
            ObjectField animationClipAssetField = new("动画资源")
            {
                objectType = typeof(AnimationClip),
                value = trackItem.AnimationEvent.AnimationClip
            };
            animationClipAssetField.RegisterValueChangedCallback(OnAnimationClipAssetFieldValueChanged);
            root.Add(animationClipAssetField);
            // 根运动
            rootMotionToggle = new Toggle("应用根运动")
            {
                value = trackItem.AnimationEvent.ApplyRootMotion
            };
            rootMotionToggle.RegisterValueChangedCallback(OnRootMotionToggleValueChanged);
            root.Add(rootMotionToggle);
            // 轨道长度
            durationField = new IntegerField("轨道长度")
            {
                value = trackItem.AnimationEvent.DurationFrame
            };
            durationField.RegisterCallback<FocusInEvent>(OnDurationFieldFocusIn);
            durationField.RegisterCallback<FocusOutEvent>(OnDurationFieldFocusOut);
            root.Add(durationField);
            // 过渡时间
            transitionField = new FloatField("过渡时间")
            {
                value = trackItem.AnimationEvent.TransitionTime
            };
            transitionField.RegisterCallback<FocusInEvent>(OnTransitionFieldFocusIn);
            transitionField.RegisterCallback<FocusOutEvent>(OnTransitionFieldFocusOut);
            root.Add(transitionField);
            // 动画相关信息
            int clipFrameCount = (int)(trackItem.AnimationEvent.AnimationClip.length * trackItem.AnimationEvent.AnimationClip.frameRate);
            clipFrameCountLabel = new Label($"动画资源长度: {clipFrameCount}");
            root.Add(clipFrameCountLabel);
            isLoopLabel = new Label($"循环动画: {trackItem.AnimationEvent.AnimationClip.isLooping}");
            root.Add(isLoopLabel);
            // 设置持续帧数至选中帧
            Button setFrameBtn = new(OnSetFrameBtnClicked)
            {
                text = "设置持续帧数至选中帧"
            };
            root.Add(setFrameBtn);
        }
        
        private void OnAnimationClipAssetFieldValueChanged(ChangeEvent<Object> evt)
        {
            AnimationClip clip = (AnimationClip)evt.newValue;
            clipFrameCountLabel.text = $"动画资源长度: {(int)(clip.length * clip.frameRate)}";
            isLoopLabel.text = $"循环动画: {clip.isLooping}";
            trackItem.AnimationEvent.AnimationClip = clip;
            trackItem.ResetView();
            SkillEditorWindow.Instance.TickSkill();
        }

        private void OnRootMotionToggleValueChanged(ChangeEvent<bool> evt)
        {
            trackItem.AnimationEvent.ApplyRootMotion = evt.newValue;
            SkillEditorWindow.Instance.TickSkill();
        }
        
        #region DurationField事件
        private int oldDurationValue;
        private void OnDurationFieldFocusIn(FocusInEvent evt)
        {
            oldDurationValue = durationField.value;
        }

        private void OnDurationFieldFocusOut(FocusOutEvent evt)
        {
            if (durationField.value != oldDurationValue)
            {
                // 安全校验
                if (track.CheckFrameIndexOnDrag(itemFrameIndex + durationField.value, itemFrameIndex, false))
                {
                    // 修改数据，刷新视图
                    trackItem.AnimationEvent.DurationFrame = durationField.value;
                    trackItem.CheckFrameCount(); // 先刷新再保存，否则会刷新不了
                    SkillEditorWindow.Instance.SaveSkillConfig();
                    trackItem.ResetView();
                }
                else
                {
                    durationField.value = oldDurationValue;
                }
                SkillEditorWindow.Instance.TickSkill();
            }
        }
        
        private void OnSetFrameBtnClicked()
        {
            OnDurationFieldFocusIn(null);
            var newValue = SkillEditorWindow.Instance.CurrentSelectFrameIndex - trackItem.FrameIndex;
            if (newValue > 0) durationField.value = newValue;
            OnDurationFieldFocusOut(null);
        }
        #endregion

        #region TransitionField事件
        private float oldTransitionValue;
        private void OnTransitionFieldFocusIn(FocusInEvent evt)
        {
            oldTransitionValue = transitionField.value;
        }

        private void OnTransitionFieldFocusOut(FocusOutEvent evt)
        {
            if (!Mathf.Approximately(transitionField.value, oldTransitionValue))
            {
                trackItem.AnimationEvent.TransitionTime = transitionField.value;
            }
        }
        #endregion
    }
}