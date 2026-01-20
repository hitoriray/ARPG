using System.Collections.Generic;
using Config;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class AudioTrack : SkillTrackBase
    {
        private SkillMultiLineTrackStyle trackStyle;
        public SkillAudioData AudioData => SkillEditorWindow.Instance.SkillConfig.SkillAudioData;
        private List<AudioTrackItem> trackItems = new();

        public override void Init(VisualElement menuParent, VisualElement trackParent, float frameWidth)
        {
            base.Init(menuParent, trackParent, frameWidth);
            trackStyle = new SkillMultiLineTrackStyle();
            trackStyle.Init(menuParent, trackParent, "音效配置", AddChildTrack, CheckDeleteChildTrack, SwapChildTrack);
            
            ResetView();
        }

        public override void ResetView(float frameWidth)
        {
            base.ResetView(frameWidth);
            // 销毁当前已有的
            foreach (var item in trackItems)
            {
                item.Destroy();
            }
            trackItems.Clear();
            
            if (SkillEditorWindow.Instance.SkillConfig == null)
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
            item.Init(frameWidth, audioEvent, trackStyle.AddChildTrack());
            item.SetTrackName(audioEvent.TrackName);
            trackItems.Add(item);
        }

        /// <summary>
        /// 新增子轨道
        /// </summary>
        /// <returns></returns>
        private void AddChildTrack()
        {
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
            AudioData.FrameData.RemoveAt(index);
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
        #endregion
    }
}