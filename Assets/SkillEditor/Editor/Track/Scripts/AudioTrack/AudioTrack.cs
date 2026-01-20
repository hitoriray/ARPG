using UnityEngine.UIElements;

namespace SkillEditor
{
    public class AudioTrack : SkillTrackBase
    {
        private SkillMultiLineTrackStyle trackStyle;

        public override void Init(VisualElement menuParent, VisualElement trackParent, float frameWidth)
        {
            base.Init(menuParent, trackParent, frameWidth);
            trackStyle = new SkillMultiLineTrackStyle();
            trackStyle.Init(menuParent, trackParent, "音效配置", CheckAddChildTrack, CheckDeleteChildTrack);
            
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