using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class SkillAudioEventInspector : SkillEventDataInspectorBase<AudioTrackItem, AudioTrack>
    {
        private FloatField volumeField;

        protected override void OnDraw()
        {
            // 音效资源
            ObjectField audioClipAssetField = new("音效资源")
            {
                objectType = typeof(AudioClip),
                value = trackItem.AudioEvent.AudioClip
            };
            audioClipAssetField.RegisterValueChangedCallback(OnAudioClipAssetFieldValueChanged);
            root.Add(audioClipAssetField);
            
            // 音量
            volumeField = new("播放音量")
            {
                value = trackItem.AudioEvent.Volume
            };
            volumeField.RegisterCallback<FocusInEvent>(OnVolumeFieldFocusIn);
            volumeField.RegisterCallback<FocusOutEvent>(OnVolumeFieldFocusOut);
            root.Add(volumeField);
        }
        
        private void OnAudioClipAssetFieldValueChanged(ChangeEvent<Object> evt)
        {
            trackItem.AudioEvent.AudioClip = (AudioClip)evt.newValue;
            trackItem.ResetView();
        }
        
        #region VolumeField
        private float oldVolumeValue;
        private void OnVolumeFieldFocusIn(FocusInEvent evt)
        {
            oldVolumeValue = volumeField.value;
        }

        private void OnVolumeFieldFocusOut(FocusOutEvent evt)
        {
            if (!Mathf.Approximately(volumeField.value, oldVolumeValue))
            {
                trackItem.AudioEvent.Volume = volumeField.value;
            }
        }
        #endregion
    }
}