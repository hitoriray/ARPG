using System.Collections.Generic;
using Config;
using UnityEngine.UIElements;

namespace SkillEditor
{
    public class AttackDetectionTrack : SkillTrackBase
    {
        private SkillMultiLineTrackStyle trackStyle;
        public SkillAttackDetectionData AttackDetectionData => SkillEditorWindow.Instance.SkillConfig.SkillAttackDetectionData;
        private List<AttackDetectionTrackItem> trackItems = new();
        
        public override void Init(VisualElement menuParent, VisualElement trackParent, float frameWidth)
        {
            base.Init(menuParent, trackParent, frameWidth);
            trackStyle = new SkillMultiLineTrackStyle();
            trackStyle.Init(menuParent, trackParent, "攻击伤害检测", AddChildTrack, CheckDeleteChildTrack, SwapChildTrack, UpdateChildTrackName);
            
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
            foreach (var attackDetectionEvent in AttackDetectionData.FrameData)
            {
                CreateAttackDetectionTrackItem(attackDetectionEvent);
            }
        }

        private void CreateAttackDetectionTrackItem(SkillAttackDetectionEvent attackDetectionEvent)
        {
            var item = new AttackDetectionTrackItem();
            item.Init(this, frameWidth, attackDetectionEvent, trackStyle.AddChildTrack());
            item.SetTrackName(attackDetectionEvent.TrackName);
            trackItems.Add(item);
        }

        /// <summary>
        /// 更新子轨道名称
        /// </summary>
        private void UpdateChildTrackName(SkillMultiLineTrackStyle.ChildTrack childTrack, string newName)
        {
            // 同步给配置
            AttackDetectionData.FrameData[childTrack.GetIndex()].TrackName = newName;
            SkillEditorWindow.Instance.SaveSkillConfig();
        }

        /// <summary>
        /// 新增子轨道
        /// </summary>
        private void AddChildTrack()
        {
            SkillAttackDetectionEvent attackDetectionEvent = new();
            AttackDetectionData.FrameData.Add(attackDetectionEvent);
            CreateAttackDetectionTrackItem(attackDetectionEvent);
            SkillEditorWindow.Instance.SaveSkillConfig();
        }

        /// <summary>
        /// 检查子轨道能否删除
        /// </summary>
        /// <param name="index">子轨道的索引</param>
        /// <returns></returns>
        private bool CheckDeleteChildTrack(int index)
        {
            if (index < 0 || index >= AttackDetectionData.FrameData.Count)
                return false;
            if (AttackDetectionData.FrameData[index] == null)
                return false;
            AttackDetectionData.FrameData.RemoveAt(index);
            trackItems.RemoveAt(index);
            SkillEditorWindow.Instance.SaveSkillConfig();
            return true;
        }

        private void SwapChildTrack(int index1, int index2)
        {
            var attackDetectionData1 = AttackDetectionData.FrameData[index1];
            var attackDetectionData2 = AttackDetectionData.FrameData[index2];
            AttackDetectionData.FrameData[index1] = attackDetectionData2;
            AttackDetectionData.FrameData[index2] = attackDetectionData1;
            // 保存交给窗口的退出机制
        }

        #region 重载方法

        public override void DrawGizmos()
        {
            foreach (var item in trackItems)
            {
                int currentFrameIndex = SkillEditorWindow.Instance.CurrentSelectFrameIndex;
                SkillAttackDetectionEvent detectionEvent = item.AttackDetectionEvent;
                if (currentFrameIndex < detectionEvent.FrameIndex ||
                    currentFrameIndex > detectionEvent.FrameIndex + detectionEvent.DurationFrame)
                    continue;
                item.DrawGizmos();
            }
        }

        public override void OnSceneGUI()
        {
            foreach (var item in trackItems)
            {
                int currentFrameIndex = SkillEditorWindow.Instance.CurrentSelectFrameIndex;
                SkillAttackDetectionEvent detectionEvent = item.AttackDetectionEvent;
                if (currentFrameIndex < detectionEvent.FrameIndex ||
                    currentFrameIndex > detectionEvent.FrameIndex + detectionEvent.DurationFrame)
                    continue;
                // 必须选中才绘制
                if (SkillEditorInspector.CurrentTrackItem != item)
                    continue;
                item.OnSceneGUI();
            }
        }

        public override void Destroy()
        {
            trackStyle.Destroy();
        }
        
        #endregion
    }
}