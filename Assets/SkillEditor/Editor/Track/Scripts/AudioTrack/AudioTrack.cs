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
            trackStyle.Init(menuParent, trackParent, "音效配置", CheckAddChildTrack, CheckDeleteChildTrack);
            
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
        /// 检查子轨道能否添加
        /// </summary>
        /// <returns></returns>
        private bool CheckAddChildTrack()
        {
            return true;
        }

        /// <summary>
        /// 检查子轨道能否删除
        /// </summary>
        /// <param name="index">子轨道的索引</param>
        /// <returns></returns>
        private bool CheckDeleteChildTrack(int index)
        {
            return true;
        }

        #region 重载方法
        public override void Destroy()
        {
            trackStyle.Destroy();
        }
        #endregion
    }
}