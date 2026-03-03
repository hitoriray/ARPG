using System.Collections.Generic;
using Config;
using UnityEditor;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class AudioTrack : SkillTrackBase
    {
        private SkillMultiLineTrackStyle trackStyle;
        public SkillAudioData AudioData => SkillEditorWindow.Instance.SkillClip.SkillAudioData;
        private readonly List<AudioTrackItem> trackItemList = new();

        public override void Init(VisualElement menuParent, VisualElement trackParent, float frameWidth)
        {
            base.Init(menuParent, trackParent, frameWidth);
            trackStyle = new SkillMultiLineTrackStyle();
            trackStyle.Init(menuParent, trackParent, "音效配置", AddChildTrack, CheckDeleteChildTrack, SwapChildTrack, UpdateChildTrackName);
            
            ResetView();
        }

        public override void ResetView(float frameWidth)
        {
            base.ResetView(frameWidth);
            // 销毁当前已有的
            foreach (var item in trackItemList)
            {
                item.Destroy();
            }
            trackItemList.Clear();
            
            if (SkillEditorWindow.Instance.SkillClip == null)
                return;
            
            // 根据数据绘制TrackItem
            foreach (var audioEvent in AudioData.FrameData)
            {
                CreateAudioTrackItem(audioEvent);
            }
        }

        private void CreateAudioTrackItem(SkillAudioEvent audioEvent)
        {
            var item = new AudioTrackItem();
            item.Init(this, frameWidth, audioEvent, trackStyle.AddChildTrack());
            item.SetTrackName(audioEvent.TrackName);
            trackItemList.Add(item);
        }

        /// <summary>
        /// 更新子轨道名称
        /// </summary>
        private void UpdateChildTrackName(SkillMultiLineTrackStyle.ChildTrack childTrack, string newName)
        {
            // 同步给配置
            AudioData.FrameData[childTrack.GetIndex()].TrackName = newName;
            SkillEditorWindow.Instance.SaveSkillConfig();
        }

        /// <summary>
        /// 新增子轨道
        /// </summary>
        private void AddChildTrack()
        {
            SkillEditorWindow.Instance.RecordUndoSnapshot();
            SkillAudioEvent audioEvent = new();
            AudioData.FrameData.Add(audioEvent);
            CreateAudioTrackItem(audioEvent);
            SkillEditorWindow.Instance.SaveSkillConfig();
        }

        /// <summary>
        /// 检查子轨道能否删除
        /// </summary>
        /// <param name="index">子轨道的索引</param>
        /// <returns></returns>
        private bool CheckDeleteChildTrack(int index)
        {
            if (index < 0 || index >= AudioData.FrameData.Count)
                return false;
            if (AudioData.FrameData[index] == null)
                return false;
            SkillEditorWindow.Instance.RecordUndoSnapshot();
            AudioData.FrameData.RemoveAt(index);
            trackItemList.RemoveAt(index);
            SkillEditorWindow.Instance.SaveSkillConfig();
            return true;
        }

        private void SwapChildTrack(int index1, int index2)
        {
            var audioData1 = AudioData.FrameData[index1];
            var audioData2 = AudioData.FrameData[index2];
            AudioData.FrameData[index1] = audioData2;
            AudioData.FrameData[index2] = audioData1;
            // 保存交给窗口的退出机制
        }

        #region 重载方法
        public override void Destroy()
        {
            trackStyle.Destroy();
        }

        public override void OnPlay(int startFrameIndex)
        {
            foreach (var audioEvent in AudioData.FrameData)
            {
                if (audioEvent.AudioClip == null)
                    continue;
                
                // 1.开始帧在左边 && 长度大于当前选中帧 = 时间轴播放帧在轨道中间部分
                float audioFrameCount = audioEvent.AudioClip.length * SkillEditorWindow.Instance.SkillClip.FrameRate;
                int audioLastFrameCount =
                    (int)(audioEvent.AudioClip.length * SkillEditorWindow.Instance.SkillClip.FrameRate) + audioEvent.FrameIndex;
                if (audioEvent.FrameIndex < startFrameIndex && audioLastFrameCount > startFrameIndex)
                {
                    // 按比例播放音效
                    int offset = startFrameIndex - audioEvent.FrameIndex;
                    float playRate = offset / audioFrameCount;
                    EditorAudioUtility.PlayAudio(audioEvent.AudioClip, playRate);
                }
                else if (audioEvent.FrameIndex == startFrameIndex)
                {
                    // 播放音效，从头播放
                    EditorAudioUtility.PlayAudio(audioEvent.AudioClip, 0);
                }
            }
        }

        public override void TickView(int frameIndex)
        {
            if (SkillEditorWindow.Instance.IsPlaying)
            {
                foreach (var audioEvent in AudioData.FrameData)
                {
                    if (audioEvent.AudioClip != null && audioEvent.FrameIndex == frameIndex)
                    {
                        // 播放音效，从头播放
                        EditorAudioUtility.PlayAudio(audioEvent.AudioClip, 0);
                    }
                }
            }
        }
        
        #endregion
    }
}